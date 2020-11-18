using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class MemberAccess : CodeFixVerifier
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
            testClass.VolatileProperty = ""Volatile text"";
            var test = testClass.VolatileProperty;
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
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 15, 13)}
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 16, 24)}
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void EnsureWarnWhenSuppressed()
        {
            const string code = @"
[assembly: UmbracoSuppressVolatile]
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
            testClass.VolatileProperty = ""Volatile text"";
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", 16, 13)}
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
            testClass.VolatileProperty = ""Volatile text"";
            var test = testClass.VolatileProperty;
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
        public string VolatileProperty = ""Field text"";
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            testClass.VolatileProperty = ""Volatile text"";
            var test = testClass.VolatileProperty;
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
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 15, 13)}
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 16, 24)}
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void GetSetNonVolatileField()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        public string VolatileProperty = ""Field text"";
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            testClass.VolatileProperty = ""Volatile text"";
            var test = testClass.VolatileProperty;
        }
    }
}
";
            VerifyCSharpDiagnostic(code);
        }
        
        [TestMethod]
        public void GetSetPropertyFromVolatileClass()
        {
            const string code = @"
namespace VolatileDemo
{
    [UmbracoVolatile]
    public static class DemoClass
    { 
        public static string VolatileProperty {get; set;}
    }

    public class DemoClass2
    {
        public void Test()
        {
            DemoClass.VolatileProperty = ""Volatile text"";
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
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 14, 13)}
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 15, 24)}
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void GetSetVolatilePropertyFromOwnClass()
        {
            const string code = @"
namespace VolatileDemo
{
    public class DemoClass
    {
        [UmbracoVolatile]
        public string VolatileProperty {get; set;}

        public void Test()
        {
            this.VolatileProperty = ""Volatile text"";
            var test = this.VolatileProperty;
        }
    }
}
";
            VerifyCSharpDiagnostic(code);
        }
        
        [TestMethod]
        public void GetSetVolatileFieldWithClass()
        {
            const string code = @"
namespace VolatileDemo
{
    public class PropertyClass{}

    public class DemoClass
    {
        [UmbracoVolatile]
        public PropertyClass VolatileProperty;
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            testClass.VolatileProperty = new PropertyClass();
            var test = testClass.VolatileProperty;
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
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 17, 13)}
                },
                new DiagnosticResult
                {
                    Id = "UmbracoCodeVolatile",
                    Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                    Severity = DiagnosticSeverity.Error,
                    Locations = new[] {new DiagnosticResultLocation("Test0.cs", 18, 24)}
                }
            };

            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void GetVolatileExpressionBodyPropertyWithClass()
        {
            const string code = @"
namespace VolatileDemo
{
    public class PropertyClass{}

    public class DemoClass
    {
        [UmbracoVolatile]
        public PropertyClass VolatileProperty => new PropertyClass();
    }

    public class DemoClass2
    {
        public void Test()
        {
            var testClass = new DemoClass();
            var test = testClass.VolatileProperty;
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.DemoClass.VolatileProperty is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", 17, 24)}
            };

            VerifyCSharpDiagnostic(code, expected);
        }
        
        [TestMethod]
        public void SaveVolatileEnumToVariable()
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

    public class DemoClass2
    {
        public void Test()
        {
            var testState = TestEnum.Test;
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeVolatile",
                Message = "VolatileDemo.TestEnum.Test is volatile",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", 15, 29)}
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
        public void VolatileClassWithEnum()
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