using Moq;
using PdfSharpDslCore.Drawing;
using System.Diagnostics.CodeAnalysis;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class BuiltInFunctionsTests : BaseTests
    {
        private object? EvaluateX(string formula)
        {
            var tree = ParseText($"SET VAR X={formula};");
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.Draw(mock.Object, tree);
            return visitor.Vars["X"];
        }

        [Theory]
        [InlineData("Min(3,1,2)", 1.0)]
        [InlineData("Max(3,1,2)", 3.0)]
        [InlineData("Sum(1,2,3)", 6.0)]
        [InlineData("Abs(-4.5)", 4.5)]
        [InlineData("Round(1.25,1)", 1.3)]
        [InlineData("Round(1.5)", 2.0)]
        [InlineData("Floor(1.9)", 1.0)]
        [InlineData("Ceil(1.1)", 2.0)]
        [InlineData("Sqrt(9)", 3.0)]
        [InlineData("Pow(2,3)", 8.0)]
        public void MathFunctions(string formula, double expected) => Assert.Equal(expected, EvaluateX(formula));

        [Theory]
        [InlineData("Upper(\"abc\")", "ABC")]
        [InlineData("Lower(\"ABC\")", "abc")]
        [InlineData("Len(\"abcde\")", 5.0)]
        [InlineData("Substr(\"abcdef\",2)", "cdef")]
        [InlineData("Substr(\"abcdef\",2,2)", "cd")]
        [InlineData("Replace(\"abcabc\",\"a\",\"X\")", "XbcXbc")]
        [InlineData("Trim(\"  abc  \")", "abc")]
        public void StringFunctions(string formula, object expected) => Assert.Equal(expected, EvaluateX(formula));

        [Fact]
        public void FormatFunction()
        {
            Assert.Equal("1,284.50", EvaluateX("Format(1284.5,\"N2\")"));
        }

        [Fact]
        public void DateFunctionsAreUsableThroughFormat()
        {
            Assert.Equal(System.DateTime.Today.ToString("yyyy-MM-dd"), EvaluateX("Format(Today(),\"yyyy-MM-dd\")"));
            Assert.Equal(System.DateTime.Now.ToString("yyyy-MM-dd"), EvaluateX("Format(Now(),\"yyyy-MM-dd\")"));
        }

        [Theory]
        [InlineData("Iif(1>0,\"yes\",\"no\")", "yes")]
        [InlineData("Iif(1<0,\"yes\",\"no\")", "no")]
        public void LogicFunctions(string formula, object expected) => Assert.Equal(expected, EvaluateX(formula));

        [Fact]
        public void BadArityThrowsPdfParserException()
        {
            Assert.Throws<PdfSharpDslCore.Parser.PdfParserException>(() => EvaluateX("Abs(1,2)"));
        }

        [Fact]
        public void HostRegisteredFunctionOverridesBuiltIn()
        {
            var tree = ParseText("SET VAR X=Sum(1,2,3);");
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.RegisterFormulaFunction("Sum", args => 42.0);
            visitor.Draw(mock.Object, tree);
            Assert.Equal(42.0, visitor.Vars["X"]);
        }
    }
}
