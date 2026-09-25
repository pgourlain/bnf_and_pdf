using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Drawing.Charts;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class ChartTests : BaseTests
    {
        private sealed class Recorded
        {
            public List<(double X, double Y, double W, double H, PdfColor Fill)> Rects { get; } = new();
            public List<(double Start, double Sweep, PdfColor Fill)> Pies { get; } = new();
            public List<(double X1, double Y1, double X2, double Y2)> Lines { get; } = new();
            public List<string> Texts { get; } = new();
            public int Ellipses;
        }

        private static (Mock<IPdfDocumentDrawer> Drawer, Recorded Calls) NewDrawer()
        {
            var mock = new Mock<IPdfDocumentDrawer>();
            var calls = new Recorded();
            mock.SetupProperty(d => d.CurrentBrush, new PdfBrush(PdfColor.Black));
            mock.SetupProperty(d => d.CurrentPen, new PdfPen(PdfColor.Black, 1));
            mock.SetupProperty(d => d.CurrentFont, new PdfFont("Arial", 10, PdfFontStyle.Bold));
            mock.SetupProperty(d => d.HighlightBrush);
            mock.Setup(d => d.MeasureText(It.IsAny<string>(), It.IsAny<double?>())).Returns<string, double?>((t, _) => new PdfSize(t.Length * 4, 10));
            mock.Setup(d => d.DrawRect(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), true))
                .Callback<double, double, double, double, bool>((x, y, w, h, _) => calls.Rects.Add((x, y, w, h, mock.Object.CurrentBrush.Color)));
            mock.Setup(d => d.DrawPie(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double>(), It.IsAny<double>(), true))
                .Callback<double, double, double?, double?, double, double, bool>((_, _, _, _, s, sw, _) => calls.Pies.Add((s, sw, mock.Object.CurrentBrush.Color)));
            mock.Setup(d => d.DrawLine(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                .Callback<double, double, double, double>((a, b, c, d2) => calls.Lines.Add((a, b, c, d2)));
            mock.Setup(d => d.DrawEllipse(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), true))
                .Callback(() => calls.Ellipses++);
            mock.Setup(d => d.DrawLineText(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double?>(),
                    It.IsAny<PdfHorizontalAlignment>(), It.IsAny<PdfVerticalAlignment>(), It.IsAny<TextOrientation>(), It.IsAny<TextFitOptions?>()))
                .Callback<string, double, double, double?, double?, PdfHorizontalAlignment, PdfVerticalAlignment, TextOrientation, TextFitOptions?>(
                    (t, _, _, _, _, _, _, _, _) => calls.Texts.Add(t));
            return (mock, calls);
        }

        private (Mock<IPdfDocumentDrawer> Drawer, Recorded Calls) Run(string source)
        {
            var tree = ParseText(source);
            var (drawer, calls) = NewDrawer();
            new PdfDrawerVisitor().Draw(drawer.Object, tree);
            return (drawer, calls);
        }

        [Fact]
        public void ChartParses()
        {
            ParseText("CHART bar 40,100,300,200 Data=[12,30,18] Labels=[\"Q1\",\"Q2\",\"Q3\"] Colors=[steelblue];");
            ParseText("CHART pie 380,100,150,150 Data=[1,2];");
            ParseText("CHART line 0,0,100,100 Data=\"1,2,3\" Labels=$L Colors=[\"red\",\"#00FF00\"];");
        }

        [Fact]
        public void BarChartDrawsOneBarPerValueInItsColor()
        {
            var (_, calls) = Run("CHART bar 40,100,300,200 Data=[12,30,18] Colors=[steelblue, tomato, red];");

            Assert.Equal(3, calls.Rects.Count);
            Assert.Equal(new[] { PdfColors.FromName("steelblue"), PdfColors.FromName("tomato"), PdfColors.FromName("red") },
                calls.Rects.Select(r => r.Fill).ToArray());
            // heights follow the values
            Assert.True(calls.Rects[1].H > calls.Rects[2].H && calls.Rects[2].H > calls.Rects[0].H);
            Assert.Equal(30.0 / 12, calls.Rects[1].H / calls.Rects[0].H, 3);
            // bars stay inside the chart rectangle
            Assert.All(calls.Rects, r =>
            {
                Assert.InRange(r.X, 40, 340);
                Assert.InRange(r.X + r.W, 40, 340);
                Assert.InRange(r.Y, 100, 300);
                Assert.InRange(r.Y + r.H, 100, 300);
            });
        }

        [Fact]
        public void BarChartLabelsAxisAndValues()
        {
            var (_, calls) = Run("CHART bar 40,100,300,200 Data=[12,30,18] Labels=[\"Q1\",\"Q2\",\"Q3\"];");

            Assert.Contains("Q1", calls.Texts);
            Assert.Contains("Q3", calls.Texts);
            // value above each bar
            Assert.Contains("12", calls.Texts);
            Assert.Contains("30", calls.Texts);
            // axis ticks 0..30 by 10
            Assert.Contains("0", calls.Texts);
            Assert.Contains("10", calls.Texts);
            Assert.Contains("20", calls.Texts);
            // one grid line per tick
            Assert.Equal(4, calls.Lines.Count);
        }

        [Fact]
        public void DefaultColorsAreUsedWhenNoneIsGivenAndCycleWhenTooFew()
        {
            var (_, defaults) = Run("CHART bar 0,0,300,200 Data=[1,2,3];");
            var (_, cycled) = Run("CHART bar 0,0,300,200 Data=[1,2,3] Colors=[\"red\",\"blue\"];");

            Assert.Equal(ChartRenderer.DefaultPalette.Take(3), defaults.Rects.Select(r => r.Fill));
            Assert.Equal(new[] { "red", "blue", "red" }.Select(PdfColors.FromName), cycled.Rects.Select(r => r.Fill));
        }

        [Fact]
        public void NegativeValuesDrawBelowTheZeroLine()
        {
            var (_, calls) = Run("CHART bar 0,0,300,200 Data=[10,-10];");

            Assert.Equal(2, calls.Rects.Count);
            Assert.True(calls.Rects[1].Y > calls.Rects[0].Y);
            Assert.Equal(calls.Rects[0].Y + calls.Rects[0].H, calls.Rects[1].Y, 3);
            Assert.Contains("-10", calls.Texts);
        }

        [Fact]
        public void LineChartConnectsThePointsAndMarksThem()
        {
            var (_, calls) = Run("CHART line 0,0,300,200 Data=[5,10,7,12] Colors=[red];");

            Assert.Equal(4, calls.Ellipses);
            Assert.Empty(calls.Rects);
            // 4 ticks of grid (0,5,10,15 or similar) are also lines: at least the 3 segments are extra
            Assert.True(calls.Lines.Count >= 3 + 3);
        }

        [Fact]
        public void PieChartSweepsAddUpToOneTurnStartingAtTheTop()
        {
            var (_, calls) = Run("CHART pie 0,0,300,200 Data=[1,1,2] Colors=[red,green,blue];");

            Assert.Equal(3, calls.Pies.Count);
            Assert.Equal(-90, calls.Pies[0].Start);
            Assert.Equal(360.0, calls.Pies.Sum(p => p.Sweep), 6);
            Assert.Equal(new[] { 90.0, 90.0, 180.0 }, calls.Pies.Select(p => p.Sweep).ToArray());
            Assert.Equal(calls.Pies[0].Start + calls.Pies[0].Sweep, calls.Pies[1].Start, 6);
        }

        [Fact]
        public void PieChartLegendShowsLabelsAndPercentages()
        {
            var (_, calls) = Run("CHART pie 0,0,300,200 Data=[1,3] Labels=[\"Small\",\"Big\"];");

            Assert.Equal(new[] { "Small (25%)", "Big (75%)" }, calls.Texts);
            Assert.Equal(2, calls.Rects.Count);
        }

        [Fact]
        public void PieChartWithOneValueIsAWholeDisc()
        {
            var (_, calls) = Run("CHART pie 0,0,100,100 Data=[5];");

            Assert.Empty(calls.Pies);
            Assert.Equal(1, calls.Ellipses);
        }

        [Fact]
        public void PieChartIsASquareCenteredInItsRectangleWithoutLegend()
        {
            var (drawer, _) = Run("CHART pie 10,20,300,100 Data=[1,2];");

            drawer.Verify(d => d.DrawPie(110, 20, 100, 100, -90, It.IsAny<double>(), true), Times.Once);
        }

        [Fact]
        public void DataCanBeATextOrAVariable()
        {
            var (_, fromText) = Run("CHART bar 0,0,300,200 Data=\"1, 2,3\";");
            var (_, fromVar) = Run("SET VAR D=[4,5]; CHART bar 0,0,300,200 Data=$D;");

            Assert.Equal(3, fromText.Rects.Count);
            Assert.Equal(2, fromVar.Rects.Count);
        }

        [Fact]
        public void PenBrushAndFontAreRestoredAfterTheChart()
        {
            var (drawer, _) = Run("CHART bar 0,0,300,200 Data=[1,2,3] Labels=[\"a\",\"b\",\"c\"]; CHART pie 0,0,300,200 Data=[1,2];");

            Assert.Equal(PdfColor.Black, drawer.Object.CurrentBrush.Color);
            Assert.Equal(1, drawer.Object.CurrentPen.Width);
            Assert.Equal(10, drawer.Object.CurrentFont.Size);
            Assert.Equal(PdfFontStyle.Bold, drawer.Object.CurrentFont.Style);
        }

        [Fact]
        public void HexColorsAreAccepted()
        {
            var (_, calls) = Run("CHART bar 0,0,300,200 Data=[1] Colors=[\"#FF8000\"];");

            Assert.Equal(PdfColor.FromRgb(255, 128, 0), calls.Rects[0].Fill);
        }

        [Fact]
        public void UnknownColorSuggests()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("CHART bar 0,0,300,200 Data=[1] Colors=[\"steelblu\"];"));

            Assert.Contains("Unknown color 'steelblu'", ex.Message);
            Assert.Contains("Did you mean 'steelblue'?", ex.Message);
        }

        [Fact]
        public void NonNumericDataThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("CHART bar 0,0,300,200 Data=[1,\"x\"];"));

            Assert.StartsWith("CHART Data item 1 ('x') is not a number", ex.Message);
        }

        [Fact]
        public void EmptyDataTooSmallAndNegativePieThrow()
        {
            Assert.Throws<PdfParserException>(() => Run("CHART bar 0,0,300,200 Data=[];"));
            Assert.Throws<PdfParserException>(() => Run("CHART bar 0,0,30,20 Data=[1,2];"));
            Assert.Throws<PdfParserException>(() => Run("CHART pie 0,0,300,200 Data=[1,-2];"));
            Assert.Throws<PdfParserException>(() => Run("CHART pie 0,0,300,200 Data=[0,0];"));
        }

        [Fact]
        public void ChartNeedsARectangle()
        {
            Assert.True(CreateParser().Parse("CHART bar 0,0 Data=[1];").HasErrors());
            Assert.Throws<PdfParserException>(() => Run("CHART bar 0,0,0,100 Data=[1];"));
        }

        [Theory]
        [InlineData(0, 30, 0, 30, 10)]
        [InlineData(0, 12, 0, 15, 5)]
        [InlineData(0, 1, 0, 1, 0.5)]
        [InlineData(-10, 10, -10, 10, 5)]
        [InlineData(0, 0, 0, 1, 0.5)]
        [InlineData(0, 1000, 0, 1000, 500)]
        public void NiceRangeGivesRoundTicks(double min, double max, double expectedMin, double expectedMax, double expectedStep)
        {
            var (niceMin, niceMax, step) = ChartRenderer.NiceRange(min, max);

            Assert.Equal(expectedMin, niceMin, 6);
            Assert.Equal(expectedMax, niceMax, 6);
            Assert.Equal(expectedStep, step, 6);
        }

        [Fact]
        public void NothingIsDrawnAtANegativeCoordinateWhichWouldMeanTheBottomOfThePage()
        {
            // a chart at the very top of the page (or of a row template, y=0) must not put text at y < 0
            var (drawer, _) = NewDrawer();
            var texts = new List<double>();
            drawer.Setup(d => d.DrawLineText(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double?>(),
                    It.IsAny<PdfHorizontalAlignment>(), It.IsAny<PdfVerticalAlignment>(), It.IsAny<TextOrientation>(), It.IsAny<TextFitOptions?>()))
                .Callback<string, double, double, double?, double?, PdfHorizontalAlignment, PdfVerticalAlignment, TextOrientation, TextFitOptions?>(
                    (_, x, y, _, _, _, _, _, _) => { texts.Add(x); texts.Add(y); });

            ChartRenderer.Draw(drawer.Object, PdfChartType.Bar, 0, 0, 200, 100, new[] { 3.0, 1, 2 }, new[] { "a", "b", "c" }, null);

            Assert.All(texts, coordinate => Assert.True(coordinate >= 0, $"coordinate {coordinate} is negative"));
        }

        [Fact]
        public void ChartIsRenderedInARealPdfAndReplayedInARowTemplate()
        {
            var tree = ParseText("ROWTEMPLATE Count=2 Y=50 BorderSize=4 CHART bar 20,0,200,80 Data=[3,1,2] Labels=[\"a\",\"b\",\"c\"]; ENDROWTEMPLATE");
            using var drawer = new PdfDocumentDrawer();
            new PdfDrawerVisitor().Draw(drawer, tree);

            var pdf = drawer.PublishPdf();

            Assert.True(pdf.Length > 500);
            Assert.Equal(1, new PdfBinaryInspector(new MemoryStream(pdf)).PageCount);
        }
    }
}
