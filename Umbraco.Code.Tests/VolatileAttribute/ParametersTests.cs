using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class ParametersTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UmbracoVolatileAnalyzer();
        }
        
        [TestMethod]
        public void RequestVolatileClassAsParameter()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class DemoClass
    {}

    public class DemoClass2
    {
        public void Test(DemoClass volatileClass)
        {
        }
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 10, 26) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void EnsureWarningWhenSuppressedVolatileClassAsParameter()
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
        public void Test(DemoClass volatileClass)
        {
        }
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass is volatile",
                Severity = DiagnosticSeverity.Warning,
                Locations = new []{ new DiagnosticResultLocation("Test0.cs", 11, 26) }
            };
            
            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void PassVolatileEnumToMethod()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public enum TestEnum
    {
        Test,
        AnotherTest
    }

    public class DemoClass
    {
        public void MethodReceivingEnum(TestEnum testEnum)
        {
        }

        public void MethodPassingEnum()
        {
            MethodReceivingEnum(TestEnum.Test);
        }
    }
}
";

            var expected = new []
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.TestEnum is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 13, 41)}
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.TestEnum.Test is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 19, 33)}
                } 
            };
            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void VolatileClassWithEnumAsParameter()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public class VolatileClassWithEnum
    {
        public enum VolatileEnum
        {
            EntryOne,
            EntryTwo
        }
    }

    public class ClassConsumingEnum
    {
        public void MethodConsumingEnum(VolatileClassWithEnum.VolatileEnum volatileEnum)
        {
        }

        public void MethodPassingEnum()
        {
            MethodConsumingEnum(VolatileClassWithEnum.VolatileEnum.EntryOne);
        }
    }
}
";

            var expected = new[]
            {
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.VolatileClassWithEnum.VolatileEnum is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 16, 41)}
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.VolatileClassWithEnum.VolatileEnum.EntryOne is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 22, 33)}
                }
            };
            VerifyCSharpDiagnostic(code, expected);
        }
    }
}