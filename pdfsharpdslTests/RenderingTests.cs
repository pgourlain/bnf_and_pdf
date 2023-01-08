using PdfSharpCore.Pdf;
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
    public class RenderingTests : BaseTests
    {
        [Theory]
        [InlineData("title")]
        [InlineData("linetext")]
        public void UnitRenderingTests(string file)
        {
            var input = File.ReadAllText($"./RenderingFiles/{file}.txt");
            var expected = File.ReadAllText($"./RenderingFiles/{file}-expected.txt");

            var parsingResult = ParseText(input);
            var drawer = new TextDocumentDrawer();
            new PdfDrawerVisitor().Draw(drawer, parsingResult);
            Assert.Equal(expected, drawer.OutputRendering.ToString());

        }
    }
}
