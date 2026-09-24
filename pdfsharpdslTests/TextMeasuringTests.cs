using PdfSharpDslCore.Drawing;
using System.Diagnostics.CodeAnalysis;
using TerraPDF.Core;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class TextMeasuringTests : BaseTests
    {
        [Fact]
        public void TextWidthMatchesVectorCanvasMeasurement()
        {
            var tree = ParseText("SET VAR W=TextWidth(\"abc\");");
            using var drawer = new PdfDocumentDrawer();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.Draw(drawer, tree);

            var expected = VectorCanvas.MeasureTextWidth("abc", 10, "Helvetica", false, false);
            Assert.Equal(expected, visitor.Vars["W"]);
        }

        [Fact]
        public void TextHeightWithMaxWidthWrapsToMultipleLines()
        {
            var tree = ParseText(
                "SET VAR H1=TextHeight(\"a\");" +
                "SET VAR H2=TextHeight(\"this is a fairly long line of text that should wrap\",60);");
            using var drawer = new PdfDocumentDrawer();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.Draw(drawer, tree);

            Assert.True((double)visitor.Vars["H2"]! > (double)visitor.Vars["H1"]!);
        }
    }
}
