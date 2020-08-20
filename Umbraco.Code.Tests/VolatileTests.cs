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
            return new UmbracoVolatileAnalyzer();
        }

        [TestMethod]
        public void ErrorMethodCallFromVolatileObject()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
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
                Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
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
        [UmbracoVolatile]
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
                Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 13) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void NoErrorIfNoAttribute()
        {
            const string code = @"
namespace VolatileDemo
{
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

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void NonVolatileAmongstVolatile()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        [UmbracoVolatile]
        public void VolatileMethod()
        {
            Console.WriteLine(""""!!!Danger to manifold!!!"""");
        }

        public void NonVolatileMethod(){
            Console.WriteLine(""Saef"")
        }

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            testClass.NonVolatileMethod();
        }
    }
}
";

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void EnsureWarnWhenSuppressed()
        {
            const string code = @"
[assembly: UmbracoSuppressVolatile]
namespace VolatileDemo
{
    [UmbracoVolatile]
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
                Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ErrorFromInheretedVolatile()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class DemoClass
    {
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

    }

    public class InheretedVolatile : DemoClass
    {

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new InheretedVolatile();
            testClass.VolatileMethod();
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 13) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }


        [TestMethod]
        public void ErrorFromInheretedVolatileMethod()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {   
        [UmbracoVolatile]
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

    }

    public class InheretedVolatile : DemoClass
    {

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new InheretedVolatile();
            testClass.VolatileMethod();
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 13) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void NoErrorFromInheretedNonVolatileMethod()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {   
        [UmbracoVolatile]
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

        public void NonVolatileMethod()
        {
            Console.WriteLine(""No Dange"");
        }

    }

    public class InheretedVolatile : DemoClass
    {

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new InheretedVolatile();
            testClass.NonVolatileMethod();
        }
    }
}";

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void NoErrorFromVolatileUsedWithinOwnClass()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {   
        [UmbracoVolatile]
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

        public void NonVolatileMethod()
        {
            VolatileMethod();
        }

    }
}";

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void VolatileClassNoErrorFromSelf()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class DemoClass
    {   
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

        public void StillVolatile()
        {
            VolatileMethod();
        }

    }
}";

            VerifyCSharpDiagnostic(code);
        }


        [TestMethod]
        public void NoErrorFromInherentedNonVolatileParrent()
        {
            const string code = @"
namespace VolatileDemo
{
    public class NonVolatileParrent
    {
        public void NonVolatileMethod()
        {

        }
    }

    [UmbracoVolatile]
    public class DemoClass : NonVolatileParrent
    {   
        
        public void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

    }

    public class Consumer
    {
        public void testMethod()
        {
            var testClass = new DemoClass();
            testClass.NonVolatileMethod();    
        }
    }
}";

            VerifyCSharpDiagnostic(code);
        }
    }
}
