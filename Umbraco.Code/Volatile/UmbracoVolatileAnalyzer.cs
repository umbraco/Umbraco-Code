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
        private const string DiagnosticId = "UmbracoCodeVolatile";
        private const string Category = "Access modifier";
        private const string HelpLinkUri = "https://our.umbraco.com/documentation/Reference/UmbracoVolatile/"; 

        private static readonly LocalizableString Title = "Umbraco Volatile method";
        private static readonly LocalizableString MessageFormat = "{0} is volatile";
        private static readonly LocalizableString Description = "Method is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                "to suppress the error down to a warning, add UmbracoSuppressVolatile as an assembly level attribute.";

        private static readonly DiagnosticDescriptor ErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Error, true, Description, HelpLinkUri);
        private static readonly DiagnosticDescriptor WarningRule
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Warning, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ErrorRule, WarningRule);

        public override void Initialize(AnalysisContext context)
        {
            // Since the analyzer doesn't read or write anything it's safe to run it concurrently.
            context.EnableConcurrentExecution();
            // Analyze methods that are invoked (InvocationExpression)
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            // Get the method that is invoked as an expression
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            
            // Ignore if we can't turn the invocationExpr into a method symbol allowing us to access it's attributes and containing class
            if (!(context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken).Symbol is IMethodSymbol
                invokedMethodSymbol))
            {
                return;
            }
            
            // Ignore if we cant get the method from which the invoked method was invoked
            if (!(context.ContainingSymbol is IMethodSymbol containingMethodSymbol))
            {
                return;
            }

            // Don't raise an error if the volatile method is used within its own class
            if (SymbolEqualityComparer.Default.Equals(invokedMethodSymbol.ContainingType,
                containingMethodSymbol.ContainingType))
            {
                return;
            }

            // Get the attributes from the invoked method and the class containing it.
            var attributes = invokedMethodSymbol.GetAttributes().Union(invokedMethodSymbol.ContainingType.GetAttributes());
            
            // Ignore if the method or its containing class is NOT marked with the volatile attribute
            // the attribute is however only checked by name, meaning that's it's not necessary to use the attributes from this project. 
            if (!attributes.Any(x =>
                (x.AttributeClass?.Name == "UmbracoVolatile" || x.AttributeClass?.Name == "UmbracoVolatileAttribute")))
            {
                return;
            }
            
            // Get the assembly that the method was invoked from, and get all the attributes from that assembly.
            var assemblyAttributes = containingMethodSymbol.ContainingAssembly.GetAttributes();

            // If the assembly has a SuppressVolatileAttribute issue a warning otherwise issue an error.
            var isReducedToWarning = assemblyAttributes.Any(x => !(x is null) &&
                                                                 (x.AttributeClass?.Name ==
                                                                  "UmbracoSuppressVolatile" ||
                                                                  x.AttributeClass?.Name ==
                                                                  "UmbracoSuppressVolatileAttribute"));
            
            var diagnostic = Diagnostic.Create(
                isReducedToWarning ? WarningRule : ErrorRule,
                invocationExpr.GetLocation(),
                invokedMethodSymbol.ToString());
            
            context.ReportDiagnostic(diagnostic);
            }
        }
    }
