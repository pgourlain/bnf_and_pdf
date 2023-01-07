using Castle.Components.DictionaryAdapter.Xml;
using Irony.Parsing;
using Moq;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]

    public class VisitorTests : BaseTests
    {
        [Fact]
        public void TestValidFiles()
        {
            var res = ParseText("SET BRUSH black");
            var mock = new Mock<IPdfDocumentDrawer>();
            mock.SetupProperty(x => x.CurrentBrush);
            new PdfDrawerVisitor().Draw(mock.Object, res);

            Assert.Equal(XColors.Black, ((XSolidBrush)mock.Object.CurrentBrush).Color);
        }

        [Theory]
        [InlineData("SET VAR X=-2+3*2+5/2-3;", -2 + 3 * 2 + 5 / 2.0 - 3)]
        [InlineData("SET VAR X=-(2);", -2.0)]
        [InlineData("SET VAR X=\"coucou3\"+(2);", "coucou32")]
        [InlineData("SET VAR X=\"3\"+(2);", 5.0)]
        [InlineData("SET VAR X=\"3\"+(Random()+Random(1,2,3));", 9.0)]
        [InlineData("SET VAR A=2;SET VAR B=3;SET VAR X=Sum($A*$A, $B*$B,Sum(1,2,3));", 19.0)]
        public void TestFormulaEvaluator(string input, object expected)
        {
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.RegisterFormulaFunction("RANDOM", (args) => args.Sum(x => Convert.ToDouble(x)));
            visitor.RegisterFormulaFunction("Sum", (args) => args.Sum(x => Convert.ToDouble(x)));
            visitor.Draw(mock.Object, res);
            Assert.Equal(expected, visitor.Vars["X"]);
        }


        [Theory]

        [InlineData("SET FONT Name=getFontName() Size=12;","Arial", 12)]
        public void TestFormulaEvaluatorWithFontName(string input, object expected, int size)
        {
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            mock.SetupProperty(x => x.CurrentFont, new XFont("Consolas", 8));

            var visitor = new PdfDrawerForTestsVisitor();
            visitor.RegisterFormulaFunction("getFontName", (_) => expected);
            var drawer = mock.Object;
            visitor.Draw(drawer, res);
            var f = drawer.CurrentFont;
            //because font names change on different OS
            var pi = typeof(XFont).GetProperty("FamilyName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.StartsWith((string)expected, (string)pi.GetValue(drawer.CurrentFont)!);
            Assert.Equal(size, drawer.CurrentFont.Size);
        }


        [Theory]
        [InlineData("pdf1-custom-udfs.txt")]
        public void TestCustomUdfEvaluator(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.Draw(mock.Object, res);
            Assert.Equal(10, visitor.UDFs.Count);
            foreach (var udFs in visitor.UDFs)
            {
                var n = int.Parse(udFs.Key[6..]);
                //check that number arguments is equal
                Assert.Equal(n, udFs.Value.Length);

            }
        }

        [Theory]
        [InlineData("pdf1-conditions.txt")]
        public void TestConditionEvaluator(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();

            visitor.Draw(mock.Object, res);
            foreach (var udF in visitor.UDFs)
            {
                Assert.Equal("OK", udF.Value[0]);
            }
        }
    }
}
