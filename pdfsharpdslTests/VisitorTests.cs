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
        [InlineData("SET VAR X=\"3\"+(Random()+Random(1,2,3));", 3.0)]
        public void TestFormulaEvaluator(string input, object expected)
        {
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.RegisterFormulaFunction("RANDOM", (_) => 0.0);
            visitor.Draw(mock.Object, res);
            Assert.Equal(expected, visitor.Vars["X"]);
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
            foreach (var udFs in visitor.UDFs )
            {
                var n = int.Parse(udFs.Key[6..]);
                //check that number arguments is equal
                Assert.Equal(n, udFs.Value.Length);

            }
        }
    }
}
