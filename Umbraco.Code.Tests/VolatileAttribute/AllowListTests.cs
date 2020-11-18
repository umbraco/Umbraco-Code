using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.Tests.Verifiers;
using Umbraco.Code.Volatile;

namespace Umbraco.Code.Tests.VolatileAttribute
{
    [TestClass]
    public class AllowListTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UmbracoVolatileAnalyzer();
        }

        [TestInitialize]
        public void SetProjectNameToAllowedProject()
        {
            TestProjectName = "Umbraco.Core";
        }

        [TestCleanup]
        public void SetProjectNameToDefault()
        {
            TestProjectName = "TestProject";
        }
        

        [TestMethod]
        public void MethodsIgnoresAllowedProject()
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

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ObjectConstructionIgnoresAllowedProject()
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
            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ObjectInheritanceIgnoresAllowedProject()
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
            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void InterfaceImplementationIgnoresAllowedProject()
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
            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void AttributeApplicationIgnoresAllowedProject()
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
            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void MemberAccessIgnoresAllowedProject()
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
            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ParametersIgnoresAllowedProject()
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
            VerifyCSharpDiagnostic(code);
        }
    }
}