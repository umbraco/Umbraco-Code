using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbraco.Code.MapAll;
using Umbraco.Code.Tests.Verifiers;

namespace Umbraco.Code.Tests
{
    // FIXME  work-in-progress
    // see analyzer: what-if we enter some complex nested block?
    //  would need "not all code paths etc" = TODO later

    [TestClass]
    public class MapAllTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MapAllAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new MapAllFixProvider();
        }

        [TestMethod]
        public void NoErrorOnEmptyClass()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        public void Map(Thing source, Thing target)
        {
        }
    }
}
";

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ErrorOnOneMissingAssignment()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
        public int Value1 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        public void Map(Thing source, Thing target)
        {
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map property Value1.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void NoErrorIfNoComment()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
        public int Value1 { get; set; }
    }

    public class Mapper
    {
        public void Map(Thing source, Thing target)
        {
        }
    }
}
";

            VerifyCSharpDiagnostic(code);
        }

        [TestMethod]
        public void ErrorOnTwoMissingAssignments()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        public void Map(Thing source, Thing target)
        {
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map properties Value1, Value2.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void DetectMemberAssignment()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        public void Map(Thing source, Thing target)
        {
            target.Value1 = source.Value1;
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map property Value2.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void MustSetActualTargetProprety()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        public void Map(Thing source, Thing target)
        {
            var thing = new Thing();
            thing.Value1 = 33;
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map properties Value1, Value2.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void CanExcludeMember()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll -Value2
        public void Map(Thing source, Thing target)
        {
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map property Value1.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void CanExcludeMembers()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll -Value2 -Value3
        public void Map(Thing source, Thing target)
        {
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map property Value1.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void CanFix()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing1
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    public class Thing2
    {
        public int Value1 { get; set; }
        public int Value2 { get; private set; }
        public int Value3 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        // Umbraco.Code.MapAll woot
        public void Map(Thing1 source, Thing2 target)
        {
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map properties Value1, Value3.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);

            const string fixedCode = @"
namespace Umbraco.Code.Tests.TestData
{
    public class Thing1
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }

    public class Thing2
    {
        public int Value1 { get; set; }
        public int Value2 { get; private set; }
        public int Value3 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        // Umbraco.Code.MapAll woot
        public void Map(Thing1 source, Thing2 target)
        {
            target.Value1 = source.Value1;
            target.Value3 = default; // fixme
        }
    }
}
";

            VerifyCSharpFix(code, fixedCode);
        }

        [TestMethod]
        public void CanFixWithInterface()
        {
            const string code = @"
namespace Umbraco.Code.Tests.TestData
{
    public interface IThing
    {
        int Value { get; set; }
    }

    public interface IThing1 : IThing
    {
        int Value1 { get; set; }
    }

    public interface IThing2 : IThing
    {
        int Value1 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        public void Map(IThing1 source, IThing2 target)
        {
        }
    }
}
";

            var expected = new DiagnosticResult
            {
                Id = "UmbracoCodeMapAll",
                Message = "Method does not map properties Value, Value1.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 22, 21) }
            };

            VerifyCSharpDiagnostic(code, expected);

            const string fixedCode = @"
namespace Umbraco.Code.Tests.TestData
{
    public interface IThing
    {
        int Value { get; set; }
    }

    public interface IThing1 : IThing
    {
        int Value1 { get; set; }
    }

    public interface IThing2 : IThing
    {
        int Value1 { get; set; }
    }

    public class Mapper
    {
        // Umbraco.Code.MapAll
        public void Map(IThing1 source, IThing2 target)
        {
            target.Value = source.Value;
            target.Value1 = source.Value1;
        }
    }
}
";

            VerifyCSharpFix(code, fixedCode);
        }

        [TestMethod]
        public void CanParseComment()
        {
            List<string> excludes = null;
            Assert.IsFalse(CommentLineParser.ParseCommentLine("//", ref excludes));
            Assert.IsNull(excludes);
            Assert.IsFalse(CommentLineParser.ParseCommentLine("// ", ref excludes));
            Assert.IsNull(excludes);
            Assert.IsFalse(CommentLineParser.ParseCommentLine("// foo", ref excludes));
            Assert.IsNull(excludes);
            Assert.IsFalse(CommentLineParser.ParseCommentLine("// -foo", ref excludes));
            Assert.IsNull(excludes);
            Assert.IsTrue(CommentLineParser.ParseCommentLine("// Umbraco.Code.MapAll", ref excludes));
            Assert.IsNull(excludes);
            Assert.IsTrue(CommentLineParser.ParseCommentLine("// Umbraco.Code.MapAll foo ", ref excludes));
            Assert.IsNull(excludes);
            Assert.IsTrue(CommentLineParser.ParseCommentLine("// Umbraco.Code.MapAll -Foo", ref excludes));
            Assert.IsNotNull(excludes);
            Assert.AreEqual(1, excludes.Count);
            Assert.AreEqual("Foo", excludes[0]);
            excludes.Clear();
            Assert.IsTrue(CommentLineParser.ParseCommentLine("// Umbraco.Code.MapAll -Foo -Bar", ref excludes));
            Assert.IsNotNull(excludes);
            Assert.AreEqual(2, excludes.Count);
            Assert.AreEqual("Foo", excludes[0]);
            Assert.AreEqual("Bar", excludes[1]);
            excludes.Clear();
        }
    }

    // use: View > OtherWindows > Syntax Visualizer
    public class SampleCode
    {
        public class SampleClass
        {
            public string Value { get; set; }
        }

        // Umbraco.Code.MapAll
        public void Map(SampleClass source, SampleClass target)
        {
            target.Value = source.Value;
            target.Value = default; // fixme
        }
    }
}
