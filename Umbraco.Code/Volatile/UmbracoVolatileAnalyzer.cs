using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;


namespace Umbraco.Code.Volatile
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UmbracoVolatileAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UmbracoCodeVolatile";
        public const string Category = "Usage";
        private const string HelpLinkUri = "https://github.com/umbraco/Umbraco-Code"; // TODO: use actual helpful link

        private static readonly LocalizableString Title = "Umbraco Volatile method";
        private static readonly LocalizableString MessageFormat = "{0} is volatile";
        private static readonly LocalizableString Description = "Method is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                "to suppress the error down to a warning, add UmbracoSupressVolatile as an assembly level attribute.";

        private static readonly DiagnosticDescriptor ErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Error, true, Description, HelpLinkUri);
        private static readonly DiagnosticDescriptor WarningRule
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Warning, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ErrorRule, WarningRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            var methodSymbol =
                context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken).Symbol as IMethodSymbol;

            if (!(methodSymbol is null))
            {
                var attributes = methodSymbol.GetAttributes().Union(methodSymbol.ContainingType.GetAttributes());

                if (!(attributes is null) && attributes.Any(x => (x.AttributeClass.Name == "UmbracoVolatile" || x.AttributeClass.Name == "UmbracoVolatileAttribute")))
                {
                    var assemblyAttributes = (context.ContainingSymbol as IMethodSymbol).ContainingAssembly.GetAttributes();

                    if (assemblyAttributes.Any(x => !(x is null) &&
                    (x.AttributeClass.Name == "UmbracoSuppressVolatileAttribute" || x.AttributeClass.Name == "UmbracoSuppressVolatile")))
                    {
                        var diagnostic = Diagnostic.Create(WarningRule, invocationExpr.GetLocation(), methodSymbol.ToString());
                        context.ReportDiagnostic(diagnostic);
                    }
                    else
                    {
                        var diagnostic = Diagnostic.Create(ErrorRule, invocationExpr.GetLocation(), methodSymbol.ToString());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
