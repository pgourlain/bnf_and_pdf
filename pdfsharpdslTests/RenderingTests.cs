using PdfSharpCore.Pdf;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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

        [Fact]
        public void UseFontWithVariables()
        {
            var input = @"
            SET VAR FONTCOUNT = GetFontCount();

            FOR I = 0 TO $FONTCOUNT DO
            SET VAR FNAME = GetFont($I);
            SET FONT Name =$FNAME Size = 12;
            LINETEXT 10,100 + 50 *$I HAlign = left VAlign = bottom Text = ""Test font :"" + $FNAME;
            ENDFOR
";
            var parsingResult = ParseText(input);
            var drawer = new TextDocumentDrawer();
            var visitor = new PdfDrawerVisitor();
            visitor.RegisterFormulaFunction("GetFontCount", (_) => 1);
            visitor.RegisterFormulaFunction("GetFont", (_) => "Arial");

            visitor.Draw(drawer, parsingResult);
            //should not failed
        }
    }
}
