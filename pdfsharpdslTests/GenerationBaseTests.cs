using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class GenerationBaseTests : BaseTests
    {
        protected MemoryStream GeneratePdf(string dslFileContent)
        {
            var parsingResult = ParseText(dslFileContent);

            using var drawer = new PdfDocumentDrawer();
            new PdfDrawerVisitor().Draw(drawer, parsingResult);

            var result = new MemoryStream();
            drawer.PublishPdf(result);
            result.Position = 0;
            return result;
        }
    }
}
