using Pdf.Parser;

namespace pdfsharpdslTests
{
    public class ParserTests
    {
        
        [Theory()]
        [InlineData("pdf1.txt")]
        public void TestValidFiles(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            var p = new Irony.Parsing.Parser(new PdfGrammar());

            var parsingResult = p.Parse(input);
            Assert.False(parsingResult.HasErrors());
        }

        [Theory()]
        [InlineData("invalid-pdf1.txt")]
        public void TestInValidFiles(string file)
        {
            var input = File.ReadAllText($"./InvalidInputFiles/{file}");
            var p = new Irony.Parsing.Parser(new PdfGrammar());

            var parsingResult = p.Parse(input);
            Assert.True(parsingResult.HasErrors());
        }
    }
}