using Irony.Parsing;
using Moq;
using PdfSharpCore.Drawing;
using PdfSharpDslCore;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    public class VisitorTests
    {

        private ParseTree ParseText(string text)
        {
            var p = new Irony.Parsing.Parser(new PdfGrammar());

            var parsingResult = p.Parse($"{text}\r\n");
            Assert.False(parsingResult.HasErrors());
            return parsingResult;
        }


        [Fact]
        public void TestValidFiles()
        {
            var res = ParseText("SET BRUSH black");            
            var mock = new Mock<IPdfDocumentDrawer>();
            mock.SetupProperty(x => x.CurrentBrush);
            new PdfDrawerVisitor().Draw(mock.Object, res);

            Assert.Equal(XColors.Black, ((XSolidBrush)mock.Object.CurrentBrush).Color);
        }
    }
}
