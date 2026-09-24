using PdfSharpDslCore.Drawing;

namespace pdfsharpdslTests
{
    public class PdfDocumentDrawerTests
    {
        [Fact]
        public void DslOrientationsProduceRotatedTextMatrices()
        {
            var parser = new Irony.Parsing.Parser(new PdfSharpDslCore.Parser.PdfGrammar());
            var tree = parser.Parse(
                "LINETEXT 220,235 HAlign=left VAlign=vcenter Orientation=vertical Text=\"vertical\";" +
                "LINETEXT 300,235 HAlign=left VAlign=vcenter Orientation=30 Text=\"30 degree rotation\";");
            Assert.False(tree.HasErrors());
            using var drawer = new PdfDocumentDrawer();
            new PdfSharpDslCore.Parser.PdfDrawerVisitor().Draw(drawer, tree);
            var content = ReadContent(drawer.PublishPdf());

            Assert.Matches(@"0\.000000 -1\.000000 1\.000000 0\.000000 220\.00 [\d.]+ Tm\n\(vertical\) Tj", content);
            Assert.Matches(@"0\.866025 -0\.500000 0\.500000 0\.866025 300\.00 [\d.]+ Tm\n\(30 degree rotation\) Tj", content);
        }

        [Theory]
        [InlineData("DEBUGOPTIONS DEBUG_TEXT;LINETEXT 10,10 Text=\"hello\";", 1)]
        [InlineData("DEBUGOPTIONS DEBUG_ROWTEMPLATE;ROWTEMPLATE Count=1 Y=10 LINETEXT 10,0 Text=\"row\"; ENDROWTEMPLATE", 1)]
        [InlineData("DEBUGOPTIONS DEBUG_RULE;LINETEXT 10,10 Text=\"hello\";NEWPAGE;LINETEXT 10,10 Text=\"hello\";", 2)]
        [InlineData("LINETEXT 10,10 Text=\"hello\";DEBUGOPTIONS PAGE DEBUG_RULE;NEWPAGE;LINETEXT 10,10 Text=\"hello\";", 1)]
        [InlineData("LINETEXT 10,10 Text=\"hello\";DEBUGOPTIONS PAGE DEBUG_ALL;NEWPAGE;LINETEXT 10,10 Text=\"hello\";", 1)]
        [InlineData("LINETEXT 10,10 Text=\"hello\";", 0)]
        public void DebugOptionsAreDrawnInRed(string input, int expectedRedPages)
        {
            var parser = new Irony.Parsing.Parser(new PdfSharpDslCore.Parser.PdfGrammar());
            var tree = parser.Parse(input);
            Assert.False(tree.HasErrors());
            using var drawer = new PdfDocumentDrawer();
            new PdfSharpDslCore.Parser.PdfDrawerVisitor().Draw(drawer, tree);

            var redPages = ReadStreams(drawer.PublishPdf()).Count(content => content.Contains("1.0000 0.0000 0.0000 RG\n"));

            Assert.Equal(expectedRedPages, redPages);
        }

        [Fact]
        public void RowTemplateDebugRectIsDrawnAtRowPosition()
        {
            var parser = new Irony.Parsing.Parser(new PdfSharpDslCore.Parser.PdfGrammar());
            var tree = parser.Parse("DEBUGOPTIONS DEBUG_ROWTEMPLATE;ROWTEMPLATE Count=1 Y=300 LINE 10,0,100,20; ENDROWTEMPLATE");
            Assert.False(tree.HasErrors());
            using var drawer = new PdfDocumentDrawer();
            new PdfSharpDslCore.Parser.PdfDrawerVisitor().Draw(drawer, tree);

            var content = ReadContent(drawer.PublishPdf());

            //red rect from y=300 to y=320 on A4, pdf y axis is bottom up
            Assert.Matches(@"1\.0000 0\.0000 0\.0000 RG\n10\.00 521\.89 90\.00 20\.00 re\n", content);
        }

        [Fact]
        public void PolygonsKeepStylesFromTheirDrawingInstructions()
        {
            var parser = new Irony.Parsing.Parser(new PdfSharpDslCore.Parser.PdfGrammar());
            var tree = parser.Parse(
                "SET PEN darkslategray 2 solid;POLYGON 10,20,30,20,20,40;" +
                "SET BRUSH lightseagreen;FILLPOLYGON 50,20,70,20,60,40;" +
                "SET PEN crimson 0.5 solid;SET BRUSH black;");
            Assert.False(tree.HasErrors());
            using var drawer = new PdfDocumentDrawer();
            new PdfSharpDslCore.Parser.PdfDrawerVisitor().Draw(drawer, tree);

            var content = ReadContent(drawer.PublishPdf());

            Assert.Contains("2.00 w\n0.1843 0.3098 0.3098 RG\n", content);
            Assert.Contains("0.1255 0.6980 0.6667 rg\n", content);
            Assert.Contains("h\nS\n", content);
            Assert.Contains("h\nB\n", content);
            Assert.DoesNotContain("0.0000 0.0000 0.0000 rg\n", content);
        }

        [Fact]
        public void LineToKeepsEachStartPointAndPenAtRecordingTime()
        {
            using var drawer = new PdfDocumentDrawer();
            drawer.CurrentPen = new PdfPen(new PdfColor(255, 128, 0, 128), 3);
            drawer.MoveTo(10, 20);
            drawer.LineTo(30, 40);
            drawer.LineTo(50, 20);
            drawer.CurrentPen = new PdfPen(PdfColor.Black, 1);
            drawer.MoveTo(100, 100);

            var content = ReadContent(drawer.PublishPdf());

            Assert.Contains("0.5020 0.0000 0.5020 RG\n", content);
            Assert.Contains("3.00 w\n", content);
            Assert.Contains("10.00 821.89 m\n30.00 801.89 l\n", content);
            Assert.Contains("30.00 801.89 m\n50.00 821.89 l\n", content);
        }

        [Fact]
        public void DashStylesUseNativePdfPatternAndDoNotLeak()
        {
            var parser = new Irony.Parsing.Parser(new PdfSharpDslCore.Parser.PdfGrammar());
            var tree = parser.Parse(
                "SET PEN black 2 dash;LINE 10,20,110,20;" +
                "SET PEN black 2 solid;LINE 10,40,110,40;");
            Assert.False(tree.HasErrors());
            using var drawer = new PdfDocumentDrawer();
            new PdfSharpDslCore.Parser.PdfDrawerVisitor().Draw(drawer, tree);

            var content = ReadContent(drawer.PublishPdf());

            Assert.Contains("[8.00 6.00] 0.00 d\n", content);
            Assert.Contains("q\n[8.00 6.00] 0.00 d\n", content);
            Assert.Contains("Q\n2.00 w\n0.0000 0.0000 0.0000 RG\n10.00 801.89 m\n110.00 801.89 l\nS\n", content);
        }

        [Fact]
        public void DashStylesApplyToRectangleOutlines()
        {
            var parser = new Irony.Parsing.Parser(new PdfSharpDslCore.Parser.PdfGrammar());
            var tree = parser.Parse("SET PEN crimson 1 dash;RECT 20,40,-20,-55;");
            Assert.False(tree.HasErrors());
            using var drawer = new PdfDocumentDrawer();
            new PdfSharpDslCore.Parser.PdfDrawerVisitor().Draw(drawer, tree);

            var content = ReadContent(drawer.PublishPdf());

            Assert.Contains("q\n[4.00 3.00] 0.00 d\n", content);
            Assert.Contains("re\nS\nQ\n", content);
        }

        [Fact]
        public void TablesMeasureAutoColumnsAndMultilineRows()
        {
            using var drawer = new PdfDocumentDrawer();
            var table = new TableDefinition();
            table.Columns.Add(new ColumnDefinition { ColumnHeaderName = "Feature", MaxWidth = 130, Font = new PdfFont("Arial", 9) });
            table.Columns.Add(new ColumnDefinition { ColumnHeaderName = "Syntax", DesiredWidth = 140, MaxWidth = 180, Font = new PdfFont("Arial", 9) });
            table.Columns.Add(new ColumnDefinition { ColumnHeaderName = "Notes", MaxWidth = 240, Font = new PdfFont("Arial", 9) });
            table.Rows.Add(new RowDefinition { Data = ["Automatic sizing", "Width=auto", "Desired width follows content up to MaxWidth"] });
            table.Rows.Add(new RowDefinition { Data = ["Multiline cell", "COL expression", "Line one\r\nLine two\r\nLine three"] });

            drawer.DrawTable(40, 90, table);

            Assert.True(table.Columns[0].DesiredWidth > 0);
            Assert.Equal(140, table.Columns[1].DrawWidth);
            Assert.True(table.Columns[2].DesiredWidth > table.Columns[2].ColumnHeaderName.Length);
            Assert.True(table.Rows[1].DesiredHeight > table.Rows[0].DesiredHeight);
        }

        private static string ReadContent(byte[] pdf) => string.Concat(ReadStreams(pdf));

        private static IEnumerable<string> ReadStreams(byte[] pdf)
        {
            var raw = System.Text.Encoding.Latin1.GetString(pdf);
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                raw, @"stream\r?\n(?<data>.*?)\r?\nendstream", System.Text.RegularExpressions.RegexOptions.Singleline))
            {
                using var compressed = new MemoryStream(System.Text.Encoding.Latin1.GetBytes(match.Groups["data"].Value));
                using var inflated = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionMode.Decompress);
                using var reader = new StreamReader(inflated, System.Text.Encoding.Latin1);
                yield return reader.ReadToEnd();
            }
        }

        [Fact]
        public void NewPageUsesDefaultsAndPersistsExplicitSettings()
        {
            using var drawer = new PdfDocumentDrawer();
            var pageNumbers = new List<int>();
            Action<int> callback = pageNumbers.Add;
            drawer.RegisterOnNewPage(callback);
            drawer.RegisterOnNewPage(callback);

            Assert.Equal(595.28, drawer.PageWidth, 2);
            Assert.Equal(841.89, drawer.PageHeight, 2);

            drawer.NewPage(PdfPageSize.Letter, PdfPageOrientation.Landscape);
            Assert.Equal(792, drawer.PageWidth);
            Assert.Equal(612, drawer.PageHeight);

            drawer.NewPage();
            Assert.Equal(792, drawer.PageWidth);
            Assert.Equal(612, drawer.PageHeight);
            Assert.Equal(new[] { 2, 3 }, pageNumbers);

            drawer.UnRegisterOnNewPage(callback);
            drawer.NewPage();
            Assert.Equal(new[] { 2, 3 }, pageNumbers);

            using var stream = new MemoryStream();
            drawer.PublishPdf(stream);
            var pdf = new PdfBinaryInspector(stream);
            Assert.Equal(4, pdf.PageCount);
            Assert.Equal(4, pdf.MediaBoxes.Count);
        }
    }
}