﻿using System;
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
        private const string HelpLinkUri = "https://github.com/umbraco/Umbraco-Code";

        private static readonly LocalizableString Title = "Umbraco Volatile method";
        private static readonly LocalizableString MessageFormat = "Method is volatile";
        private static readonly LocalizableString Description = "Method is volatile and may break in the future";

        private static readonly DiagnosticDescriptor ErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Error, true, Description, HelpLinkUri);
        private static readonly DiagnosticDescriptor WarningRule
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Warning, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ErrorRule, WarningRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            var methodSymbol =
                context.SemanticModel.GetSymbolInfo(invocationExpr, context.CancellationToken).Symbol as IMethodSymbol;

            // Collect attributes 
            if (!(methodSymbol is null))
            {
                var attributes = methodSymbol.GetAttributes().Union(methodSymbol.ContainingType.GetAttributes());

                if (!(attributes is null) && attributes.Any())
                {
                    foreach (var attribute in attributes)
                    {
                        if (attribute.AttributeClass.Name == "UmbracoVolatile")
                        {
                            var assemblyAttributes = (context.ContainingSymbol as IMethodSymbol).ContainingAssembly.GetAttributes();
                            // Why is the "Attribute" part removed from normal attribute, but not assembly attribute? o.O 
                            if (assemblyAttributes.Any(x => !(x is null) && x.AttributeClass.Name == "UmbracoSuppressVolatile"))
                            {
                                var diagnostic = Diagnostic.Create(WarningRule, invocationExpr.GetLocation());
                                context.ReportDiagnostic(diagnostic);
                            }
                            else
                            {
                                var diagnostic = Diagnostic.Create(ErrorRule, invocationExpr.GetLocation());
                                context.ReportDiagnostic(diagnostic);
                            }
                            
                        }
                    }
                }
            }
        }
    }
}