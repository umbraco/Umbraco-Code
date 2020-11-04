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

        [TestMethod]
        public void InheritVolatileClass()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class DemoClass
    {}

    public class DemoClass2 : DemoClass
    {}
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 8, 5) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void InheritFromClassInheritingVolatile()
        {
            // Right now you are allowed to inherit a class, that in turn inherits from a volatile class
            // However, if you try to access q method from the volatile base class you get the error
            // See ErrorFromInheritedVolatileOnSelf tests to see an example
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class DemoClass
    {}

    public class DemoClass2 : DemoClass
    {}

    public class DemoClass3 : DemoClass2
    {}
}
";
            // Error at: public class DemoClass2 : DemoClass
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 8, 5) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
    }
}