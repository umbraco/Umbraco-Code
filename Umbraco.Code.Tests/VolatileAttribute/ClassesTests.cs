using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class ClassesTests : CodeFixVerifier
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
            // However, if you try to access a method from the volatile base class you get the error
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
        
        [TestMethod]
        public void EnsureWarningWhenSuppressedInstantiating()
        {
            const string code = @"
[assembly: UmbracoSuppressVolatile]
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
                Severity = DiagnosticSeverity.Warning,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 13, 29) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void EnsureWarningWhenSuppressedInheriting()
        {
            const string code = @"
[assembly: UmbracoSuppressVolatile]
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
                Severity = DiagnosticSeverity.Warning,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 9, 5) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ImplementingVolatileInterface()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public interface IVolatileInterface
    { }
    
    public class ImplementingClass : IVolatileInterface
    { }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.IVolatileInterface is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 8, 5) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ImplementingTwoVolatileInterfaces()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public interface IFirstVolatileInterface
    { }

    [UmbracoVolatile]
    public interface ISecondVolatileInterface
    { }
    
    public class ImplementingClass : IFirstVolatileInterface, ISecondVolatileInterface
    { }
}
";
            var expected = new []
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.IFirstVolatileInterface is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 12, 5) }
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.ISecondVolatileInterface is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 12, 5) }
                }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ImplementingVolatileAndNonVolatileInterface()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public interface IVolatileInterface
    { }

    public interface INonVolatileInterface
    { }
    
    public class ImplementingClass : IVolatileInterface, INonVolatileInterface
    { }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.IVolatileInterface is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 11, 5) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ImplementingVolatileInterfaceAndVolatileClass()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public interface IVolatileInterface
    { }

    [UmbracoVolatile]
    public class VolatileClass
    { }
    
    public class ImplementingClass : VolatileClass, IVolatileInterface
    { }
}
";
            var expected = new []
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.IVolatileInterface is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 12, 5) }
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.VolatileClass is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new []{ new DiagnosticResultLocation("Test0.cs", 12, 5) }
                }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ImplementingNonVolatileInterface()
        {
            const string code = @"
namespace VolatileDemo
{
    public interface INonVolatileInterface
    { }
    
    public class ImplementingClass : INonVolatileInterface
    { }
}
";

            VerifyCSharpDiagnostic(code);
        }
    }
}