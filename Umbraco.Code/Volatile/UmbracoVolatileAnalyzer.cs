﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        private static readonly LocalizableString Title = "Umbraco Volatile";
        private static readonly LocalizableString MessageFormat = "{0} is volatile";
        private static readonly LocalizableString MethodDescription = "The method is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                      "to suppress the error down to a warning, add UmbracoSuppressVolatile as an assembly level attribute.";
        private static readonly LocalizableString ClassDescription = "The class is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                     "to suppress the error down to a warning, add UmbracoSuppressVolatile as an assembly level attribute.";
        private static readonly LocalizableString MemberDescription = "The member is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                     "to suppress the error down to a warning, add UmbracoSuppressVolatile as an assembly level attribute.";
        private static readonly LocalizableString AttributeDescription = "The attribute is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
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
        
        private static readonly DiagnosticDescriptor MemberErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Error, true, MemberDescription, HelpLinkUri);
        private static readonly DiagnosticDescriptor MemberWarningRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Warning, true, MemberDescription, HelpLinkUri);
        
        private static readonly DiagnosticDescriptor AttributeErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Error, true, AttributeDescription, HelpLinkUri);
        private static readonly DiagnosticDescriptor AttributeWarningRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Warning, true, AttributeDescription, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(MethodErrorRule, MethodWarningRule, ClassErrorRule, ClassWarningRule);

        public override void Initialize(AnalysisContext context)
        {
            // Since the analyzer doesn't read or write anything it's safe to run it concurrently.
            // TODO: Re enable concurrent execution, it's just a headache when debugging.
            // context.EnableConcurrentExecution();
            // Analyze methods that are invoked (InvocationExpression)
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
            // Analyze constructors that are invoked (ObjectCreationExpression)
            context.RegisterSyntaxNodeAction(AnalyzeConstructorInvocation, SyntaxKind.ObjectCreationExpression);
            // Analyze classes that are declared (ClassDeclaration)
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            // Analyze fields and properties that are accessed.
            var propertyAndFieldKinds = new[]
            {
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.PointerMemberAccessExpression
            };
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, propertyAndFieldKinds);
            // Analyze when applying an attribute.
            context.RegisterSyntaxNodeAction(AnalyzeAttributeList, SyntaxKind.AttributeList);
            // Analyze when requesting/passing parameters
            context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
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
        
        /// <summary>
        /// Gets all the attributes of the symbol, and any parent class of the symbol
        /// </summary>
        /// <remarks>
        /// I'm a bit worried about the performance of this, however it's necessary since a volatile class can
        /// have public child classes and enums which should in turn inherit the volatile status, so we have to look at
        /// all of them
        /// </remarks>
        /// <param name="symbol">Symbol to get attributes from</param>
        /// <returns>List of all attributes</returns>
        private static ImmutableArray<AttributeData> GetAllAttributes(ISymbol symbol)
        {
            var attributes = symbol.GetAttributes();
            var containingTypeSymbol = symbol.ContainingType;
            while (!(containingTypeSymbol is null))
            {
                attributes = attributes.AddRange(containingTypeSymbol.GetAttributes());
                // We set containingTypeSymbol to the ContainingType of the containingTypeSymbol to "step out" once
                containingTypeSymbol = containingTypeSymbol.ContainingType;
            }

            return attributes;
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

        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        { 
            var accessExpression = (MemberAccessExpressionSyntax) context.Node;
            
            var symbol = context.SemanticModel.GetSymbolInfo(accessExpression, context.CancellationToken).Symbol;

            // If no symbol was found, return
            if (symbol is null) return;

            // We only want to check on IFieldSymbol and IPropertySymbol, not method symbols and so on.
            if (!(symbol is IFieldSymbol || symbol is IPropertySymbol)) return;

            // We check if there is a containing symbol of the context, if there isn't it's safe to assume that 
            // the volatile member has not been accessed from within its own class.
            // If there is a containing symbol we check if its type is the same as the members type, if it is we return. 
            if (context.ContainingSymbol != null && 
                SymbolEqualityComparer.Default.Equals(symbol.ContainingType, 
                context.ContainingSymbol.ContainingType))
            {
                return;
            }

            // Stop analysis if no volatile attribute is found.
            if (!HasVolatileAttribute(GetAllAttributes(symbol))) return;

            var isReducedToWarning = HasSuppressAttribute(symbol.ContainingAssembly);

            var diagnostic = Diagnostic.Create(
                isReducedToWarning ? MemberWarningRule : MemberErrorRule, 
                accessExpression.GetLocation(),
                $"{symbol.ContainingNamespace.Name}.{symbol.ContainingType.Name}.{symbol.Name}");
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeAttributeList(SyntaxNodeAnalysisContext context)
        {
            var attributeList = (AttributeListSyntax) context.Node;

            ISymbol volatileAttribute = null;
            // You can apply more than one attribute in an attribute list, so we have to check all of them
            foreach (var attributeSyntax in attributeList.Attributes)
            {
                var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken).Symbol;
                // If we couldn't find the attribute symbol we just skip it.
                if(attributeSymbol is null) continue;

                // Attributes are a bit weird, the symbol the semantic model returns is a method symbol of the constructor
                // So we have to get the containing type and check that for any UmbracoVolatileAttributes.
                var attributes = attributeSymbol.ContainingType.GetAttributes();

                if (HasVolatileAttribute(attributes))
                {
                    volatileAttribute = attributeSymbol.ContainingType;
                    // As soon as we find an attribute which is volatile we break, to not spend unnecessary time on it.
                    break;
                }
            }

            // We found no volatile attributes, we can stop.
            if (volatileAttribute is null) return;
            
            // Finding the assembly is a bit weird for attribute lists as well, since the syntax node specifies the 
            // list it self, we can however use the containing symbol of the context, which should be a 
            // Class declaration, method declaration etc.
            var containingAssembly = context.ContainingSymbol?.ContainingAssembly;
            // If we find no assembly we just return, this shouldn't happen however, since this would mean that 
            // the attribute is essentially being applied to nothing.
            if (containingAssembly is null) return;
            var isReducedToWarning = HasSuppressAttribute(containingAssembly);

            var diagnostic = Diagnostic.Create(
                isReducedToWarning ? AttributeWarningRule : AttributeErrorRule,
                attributeList.GetLocation(),
                $"{volatileAttribute.ContainingNamespace.Name}.{volatileAttribute.Name}");
            context.ReportDiagnostic(diagnostic);
        }
        
        private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var parameterSyntax = (ParameterSyntax) context.Node;

            // We try and get the type of the parameter as a symbol to see if it's volatile, if we can't we stop analysis
            var parameterTypeSyntax = parameterSyntax.Type;
            if (parameterTypeSyntax is null) return;
            
            var parameterTypeSymbol = context.SemanticModel.GetSymbolInfo(parameterTypeSyntax, context.CancellationToken).Symbol;
            if (parameterTypeSymbol is null) return;

            // If it's not volatile we stop
            if (!HasVolatileAttribute(GetAllAttributes(parameterTypeSymbol))) return;
            
            // Since we don't already have the symbol for the parameter syntax we just use the same trick as in AnalyzeAttributeList
            var containingAssembly = context.ContainingSymbol?.ContainingAssembly;
            if (containingAssembly is null) return;
            var isReducedToWarning = HasSuppressAttribute(containingAssembly);

            // TODO: Create error rule for parameters, or just use a generic one, it's getting out of hand with the amount of error rules...
            var diagnostics = Diagnostic.Create(
                isReducedToWarning ? ClassWarningRule : ClassErrorRule,
                parameterSyntax.GetLocation(),
                $"{parameterTypeSymbol.ContainingNamespace}.{parameterTypeSymbol.Name}");
            context.ReportDiagnostic(diagnostics);
        }
        
    }
}
