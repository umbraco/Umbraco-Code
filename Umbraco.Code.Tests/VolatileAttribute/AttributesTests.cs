using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class AttributesTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UmbracoVolatileAnalyzer();
        }

        [TestMethod]
        public void ApplyAVolatileAttributeToAClass()
        {
            const string code = @"
using System;
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class TestAttribute : Attribute
    {}

    [Test]
    public class DemoClass2
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.TestAttribute is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 9, 5) }
            };
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ApplyAVolatileAttributeToAMethod()
        {
            const string code = @"
using System;
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class TestAttribute : Attribute
    {}

    public class DemoClass2
    {
        [Test]
        public void SomeMethod()
        {}
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.TestAttribute is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 11, 9) }
            };
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ApplyAVolatileAttributeToAField()
        {
            const string code = @"
using System;
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class TestAttribute : Attribute
    {}

    public class DemoClass2
    {
        [Test]
        public string SomeField = ""This is a field"";
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.TestAttribute is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 11, 9) }
            };
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ApplyAVolatileAttributeToAProperty()
        {
            const string code = @"
using System;
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class TestAttribute : Attribute
    {}

    public class DemoClass2
    {
        [Test]
        public string SomeField => ""Test"";
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.TestAttribute is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 11, 9) }
            };
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ApplyANonVolatileAttributeToAClass()
        {
            const string code = @"
using System;
namespace VolatileDemo
{
    public class TestAttribute : Attribute
    {}

    [Test]
    public class DemoClass2
    {
    }
}
";
            VerifyCSharpDiagnostic(code);
        }
        
        [TestMethod]
        public void EnsureWarningWhenSuppressed()
        {
            const string code = @"
using System;
[assembly: UmbracoSuppressVolatile]
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class TestAttribute : Attribute
    {}

    [Test]
    public class DemoClass2
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.TestAttribute is volatile",
                Severity = DiagnosticSeverity.Warning,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 10, 5) }
            };
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void AttributeListWithMultipleAttributes()
        {
            const string code = @"
using System;
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class VolatileTestAttribute : Attribute
    {}

    public class NonVolatileTestAttribute : Attribute
    {}

    [NonVolatileTest, VolatileTest]
    public class DemoClass2
    {}
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.VolatileTestAttribute is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 12, 5) }
            };
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void ApplyAVolatileAttributeToAssembly()
        {
            // This test is a bit weird, because we have to use two code files
            // If we try to define the attribute and apply it to the assembly in the same file, 
            // the attribute won't exist when we try to apply it and therefore we can't convert it into an ISymbol
            // In the code.
            
            const string attributeCode = @"
using System;
namespace AttributeSpace{
    [UmbracoVolatile]
    public class TestAttribute : Attribute
    {}
}
";
            const string code = @"
using AttributeSpace;
[assembly: Test]
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "AttributeSpace.TestAttribute is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test1.cs", 3, 1) }
            };
            VerifyCSharpDiagnostic(new []{attributeCode, code}, expected);
        }
    }
}