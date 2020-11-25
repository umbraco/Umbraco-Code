using System;
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
        private static readonly LocalizableString Description = "The resource is volatile and may break in the future and it's therefore not recommended to use outside testing, " +
                                                                "to suppress the error down to a warning, add UmbracoSuppressVolatile as an assembly level attribute.";

        private static readonly DiagnosticDescriptor ErrorRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Error, true, Description, HelpLinkUri);
        private static readonly DiagnosticDescriptor WarningRule 
            = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, 
                DiagnosticSeverity.Warning, true, Description, HelpLinkUri);
        

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ErrorRule, WarningRule);
        
        // We don't want to report any errors or warnings in our own projects.
        // We could just suppress the errors in the Umbraco project, however, this would result in an immense amount of irrelevant warnings
        // which would obfuscate actual warnings we might be interested in.
        private static readonly ImmutableArray<string> AllowedProjects = ImmutableArray.Create(
            "Umbraco.Core",
            "Umbraco.Examine.Lucene",
            "Umbraco.Infrastructure",
            "Umbraco.ModelsBuilder.Embedded",
            "Umbraco.Persistance.SqlCe",
            "Umbraco.PublishedCache.NuCache",
            "Umbraco.Web",
            "Umbraco.Web.BackOffice",
            "Umbraco.Web.Common",
            "Umbraco.Web.UI",
            "Umbraco.Web.UI.Client",
            "Umbraco.Web.UI.NetCore",
            "Umbraco.Web.Website",
            "Umbraco.Tests",
            "Umbraco.Tests.Common",
            "Umbraco.Tests.UnitTests",
            "Umbraco.Tests.Integration",
            "Umbraco.Tests.TestData",
            "Umbraco.Tests.Benchmarks"
            );

        public override void Initialize(AnalysisContext context)
        {
            // Since the analyzer doesn't read or write anything it's safe to run it concurrently.
            context.EnableConcurrentExecution();
            
            var propertyAndFieldKinds = new[]
            {
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.PointerMemberAccessExpression
            };
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeConstructorInvocation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, propertyAndFieldKinds);
            context.RegisterSyntaxNodeAction(AnalyzeAttributeList, SyntaxKind.AttributeList);
            context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        }
        
        private static bool IsAllowedProject(SyntaxNodeAnalysisContext context)
        {
            var containingAssembly = context.ContainingSymbol?.ContainingAssembly;
            if (containingAssembly is null) return false;
            return AllowedProjects.Contains(containingAssembly.Name);
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
        
        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, SyntaxNode errorNode, IAssemblySymbol errorAssembly, string messageArg)
        {
            var diagnostic = Diagnostic.Create(
                HasSuppressAttribute(errorAssembly) ? WarningRule : ErrorRule,
                errorNode.GetLocation(),
                messageArg);

            context.ReportDiagnostic(diagnostic);
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
        private static ImmutableArray<AttributeData> GetAllContainingTypesAttributes(ISymbol symbol)
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

        /// <summary>
        /// Analyzes methods that are invoked (InvocationExpression)
        /// </summary>
        /// <param name="context"></param>
        private static void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
        {
            // If the method is invoked from our own project, report no error.
            if(IsAllowedProject(context))
            {
                return;
            }
            
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

            // Don't report an error if the volatile method is used within its own class
            if (SymbolEqualityComparer.Default.Equals(invokedMethodSymbol.ContainingType,
                containingMethodSymbol.ContainingType))
            {
                return;
            }

            // Get the attributes from the invoked method and the class containing it.
            var attributes = invokedMethodSymbol.GetAttributes().Union(invokedMethodSymbol.ContainingType.GetAttributes());
            
            // Report the error if the method or its containing class is marked with the volatile attribute
            // the attribute is however only checked by name, meaning that's it's not necessary to use the attributes from this project. 
            if (HasVolatileAttribute(attributes))
            {
                ReportDiagnostic(context, invocationExpr, containingMethodSymbol.ContainingAssembly, invokedMethodSymbol.ToString());
            }
        }

        /// <summary>
        /// Analyzes constructors that are invoked (ObjectCreationExpression)
        /// </summary>
        /// <param name="context"></param>
        private static void AnalyzeConstructorInvocation(SyntaxNodeAnalysisContext context)
        {
            // If the invocation happens from an umbraco project report no error.
            if(IsAllowedProject(context)) return;

            var objectCreation = (ObjectCreationExpressionSyntax) context.Node;

            // Since we're working with constructor invocation, we want the symbol info of the created type.
            var symbolInfo = context.SemanticModel.GetSymbolInfo(objectCreation.Type, context.CancellationToken).Symbol as INamedTypeSymbol;
            // If we can't get the object creation as INamedTypeSymbol just ignore it
            if (symbolInfo is null) return;

            // If the constructed class is marked by a volatile attribute report the error
            if (HasVolatileAttribute(symbolInfo.GetAttributes()))
            {
                ReportDiagnostic(context, objectCreation, symbolInfo.ContainingAssembly, symbolInfo.ToString());
            }
        }

        /// <summary>
        /// Analyzes classes that are declared (ClassDeclaration)
        /// </summary>
        /// <param name="context"></param>
        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            // If the declaration happens from an umbraco project report no error.
            if(IsAllowedProject(context)) return;

            var classDeclaration = (ClassDeclarationSyntax) context.Node;

            var symbolInfo = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            // Get the parent class.
            var baseTypeSymbolInfo = symbolInfo?.BaseType;
            // symbolInfo should always have a parent, since all classes inherits from object, but better safe than sorry.
            if(baseTypeSymbolInfo is null) return;

            var containingAssembly = symbolInfo.ContainingAssembly;
            
            // Check implemented interfaces for volatile interfaces, needs to be done separately as you can both
            // inherit and implement interfaces at the same time, 
            foreach (var interfaceSymbol in symbolInfo.Interfaces)
            {
                if (HasVolatileAttribute(interfaceSymbol.GetAttributes()))
                {
                    ReportDiagnostic(context, classDeclaration, containingAssembly, interfaceSymbol.ToString());
                }
            }

            // Check the inherited class as well
            if(HasVolatileAttribute(baseTypeSymbolInfo.GetAttributes()))
            { 
                ReportDiagnostic(context, classDeclaration, containingAssembly, baseTypeSymbolInfo.ToString());
            }
        }

        /// <summary>
        /// Analyzes fields and properties that are accessed.
        /// </summary>
        /// <param name="context"></param>
        private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        { 
            // If the member access happens from an umbraco project report no error.
            if(IsAllowedProject(context)) return;

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

            // Report the error if a volatile attribute is found
            if (HasVolatileAttribute(GetAllContainingTypesAttributes(symbol)))
            {
                ReportDiagnostic(context, accessExpression, symbol.ContainingAssembly, symbol.ToString());
            }
        }

        /// <summary>
        /// Analyzes when an attribute is applied.
        /// </summary>
        /// <param name="context"></param>
        private static void AnalyzeAttributeList(SyntaxNodeAnalysisContext context)
        {
            // If the attribute is applied in an umbraco project report no error.
            if(IsAllowedProject(context)) return;

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

            ReportDiagnostic(context, attributeList, containingAssembly, volatileAttribute.ToString());
        }
        
        /// <summary>
        /// Analyzes when a parameter is requested or passed.
        /// </summary>
        /// <param name="context"></param>
        private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            // If the parameter is passed or requested in an umbraco project report no error.
            if(IsAllowedProject(context)) return;

            var parameterSyntax = (ParameterSyntax) context.Node;

            // We try and get the type of the parameter as a symbol to see if it's volatile, if we can't we stop analysis
            var parameterTypeSyntax = parameterSyntax.Type;
            if (parameterTypeSyntax is null) return;
            
            var parameterTypeSymbol = context.SemanticModel.GetSymbolInfo(parameterTypeSyntax, context.CancellationToken).Symbol;
            if (parameterTypeSymbol is null) return;

            // If it's not volatile we stop
            if (!HasVolatileAttribute(GetAllContainingTypesAttributes(parameterTypeSymbol))) return;
            
            // Since we don't already have the symbol for the parameter syntax we just use the same trick as in AnalyzeAttributeList
            var containingAssembly = context.ContainingSymbol?.ContainingAssembly;
            if (containingAssembly is null) return;

            ReportDiagnostic(context, parameterSyntax, containingAssembly, parameterTypeSymbol.ToString());
        }
        
    }
}
