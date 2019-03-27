using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Umbraco.Code.MapAll
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MapAllFixProvider))]
    [Shared]
    public class MapAllFixProvider : CodeFixProvider
    {
        private const string Title = "Assign all members";

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(MapAllAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First(x => x.Id == MapAllAnalyzer.DiagnosticId);
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // read unassigned member names, passed on from diagnostic
            var unassignedMemberNames = GetUnassignedMemberNames(diagnostic);
            if (unassignedMemberNames.Length == 0)
                return;

            // read available source names, passed on from diagnostic
            var availableMemberNames = GetAvailableMemberNames(diagnostic);

            // find the code block identified by the diagnostic
            var methodDeclaration = root.FindNode(diagnosticSpan) as MethodDeclarationSyntax;
            var codeBlock = methodDeclaration?.Body;
            if (codeBlock == null)
                return;

            // get the source/target parameter name from the method declaration
            var sourceName = methodDeclaration.ParameterList.Parameters[0].Identifier.ValueText;
            var targetName = methodDeclaration.ParameterList.Parameters[1].Identifier.ValueText;

            // register a code action that will invoke the fix
            context.RegisterCodeFix(CodeAction.Create(
                    Title,
                    ct => PopulateMissingAssignmentsAsync(context.Document, codeBlock, sourceName, availableMemberNames, targetName, unassignedMemberNames, ct),
                    Title),
                diagnostic);
        }

        private static string[] GetUnassignedMemberNames(Diagnostic diagnostic)
        {
            if (!diagnostic.Properties.TryGetValue(MapAllAnalyzer.UnassignedMembersKey, out var unassignedMembersString))
                return Array.Empty<string>();

            return unassignedMembersString.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(str => str.Trim())
                .ToArray();
        }

        private static string[] GetAvailableMemberNames(Diagnostic diagnostic)
        {
            if (!diagnostic.Properties.TryGetValue(MapAllAnalyzer.AvailableMembersKey, out var availableMembersString))
                return Array.Empty<string>();

            return availableMembersString.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(str => str.Trim())
                .ToArray();
        }

        private static StatementSyntax CreateStatement(string targetName, string memberName, string sourceName = null)
        {
            // target.Foo
            var targetIdentifier = SyntaxFactory.IdentifierName(targetName);
            var memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, targetIdentifier, SyntaxFactory.IdentifierName(memberName));

            // <source>
            ExpressionSyntax sourceExpression;
            if (sourceName == null)
            {
                // default
                sourceExpression = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, SyntaxFactory.Token(SyntaxKind.DefaultKeyword));
            }
            else
            {
                // source.Foo
                var sourceIdentifier = SyntaxFactory.IdentifierName(sourceName);
                sourceExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, sourceIdentifier, SyntaxFactory.IdentifierName(memberName));
            }

            // target.Foo = <source>
            var assignment = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, memberAccess, sourceExpression);

            StatementSyntax statement;
            if (sourceName == null)
            {
                // ; // fi*me
                var leading = SyntaxTriviaList.Empty;
                var whitespace = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ");
                var comment = SyntaxFactory.SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, "// fixme");
                var eol = SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine);
                //var trailing = SyntaxTriviaList.Create(comment).Add(eol);
                var trailing = new SyntaxTriviaList(whitespace, comment, eol);
                var semiColon = SyntaxFactory.Token(leading, SyntaxKind.SemicolonToken, trailing);

                // target.Foo = default; // fi*me
                statement = SyntaxFactory.ExpressionStatement(assignment, semiColon);
            }
            else
            {
                // target.Foo = source.Foo;
                statement = SyntaxFactory.ExpressionStatement(assignment);
            }

            return statement;
        }

        private static async Task<Document> PopulateMissingAssignmentsAsync(Document document, BlockSyntax codeBlock, 
            string sourceName, IEnumerable<string> availableMemberNames,
            string targetName, IEnumerable<string> unassignedMemberNames, 
            CancellationToken ct)
        {
            // Can't manipulate syntax without a syntax root
            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root == null)
                return document;

            // Add missing member assignments
            var expressions = codeBlock.Statements;
            var statements = expressions.AddRange(unassignedMemberNames
                .Select(memberName => CreateStatement(targetName, memberName, availableMemberNames.Contains(memberName) ? sourceName : null)));

            // Reformat fails due to the codefix code not compiling..
            //            newObjectInitializer =
            //                (InitializerExpressionSyntax) Formatter.Format(newObjectInitializer, MSBuildWorkspace.Create());

            root = root.ReplaceNode(codeBlock, codeBlock.WithStatements(statements));
            return document.WithSyntaxRoot(root);
        }
    }
}
