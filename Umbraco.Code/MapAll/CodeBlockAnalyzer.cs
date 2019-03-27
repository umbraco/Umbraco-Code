using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Umbraco.Code.MapAll
{
    public class CodeBlockAnalyzer
    {
        private readonly DiagnosticDescriptor _rule;
        private readonly Location _location;
        private readonly IParameterSymbol _sourceParameter;
        private readonly IParameterSymbol _targetParameter;
        private readonly List<string> _excludeMembers;
        private readonly List<string> _assignedMembers = new List<string>();

        public CodeBlockAnalyzer(DiagnosticDescriptor rule, Location location, IParameterSymbol sourceParameter, IParameterSymbol targetParameter, List<string> exclude)
        {
            _rule = rule;
            _location = location;
            _sourceParameter = sourceParameter;
            _targetParameter = targetParameter;
            _excludeMembers = exclude;
        }

        public void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax) context.Node;

            // only for = assignments
            if (!assignment.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                return;

            // to a simple member access eg "target.Value"
            if (!assignment.Left.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                return;
            var memberAccess = (MemberAccessExpressionSyntax) assignment.Left;

            // where target is the parameter
            if (!memberAccess.OperatorToken.IsKind(SyntaxKind.DotToken))
                return;
            if (!memberAccess.Expression.IsKind(SyntaxKind.IdentifierName))
                return;
            var identifier = (IdentifierNameSyntax) memberAccess.Expression;
            var symbol = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol as IParameterSymbol;
            if (!_targetParameter.Equals(symbol))
                return;

            //TODO: only if member of name Name is a property

            var memberName = memberAccess.Name; // the name of the assigned member
            _assignedMembers.Add(memberName.Identifier.ValueText);
        }

        public void EndCodeBlock(CodeBlockAnalysisContext context)
        {
            bool IsAccessible(ISymbol symbol)
                => symbol.DeclaredAccessibility == Accessibility.Public || symbol.DeclaredAccessibility == Accessibility.Internal;

            var targetTypes = _targetParameter.Type.TypeKind == TypeKind.Interface
                ? GetBaseTypesAndThisAndAllInterfaces(_targetParameter.Type)
                : GetBaseTypesAndThis(_targetParameter.Type);

            // TODO: check if member is assignable using Roslyn data flow analysis instead of these constraints,
            // as that is the only way to properly determine if it is assignable or not in a context
            var assignableProperties = targetTypes
                .SelectMany(x => x.GetMembers())
                .OfType<IPropertySymbol>()
                .Where(m =>
                    !m.IsIndexer &&
                    !m.IsReadOnly &&
                    IsAccessible(m) &&
                    IsAccessible(m.SetMethod))
                .Select(m => m.Name)
                .OrderBy(x => x);

            var unassignedMemberNames = _excludeMembers == null
                ? assignableProperties.Except(_assignedMembers).ToList()
                : assignableProperties.Except(_assignedMembers).Except(_excludeMembers).ToList();
            if (unassignedMemberNames.Count == 0)
                return;

            IEnumerable<string> availableProperties = GetBaseTypesAndThisAndAllInterfaces(_sourceParameter.Type)
                .SelectMany(x => x.GetMembers())
                .OfType<IPropertySymbol>()
                .Where(m => 
                    !m.IsIndexer &&
                    IsAccessible(m) &&
                    IsAccessible(m.GetMethod))
                .Select(m => m.Name);

            var availableMembersString = string.Join(", ", availableProperties);
            var unassignedMembersString = string.Join(", ", unassignedMemberNames);
            var plural = unassignedMemberNames.Count > 1 ? "ies" : "y";

            var properties = new Dictionary<string, string>
            {
                { MapAllAnalyzer.UnassignedMembersKey, unassignedMembersString },
                { MapAllAnalyzer.AvailableMembersKey, availableMembersString }
            }.ToImmutableDictionary();

            var diagnostic = Diagnostic.Create(_rule, _location, properties, plural, unassignedMembersString);
            context.ReportDiagnostic(diagnostic);
        }

        // https://stackoverflow.com/questions/31122993
        // https://stackoverflow.com/questions/30443616

        private IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        private IEnumerable<ITypeSymbol> GetBaseTypesAndThisAndAllInterfaces(ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }

            foreach (var i in type.AllInterfaces)
                yield return i;
        }
    }
}