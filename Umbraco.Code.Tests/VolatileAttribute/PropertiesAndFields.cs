using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class PropertiesAndFields : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UmbracoVolatileAnalyzer();
        }

        [TestMethod]
        public void GetSetVolatileProperty()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        [UmbracoVolatile]
        public string VolatileProperty {get; set;}
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            DemoClass.VolatileProperty = ""Volatile text""
            var test = DemoClass.VolatileProperty;
        }
    }
}
";

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 19, 13)}
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void GetSetNonVolatileProperty()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        public string VolatileProperty {get; set;}
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            DemoClass.VolatileProperty = ""Volatile text""
            var test = DemoClass.VolatileProperty;
        }
    }
}
";
            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void GetSetVolatileField()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        [UmbracoVolatile]
        public string VolatileProperty = ""Field text""
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            DemoClass.VolatileProperty = ""Volatile text""
            var test = DemoClass.VolatileProperty;
        }
    }
}
";

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 19, 13)}
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void NonVolatileField()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        public string VolatileProperty = ""Field text""
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            DemoClass.VolatileProperty = ""Volatile text""
            var test = DemoClass.VolatileProperty;
        }
    }
}
";
            VerifyCSharpDiagnostic(code);
        }
    }
}