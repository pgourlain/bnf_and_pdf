using PdfSharpCore.Pdf;
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
