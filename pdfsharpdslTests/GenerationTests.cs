
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf;
using PdfSharpDslCore.Parser;
using PdfSharpDslCore.Drawing;
using PdfSharpCore.Pdf.Content;
using PdfSharpCore.Pdf.Content.Objects;
using System.Diagnostics;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class GenerationTests : BaseTests
    {
        [Theory]
        [InlineData("pdf1-linetext.txt")]
        [InlineData("pdf1-all-instructions.txt")]
        public void TestDrawingNotFailed(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            using var memStm = GeneratePdf(input);
            memStm.Position = 0;
            using PdfDocument pdfDocument = PdfReader.Open(memStm, PdfDocumentOpenMode.Import);
            //generation and import not failed
            Assert.True(true);
        }


        [Theory()]
        [InlineData("pdf1-lines.txt")]
        public void TestdrawingLinesOutPut(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            using var memStm = GeneratePdf(input);
            memStm.Position = 0;
            //reopne pdf to check if "print" works
            using PdfDocument pdfDocument = PdfReader.Open(memStm, PdfDocumentOpenMode.Import);
            Assert.Equal(1, pdfDocument.PageCount);
            var p = pdfDocument.Pages[0];
            var h = p.Height.Point;
            Assert.NotNull(p);

            var lines = ExtractLines(p).ToArray();
            Assert.Equal(4, lines.Length);
            //TODO check values extracted from lines
        }

        [Theory()]
        [InlineData("pdf1-udfs.txt")]
        public void TestdrawingUdfsOutPut(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            using var memStm = GeneratePdf(input);
            memStm.Position = 0;
            //reopen pdf to check if "print" works
            using PdfDocument pdfDocument = PdfReader.Open(memStm, PdfDocumentOpenMode.Import);
            Assert.Equal(1+10, pdfDocument.PageCount);

            Assert.True(true);
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

        private IEnumerable<string> ExtractLines(PdfPage p)
        {
            var h = p.Height.Point;
            var content = ContentReader.ReadContent(p);

            foreach (COperator op in content)
            {
                string s = string.Empty;
                switch (op.OpCode.OpCodeName)
                {
                    case OpCodeName.m:
                        s = "MOVETO " + ExtractLineOperands(op.Operands, h);
                        yield return s;
                        break;
                    case OpCodeName.l:
                        s = "LINETO " + ExtractLineOperands(op.Operands, h); ;
                        yield return s;
                        break;
                    default:
                        break;
                }
            }
        }

        string ExtractLineOperands(CSequence cSequence, double pageHeight)
        {
            List<string> lines = new List<string>();
            int i = 0;
            foreach (var operand in cSequence)
            {
                if (operand is CInteger intValue)
                {
                    if (i == 1)
                    {
                        lines.Add((pageHeight - intValue.Value).ToString());
                    }
                    else
                    {
                        lines.Add(intValue.ToString());
                    }
                }
                i++;
            }
            return string.Join(",", lines.ToArray());
        }

        private MemoryStream GeneratePdf(string dslFileContent)
        {
            var parsingResult = ParseText(dslFileContent);

            //PdfSharpCore cclasses
            using var document = new PdfDocument();
            //draw parsing result
            using var drawer = new PdfDocumentDrawer(document);
            new PdfDrawerVisitor().Draw(drawer, parsingResult);

            var result = new MemoryStream();
            document.Save(result, false);
            return result;
        }
    }
}