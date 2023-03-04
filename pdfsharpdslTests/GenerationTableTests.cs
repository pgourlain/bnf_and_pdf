using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class GenerationTableTests : GenerationBaseTests
    {
        [Theory]
        [InlineData("pdf1-table.txt")]
        public void TestDrawingNotFailed(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            using var memStm = GeneratePdf(input);
            memStm.Position = 0;
            using PdfDocument pdfDocument = PdfReader.Open(memStm, PdfDocumentOpenMode.Import);
            //generation and import not failed
            Assert.True(true);
        }
    }
}
