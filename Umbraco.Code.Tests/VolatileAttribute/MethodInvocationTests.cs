﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class MethodInvocationTests : CodeFixVerifier
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

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 18, 29) }
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 19, 13) }
                }
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

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass is volatile",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 19, 29) }
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ErrorFromInheritedVolatile()
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

    public class InheritedVolatile : DemoClass
    {

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new InheritedVolatile();
            testClass.VolatileMethod();
        }
    }
}";

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 14, 5) }
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 24, 13) }
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ErrorFromInheritedVolatileOnSelf()
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

    public class InheritedVolatile : DemoClass
    {

    }

    public class DemoClass2 : InheritedVolatile
    {
        public void Test()
        {
            VolatileMethod();
        }
    }
}";

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 14, 5) }
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] { new DiagnosticResultLocation("Test0.cs", 23, 13) }
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }


        [TestMethod]
        public void ErrorFromInheritedVolatileMethod()
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

    public class InheritedVolatile : DemoClass
    {

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new InheritedVolatile();
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
            Console.WriteLine(""No Danger"");
        }

    }

    public class InheritedVolatile : DemoClass
    {

    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new InheritedVolatile();
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
        public void VolatileMethodInConstructor()
        {
            // Just making sure that a constructor is also considered a method symbol.
            const string code = @"
namespace VolatileDemo
{
    public static class DemoClass
    {
        [UmbracoVolatile]
        public static void VolatileMethod()
        {
            Console.WriteLine(""!!!Danger to manifold!!!"");
        }

    }

    public class DemoClass2
    {
        public DemoClass2()
        {
            DemoClass.VolatileMethod();
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass.VolatileMethod() is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 13) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void InvokingMethodFromVolatileInterface()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public interface IVolatileInterface
    {
        public void SomeMethod();
    }
    
    public class ImplementingClass : IVolatileInterface
    {
        public void SomeMethod()
        {} 
    }

    public class UsingClass
    {
        public void TestMethod()
        {
            IVolatileInterface implementingClass = new ImplementingClass();
            implementingClass.SomeMethod();
        }
    }
}
";
            
            var expected = new []
            {
                // This error is from implementing the interface
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.IVolatileInterface is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 10, 5) }
                },
                // This error is from using the method defined in the interface
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.IVolatileInterface.SomeMethod() is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 21, 13) }
                }, 
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
    }
}
