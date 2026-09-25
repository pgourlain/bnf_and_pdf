using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class StyleTests : BaseTests
    {
        private static Mock<IPdfDocumentDrawer> NewDrawer()
        {
            var mock = new Mock<IPdfDocumentDrawer>();
            mock.SetupProperty(x => x.CurrentBrush);
            mock.SetupProperty(x => x.CurrentFont, new PdfFont("Consolas", 8));
            mock.SetupProperty(x => x.CurrentPen, new PdfPen(PdfColor.Black, 1));
            mock.SetupProperty(x => x.HighlightBrush);
            return mock;
        }

        [Fact]
        public void UseReplaysFontBrushAndPenOfTheStyle()
        {
            var tree = ParseText(
                "STYLE h1 SET FONT Name=\"Arial\" Size=20 bold; SET BRUSH darkblue; SET PEN red 2; ENDSTYLE " +
                "USE h1;");
            var drawer = NewDrawer();

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            Assert.Equal(20, drawer.Object.CurrentFont.Size);
            Assert.Equal(PdfFontStyle.Bold, drawer.Object.CurrentFont.Style);
            Assert.Equal(PdfColors.FromName("darkblue"), drawer.Object.CurrentBrush.Color);
            Assert.Equal(2, drawer.Object.CurrentPen.Width);
        }

        [Fact]
        public void StyleCanBeUsedBeforeItsDefinition()
        {
            var tree = ParseText("USE later; STYLE later SET BRUSH red; ENDSTYLE");
            var drawer = NewDrawer();

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            Assert.Equal(PdfColors.FromName("red"), drawer.Object.CurrentBrush.Color);
        }

        [Fact]
        public void StyleDefinitionAloneChangesNothing()
        {
            var tree = ParseText("STYLE s SET BRUSH red; ENDSTYLE");
            var drawer = NewDrawer();
            drawer.Object.CurrentBrush = new PdfBrush(PdfColor.Black);

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            Assert.Equal(PdfColor.Black, drawer.Object.CurrentBrush.Color);
        }

        [Fact]
        public void UnknownStyleThrowsWithSuggestion()
        {
            var tree = ParseText("STYLE title SET BRUSH red; ENDSTYLE USE titel;");

            var ex = Assert.Throws<PdfParserException>(() => new PdfDrawerVisitor().Draw(NewDrawer().Object, tree));

            Assert.StartsWith("Unknown style 'titel'", ex.Message);
            Assert.EndsWith("Did you mean 'title'?", ex.Message);
        }

        [Fact]
        public void DuplicateStyleThrows()
        {
            var tree = ParseText("STYLE s SET BRUSH red; ENDSTYLE STYLE s SET BRUSH blue; ENDSTYLE");

            Assert.Throws<PdfParserException>(() => new PdfDrawerVisitor().Draw(NewDrawer().Object, tree));
        }

        [Fact]
        public void SetVarIsNotAllowedInAStyle()
        {
            var tree = CreateParser().Parse("STYLE s SET VAR X=1; ENDSTYLE");

            Assert.True(tree.HasErrors());
        }
    }
}
