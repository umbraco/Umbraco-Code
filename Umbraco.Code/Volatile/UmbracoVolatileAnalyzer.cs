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
using Microsoft.CodeAnalysis.Operations;


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
        private static readonly LocalizableString MethodDescription = "The method is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                      "to suppress the error down to a warning, add UmbracoSuppressVolatile as an assembly level attribute.";
        private static readonly LocalizableString ClassDescription = "The class is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                     "to suppress the error down to a warning, add UmbracoSuppressVolatile as an assembly level attribute.";

        private static readonly DiagnosticDescriptor MethodErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Error, true, MethodDescription, HelpLinkUri);
        private static readonly DiagnosticDescriptor MethodWarningRule
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                DiagnosticSeverity.Warning, true, MethodDescription, HelpLinkUri);
        
        private static readonly DiagnosticDescriptor ClassErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Error, true, ClassDescription, HelpLinkUri);
        private static readonly DiagnosticDescriptor ClassWarningRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Warning, true, ClassDescription, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(MethodErrorRule, MethodWarningRule, ClassErrorRule, ClassWarningRule);

        public override void Initialize(AnalysisContext context)
        {
            // Since the analyzer doesn't read or write anything it's safe to run it concurrently.
            context.EnableConcurrentExecution();
            // Analyze methods that are invoked (InvocationExpression)
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
            // Analyze constructors that are invoked (ObjectCreationExpression)
            context.RegisterSyntaxNodeAction(AnalyzeConstructorInvocation, SyntaxKind.ObjectCreationExpression);
            // Analyze classes that are declared (ClassDeclaration)
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            var propertyAndFieldKinds = new[]
            {
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.PointerMemberAccessExpression
            };
            context.RegisterSyntaxNodeAction(AnalyzeFieldAndProperties, propertyAndFieldKinds);
        }

        private static bool HasSuppressAttribute(IAssemblySymbol assemblySymbol)
        {
            // Get the assembly that the method was invoked from, and get all the attributes from that assembly.
            var assemblyAttributes = assemblySymbol.GetAttributes();

            // If the assembly has a SuppressVolatileAttribute issue a warning otherwise issue an error.
            return assemblyAttributes.Any(x => !(x is null) && 
                                               (x.AttributeClass?.Name == "UmbracoSuppressVolatile" || 
                                                x.AttributeClass?.Name == "UmbracoSuppressVolatileAttribute"));
            
        }

        private static bool HasVolatileAttribute(IEnumerable<AttributeData> attributes)
        {
            return attributes.Any(a =>
                a.AttributeClass?.Name == "UmbracoVolatile" || a.AttributeClass?.Name == "UmbracoVolatileAttribute");
        }

        private static void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
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
            if (!HasVolatileAttribute(attributes))
            {
                return;
            }

            // If the assembly has a SuppressVolatileAttribute issue a warning otherwise issue an error.
            var isReducedToWarning = HasSuppressAttribute(containingMethodSymbol.ContainingAssembly);
            
            var diagnostic = Diagnostic.Create(
                isReducedToWarning ? MethodWarningRule : MethodErrorRule,
                invocationExpr.GetLocation(),
                invokedMethodSymbol.ToString());
            
            context.ReportDiagnostic(diagnostic);
            }

        private static void AnalyzeConstructorInvocation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax) context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation.Type, context.CancellationToken).Symbol as INamedTypeSymbol;
            // If we can't get the object creation as INamedTypeSymbol just ignore it
            if (symbolInfo is null) return;

            // If the class is not marked by a volatile attribute throw no error
            if (!HasVolatileAttribute(symbolInfo.GetAttributes()))
            {
                return;
            }

            var isReducedToWarning = HasSuppressAttribute(symbolInfo.ContainingAssembly);
            
            var diagnostic = Diagnostic.Create(
                isReducedToWarning ? ClassWarningRule : ClassErrorRule, 
                objectCreation.GetLocation(), 
                $"{symbolInfo.ContainingNamespace.Name}.{symbolInfo.Name}");
            
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax) context.Node;

            var symbolInfo = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            var baseTypeSymbolInfo = symbolInfo?.BaseType;
            // If there is no inheritance, or symbolInfo does not exist, throw no error
            if(baseTypeSymbolInfo is null) return;
            
            // If inheritance is not marked as volatile, just return
            if(!HasVolatileAttribute(baseTypeSymbolInfo.GetAttributes())) return;

            var isReducedToWarning = HasSuppressAttribute(symbolInfo.ContainingAssembly);
            
            var diagnostic = Diagnostic.Create(
                isReducedToWarning ? ClassWarningRule : ClassErrorRule, 
                classDeclaration.GetLocation(), 
                $"{baseTypeSymbolInfo.ContainingNamespace.Name}.{baseTypeSymbolInfo.Name}");
            
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeFieldAndProperties(SyntaxNodeAnalysisContext context)
        { 
            var accessExpression = (MemberAccessExpressionSyntax) context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(accessExpression.Name, context.CancellationToken);
        }
        
    }
}
