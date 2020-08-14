using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.MapAll;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests
{
    [TestClass]
    public class VolatileTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new VolatileAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            // TODO: Change this
            return new MapAllFixProvider();
        }

        [TestMethod]
        public void ErrorMethodCallFromVolatileObject()
        {
            const string code = @"
namespace VolatileDemo
{
    [Volatile]
    public class DemoClass
    {
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            testClass.VolatileMethod();
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "Method is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 13) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ErrorOnVolatileMethodCall()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        [Volatile]
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            testClass.VolatileMethod();
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "Method is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 13) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }
    }
}
