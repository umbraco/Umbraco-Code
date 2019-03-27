using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Umbraco.Code.MapAll
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MapAllAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UmbracoCodeMapAll";
        public const string UnassignedMembersKey = DiagnosticId + "_Unassigned";
        public const string AvailableMembersKey = DiagnosticId + "_Available";

        private const string Category = "Usage";
        private const string HelpLinkUri = "https://github.com/umbraco/"; // fixme?

        private static readonly LocalizableString Title = "MapAll Analyzer";
        private static readonly LocalizableString MessageFormat = "Method does not map propert{0} {1}.";
        private static readonly LocalizableString Description = "Ensures that all properties are mapped.";

        private static readonly DiagnosticDescriptor Rule
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCodeBlockStartAction<SyntaxKind>(StartCodeBlock);
        }

        private void StartCodeBlock(CodeBlockStartAnalysisContext<SyntaxKind> context)
        {
            // only for methods
            if (context.OwningSymbol.Kind != SymbolKind.Method)
                return;

            // with at least 2 parameters
            var method = (IMethodSymbol) context.OwningSymbol;
            if (method.Parameters.Length < 2)
                return;

            // marked with the proper "Umbraco.Code.MapAll" comment
            if (!context.CodeBlock.HasLeadingTrivia)
                return;
            var singleLineComments = context.CodeBlock.GetLeadingTrivia().Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia));
            if (!MapAll(singleLineComments, out var excludes))
                return;

            // get the type symbols for parameters
            var sourceSymbol = method.Parameters[0];
            var targetSymbol = method.Parameters[1];

            var location = context.OwningSymbol.Locations.First();
            var codeBlockAnalyzer = new CodeBlockAnalyzer(Rule, location, sourceSymbol, targetSymbol, excludes);

            context.RegisterSyntaxNodeAction(codeBlockAnalyzer.AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterCodeBlockEndAction(codeBlockAnalyzer.EndCodeBlock);
        }

        private bool MapAll(IEnumerable<SyntaxTrivia> singleLineComments, out List<string> excludes)
        {
            var mapAll = false;
            excludes = null;
            foreach (var singleLineComment in singleLineComments)
                mapAll |= CommentLineParser.ParseCommentLine(singleLineComment.ToString(), ref excludes);
            return mapAll;
        }
    }
}
