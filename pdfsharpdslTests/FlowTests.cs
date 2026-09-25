using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Irony.Parsing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class FlowTests : BaseTests
    {
        private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

        private sealed class InspectableVisitor : PdfDrawerVisitor
        {
            public IDictionary<string, object?> Vars => Variables;
        }

        private static (InspectableVisitor Visitor, byte[] Pdf) Render(string source)
        {
            var tree = new Parser(new PdfGrammar()).Parse(source);
            Assert.False(tree.HasErrors(), string.Join("; ", PdfDslDiagnostics.FormatParseErrors(tree)));
            using var drawer = new PdfDocumentDrawer();
            var visitor = new InspectableVisitor();
            visitor.Draw(drawer, tree);
            return (visitor, drawer.PublishPdf());
        }

        private static int PageCount(byte[] pdf) => new PdfBinaryInspector(new MemoryStream(pdf)).PageCount;

        private static void AssertClose(double expected, object? actual) =>
            Assert.InRange(Convert.ToDouble(actual), expected - 0.01, expected + 0.01);

        [Fact]
        public void FlowParses()
        {
            ParseText("FLOW Margin=40 Top=80 PARAGRAPH Text=\"a\"; SPACE 12; ENDFLOW");
            ParseText("FLOW PARAGRAPH HAlign=right Text=\"a\"; ENDFLOW");
        }

        [Fact]
        public void CursorStartsAtTopThenSpaceAndParagraphAdvanceIt()
        {
            var (visitor, _) = Render(
                "SET FONT Name=\"Arial\" Size=10 regular;" +
                "FLOW Margin=40 Top=100 " +
                "SET VAR start = $CURSORY; " +
                "SPACE 25; " +
                "SET VAR afterSpace = $CURSORY; " +
                "PARAGRAPH Text=\"one line\"; " +
                "SET VAR afterParagraph = $CURSORY; " +
                "ENDFLOW");

            AssertClose(100, visitor.Vars["start"]);
            AssertClose(125, visitor.Vars["afterSpace"]);
            AssertClose(137, visitor.Vars["afterParagraph"]); // one 10pt line = 12pt high
        }

        [Fact]
        public void TopDefaultsToMargin()
        {
            var (visitor, _) = Render("FLOW Margin=50 SET VAR start = $CURSORY; ENDFLOW");

            AssertClose(50, visitor.Vars["start"]);
        }

        [Fact]
        public void ParagraphWrapsToFlowWidth()
        {
            // A4 is 595 wide: a 250pt-wide flow must wrap this text onto several lines, a 40pt margin flow must not
            var text = string.Join(" ", Enumerable.Repeat("lorem ipsum", 12));
            var (wide, _) = Render($"SET FONT Name=\"Arial\" Size=10 regular; FLOW Margin=10 Top=0 PARAGRAPH Text=\"{text}\"; SET VAR y = $CURSORY; ENDFLOW");
            var (narrow, _) = Render($"SET FONT Name=\"Arial\" Size=10 regular; FLOW Margin=172 Top=0 PARAGRAPH Text=\"{text}\"; SET VAR y = $CURSORY; ENDFLOW");

            Assert.True(Convert.ToDouble(narrow.Vars["y"]) > Convert.ToDouble(wide.Vars["y"]));
        }

        [Fact]
        public void LongParagraphContinuesOnNextPage()
        {
            var (visitor, pdf) = Render(
                "SET FONT Name=\"Arial\" Size=10 regular;" +
                "SET VAR t = \"\";" +
                "FOR i = 1 TO 100 DO SET VAR t = $t + \"line \" + $i + \"\\n\"; ENDFOR " +
                "FLOW Margin=40 Top=40 PARAGRAPH Text=$t; SET VAR end = $CURSORY; ENDFLOW");
            var streams = ReadStreams(pdf).ToList();

            Assert.Equal(2, streams.Count);
            Assert.Contains("(line 1) Tj", streams[0]);
            Assert.DoesNotContain("(line 100) Tj", streams[0]);
            Assert.Contains("(line 100) Tj", streams[1]);
            // 100 lines + the empty one after the last "\n", 12pt each; A4 body is 842-2*40 = 762 -> 63 lines on page 1,
            // the other 38 on page 2, starting at y=40
            AssertClose(40 + 38 * 12, visitor.Vars["end"]);
        }

        [Fact]
        public void PageBreakKeepsMasterAndMasterMarginTop()
        {
            var (visitor, pdf) = Render(
                "MASTER report MarginTop=90 TITLE Margin=20 Text=\"ACME report\"; ENDMASTER " +
                "NEWPAGE A4 portrait Master=report; " +
                "SET FONT Name=\"Arial\" Size=10 regular;" +
                "FLOW Margin=40 Top=90 " +
                "FOR i = 1 TO 80 DO PARAGRAPH Text=\"paragraph \" + $i; SPACE 6; ENDFOR " +
                "SET VAR y = $CURSORY; " +
                "ENDFLOW");
            var streams = ReadStreams(pdf).ToList();

            Assert.True(streams.Count > 1, "expected the flow to overflow onto a second page");
            Assert.All(streams, s => Assert.Contains("(ACME report) Tj", s));
            Assert.Contains("(paragraph 80) Tj", streams[^1]);
            // the last page restarts below the master's MarginTop (90), not at the flow margin
            Assert.True(Convert.ToDouble(visitor.Vars["y"]) > 90);
        }

        [Fact]
        public void ParagraphAtPageBottomMovesWholeElementWhenItDoesNotFit()
        {
            var (visitor, pdf) = Render(
                "SET FONT Name=\"Arial\" Size=10 regular;" +
                "FLOW Margin=40 Top=780 " +
                "PARAGRAPH Text=\"first\"; " +
                "SET VAR y1 = $CURSORY; " +
                "SPACE 100; " +
                "PARAGRAPH Text=\"second\"; " +
                "SET VAR y2 = $CURSORY; " +
                "ENDFLOW");
            var streams = ReadStreams(pdf).ToList();

            Assert.Equal(2, streams.Count);
            Assert.Contains("(first) Tj", streams[0]);
            Assert.Contains("(second) Tj", streams[1]);
            AssertClose(792, visitor.Vars["y1"]);
            AssertClose(40 + 12, visitor.Vars["y2"]);
        }

        [Fact]
        public void ImageIsPlacedRelativeToCursorAndAdvancesIt()
        {
            var (visitor, pdf) = Render(
                $"FLOW Margin=40 Top=100 IMAGE 10,5,50,30 point fit Data=\"data:image/png;base64,{Png}\"; " +
                "SET VAR y = $CURSORY; ENDFLOW");

            AssertClose(100 + 5 + 30, visitor.Vars["y"]);
            Assert.Equal(1, PageCount(pdf));
        }

        [Fact]
        public void ImageThatDoesNotFitMovesToNextPage()
        {
            var (visitor, pdf) = Render(
                $"FLOW Margin=40 Top=780 IMAGE 0,0,50,50 point fit Data=\"data:image/png;base64,{Png}\"; " +
                "SET VAR y = $CURSORY; ENDFLOW");

            Assert.Equal(2, PageCount(pdf));
            AssertClose(40 + 50, visitor.Vars["y"]);
        }

        [Fact]
        public void TableStartsAtCursorAndAdvancesIt()
        {
            var (visitor, _) = Render(
                "SET FONT Name=\"Arial\" Size=10 regular;" +
                "FLOW Margin=40 Top=100 " +
                "TABLE 0,0 HEAD COL Width=100 MaxWidth=100 \"H\"; ENDHEAD ROW COL \"a\"; ENDROW ROW COL \"b\"; ENDROW ENDTABLE " +
                "SET VAR y = $CURSORY; " +
                "ENDFLOW");

            // header (12 + 2 margin) + two rows (12 + 2 each)
            AssertClose(100 + 3 * 14, visitor.Vars["y"]);
        }

        [Fact]
        public void TableRowsBreakAtFlowBottomMargin()
        {
            var (visitor, pdf) = Render(
                "SET FONT Name=\"Arial\" Size=10 regular;" +
                "FLOW Margin=40 Top=40 " +
                "TABLE 0,0 HEAD COL Width=100 MaxWidth=100 \"H\"; ENDHEAD ROWTEMPLATE 120 COL \"row \" + $ROWINDEX; ENDROW ENDTABLE " +
                "SET VAR y = $CURSORY; " +
                "ENDFLOW");
            Assert.Equal(3, PageCount(pdf));
            // never draws into the bottom margin: 842 - 40 = 802
            Assert.True(Convert.ToDouble(visitor.Vars["y"]) <= 802.01);
        }

        [Fact]
        public void ParagraphOutsideFlowThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Render("PARAGRAPH Text=\"a\";"));

            Assert.Equal("PARAGRAPH can only be used inside FLOW ... ENDFLOW.", ex.Message);
        }

        [Fact]
        public void SpaceOutsideFlowThrows()
        {
            Assert.Throws<PdfParserException>(() => Render("SPACE 10;"));
        }

        [Fact]
        public void NestedFlowThrows()
        {
            Assert.Throws<PdfParserException>(() => Render("FLOW FLOW SPACE 1; ENDFLOW ENDFLOW"));
        }

        [Fact]
        public void CursorYOutsideFlowThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Render("LINETEXT 10,10 Text=$CURSORY;"));

            Assert.Equal("$CURSORY is only available inside FLOW.", ex.Message);
        }

        [Fact]
        public void FlowVariableIsAvailableAfterFlowEnds()
        {
            var (visitor, _) = Render("FLOW Margin=40 Top=100 SPACE 10; SET VAR y = $CURSORY; ENDFLOW SET VAR z = 1;");

            AssertClose(110, visitor.Vars["y"]);
            AssertClose(1, visitor.Vars["z"]);
        }

        [Fact]
        public void MissingEndFlowIsDiagnosed()
        {
            var tree = new Parser(new PdfGrammar()).Parse("FLOW Margin=40\nPARAGRAPH Text=\"a\";\n");

            Assert.Equal("Missing 'ENDFLOW' for 'FLOW' opened at line 1, col 1.", PdfDslDiagnostics.FormatParseErrors(tree).First());
        }

        private static IEnumerable<string> ReadStreams(byte[] pdf)
        {
            var text = Encoding.Latin1.GetString(pdf);
            foreach (Match match in Regex.Matches(text, "stream\\r?\\n", RegexOptions.Singleline))
            {
                var start = match.Index + match.Length;
                var end = text.IndexOf("endstream", start, StringComparison.Ordinal);
                if (end < 0) continue;
                var raw = Encoding.Latin1.GetBytes(text.Substring(start, end - start));
                string? inflated = null;
                try
                {
                    using var input = new MemoryStream(raw);
                    using var zlib = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
                    using var reader = new StreamReader(zlib, Encoding.Latin1);
                    inflated = reader.ReadToEnd();
                }
                catch (Exception) { }
                if (inflated != null && inflated.Contains(" Tf")) yield return inflated;
            }
        }
    }
}
