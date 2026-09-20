
using PdfSharpDslCore.Parser;
using PdfSharpDslCore.Drawing;
using System.Diagnostics;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class GenerationTests : GenerationBaseTests
    {
        [Theory]
        [InlineData("pdf1-linetext.txt")]
        [InlineData("pdf1-all-instructions.txt")]
        public void TestDrawingNotFailed(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            using var memStm = GeneratePdf(input);
            var pdf = new PdfBinaryInspector(memStm);

            Assert.True(pdf.HasPdfHeader);
            Assert.True(pdf.PageCount > 0);
        }


        [Theory()]
        [InlineData("pdf1-lines.txt")]
        public void TestdrawingLinesOutPut(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            using var memStm = GeneratePdf(input);
            var pdf = new PdfBinaryInspector(memStm);

            Assert.True(pdf.HasPdfHeader);
            Assert.Equal(1, pdf.PageCount);
            Assert.True(pdf.ContentStreamCount > 0);
        }

        [Fact]
        public void DrawingEmbeddedImageAddsPdfImageResource()
        {
            const string input = "NEWPAGE A4 portrait;" +
                "IMAGE 10,10,20,20 point fit Data=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=\";";

            using var memStm = GeneratePdf(input);
            var pdf = new PdfBinaryInspector(memStm);

            Assert.True(pdf.HasPdfHeader);
            Assert.Equal(1, pdf.ImageCount);
        }

        [Theory()]
        [InlineData("pdf1-udfs.txt")]
        public void TestdrawingUdfsOutPut(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            using var memStm = GeneratePdf(input);
            var pdf = new PdfBinaryInspector(memStm);

            Assert.True(pdf.HasPdfHeader);
            Assert.Equal(1 + 10, pdf.PageCount);
        }

        [Theory()]
        [InlineData("invalid-udf1.txt")]
        [InlineData("invalid-udf2.txt")]
        [InlineData("invalid-udf3.txt")]
        public void TestdrawinginvalidUdfsOutPut(string file)
        {
            var input = File.ReadAllText($"./InvalidInputFiles/{file}");
            Assert.Throws<PdfParserException>(() =>
            {
                using var memStm = GeneratePdf(input);
            });
        }

    }
}