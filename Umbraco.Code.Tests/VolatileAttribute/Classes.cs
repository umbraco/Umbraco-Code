using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class Classes : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UmbracoVolatileAnalyzer();
        }

        
        [TestMethod]
        public void InstantiateVolatileClassFromMethod()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class DemoClass
    {}

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
        }
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 12, 29) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void InstantiateVolatileClassFromField()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class DemoClass
    {}

    public class DemoClass2
    {
        public DemoClass volatileClass = new DemoClass();
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 10, 42) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void InstantiateNonVolatileClass()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
        }
    }
}
";
            VerifyCSharpDiagnostic(code);
        }
    }
}