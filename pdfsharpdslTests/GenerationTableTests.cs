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
            var pdf = new PdfBinaryInspector(memStm);

            Assert.True(pdf.HasPdfHeader);
            Assert.True(pdf.PageCount > 0);
            Assert.True(pdf.ContentStreamCount > 0);
        }
    }
}
