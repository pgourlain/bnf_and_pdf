using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Drawing.Barcodes;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class BarcodeTests : BaseTests
    {
        [Fact]
        public void EveryPatternIsUniqueAndHasTheRightWidth()
        {
            Assert.Equal(107, Code128Encoder.Patterns.Length);
            Assert.Equal(107, Code128Encoder.Patterns.Distinct().Count());
            for (var value = 0; value < 106; value++)
            {
                var pattern = Code128Encoder.Patterns[value];
                Assert.Equal(6, pattern.Length);
                Assert.Equal(11, pattern.Sum(c => c - '0'));
            }

            Assert.Equal(13, Code128Encoder.Patterns[106].Sum(c => c - '0'));
        }

        [Theory]
        // the checksum position (second to last) is 0 here: it is verified against the rule below
        [InlineData("1234", new[] { 105, 12, 34, 0, 106 })]
        [InlineData("AB", new[] { 104, 33, 34, 0, 106 })]
        // odd run of digits: the first one stays in set B so that the rest pairs up in set C
        [InlineData("12345", new[] { 104, 17, 99, 23, 45, 0, 106 })]
        // two digits only: set C from the start
        [InlineData("42", new[] { 105, 42, 0, 106 })]
        // digits then letters: back to set B
        [InlineData("1234A", new[] { 105, 12, 34, 100, 33, 0, 106 })]
        // a short digit run is not worth a switch
        [InlineData("A123B", new[] { 104, 33, 17, 18, 19, 34, 0, 106 })]
        public void ValuesFollowTheCode128Rules(string text, int[] expected)
        {
            var values = Code128Encoder.Values(text);

            Assert.Equal(expected.Length, values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                if (i == values.Count - 2) continue;
                Assert.Equal(expected[i], values[i]);
            }

            var checksum = values[0];
            for (var i = 1; i < values.Count - 2; i++) checksum += values[i] * i;
            Assert.Equal(checksum % 103, values[values.Count - 2]);
        }

        [Fact]
        public void KnownChecksums()
        {
            Assert.Equal(82, Code128Encoder.Values("1234")[3]);
            Assert.Equal(205 % 103, Code128Encoder.Values("AB")[3]);
        }

        [Fact]
        public void ModulesHaveQuietZonesAndAStartAndStopPattern()
        {
            var modules = Code128Encoder.Encode("1234");

            // 10 + (start + 2 data + checksum) 11 modules each + stop 13 + 10
            Assert.Equal(10 + 4 * 11 + 13 + 10, modules.Length);
            Assert.All(modules.Take(10), m => Assert.False(m));
            Assert.All(modules.Skip(modules.Length - 10), m => Assert.False(m));
            // start C is 211232: bar 2, space 1, bar 1, space 2, bar 3, space 2
            var start = modules.Skip(10).Take(11).Select(m => m ? '#' : '.').ToArray();
            Assert.Equal("##.#..###..", new string(start));
            // stop 2331112 ends with a 2 wide bar
            Assert.True(modules[modules.Length - 11]);
            Assert.True(modules[modules.Length - 12]);
        }

        [Fact]
        public void NonAsciiTextThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Code128Encoder.Encode("café"));

            Assert.Contains("U+00E9", ex.Message);
        }

        [Fact]
        public void EmptyTextThrows()
        {
            Assert.Throws<PdfParserException>(() => Code128Encoder.Encode(""));
        }

        [Fact]
        public void BarcodeParsesAndReachesTheDrawer()
        {
            var tree = ParseText("SET VAR CODE=\"ABC-123\"; BARCODE 40,100,200,50 Type=code128 Text=$CODE;");
            var drawer = new Mock<IPdfDocumentDrawer>();

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            drawer.Verify(d => d.DrawBarcode(40, 100, 200, 50, PdfBarcodeType.Code128, "ABC-123"), Times.Once);
        }

        [Fact]
        public void BarcodeNeedsAnExplicitSize()
        {
            Assert.True(CreateParser().Parse("BARCODE 40,100 Type=code128 Text=\"x\";").HasErrors());
        }

        [Fact]
        public void BarcodeWithAZeroSizeThrows()
        {
            var tree = ParseText("BARCODE 40,100,0,20 Type=code128 Text=\"x\";");

            var ex = Assert.Throws<PdfParserException>(() => new PdfDrawerVisitor().Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.StartsWith("BARCODE needs a positive width and height", ex.Message);
        }

        [Fact]
        public void BarcodeIsDrawnAsOneFilledPathInTheCurrentBrush()
        {
            var tree = ParseText("SET BRUSH red; BARCODE 40,100,200,50 Type=code128 Text=\"1234\";");
            using var drawer = new PdfDocumentDrawer();
            new PdfDrawerVisitor().Draw(drawer, tree);

            var content = string.Concat(ReadStreams(drawer.PublishPdf()));

            // one path: a rectangle per bar, a single fill, in red
            Assert.Single(Regex.Matches(content, @"^f\*?$", RegexOptions.Multiline));
            Assert.Contains("1.0000 0.0000 0.0000 rg", content);
            // 4 symbols of 3 bars + start C (3 bars) ... at least 3 bars per symbol
            Assert.True(Regex.Matches(content, @" m\n").Count >= 4 * 3, content);
        }

        [Fact]
        public void BarcodeInsideARowTemplateIsReplayedPerRow()
        {
            var tree = ParseText("ROWTEMPLATE Count=3 Y=100 BARCODE 40,0,200,20 Type=code128 Text=\"ROW\"; ENDROWTEMPLATE");
            using var drawer = new PdfDocumentDrawer();
            new PdfDrawerVisitor().Draw(drawer, tree);

            var content = string.Concat(ReadStreams(drawer.PublishPdf()));

            Assert.Equal(3, Regex.Matches(content, @"^f\*?$", RegexOptions.Multiline).Count);
        }

        private static IEnumerable<string> ReadStreams(byte[] pdf)
        {
            var raw = Encoding.Latin1.GetString(pdf);
            foreach (System.Text.RegularExpressions.Match match in Regex.Matches(raw, @"stream\r?\n(?<data>.*?)\r?\nendstream", RegexOptions.Singleline))
            {
                using var compressed = new MemoryStream(Encoding.Latin1.GetBytes(match.Groups["data"].Value));
                using var inflated = new System.IO.Compression.ZLibStream(compressed, System.IO.Compression.CompressionMode.Decompress);
                using var reader = new StreamReader(inflated, Encoding.Latin1);
                yield return reader.ReadToEnd();
            }
        }
    }
}
