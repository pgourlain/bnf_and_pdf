
using System.Text;
using Irony.Parsing;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Parser;

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

        private ParseTree ParseText(string text)
        {
            var p = new Irony.Parsing.Parser(new PdfGrammar());

            var parsingResult = p.Parse($"{text}\r\n");
            Assert.False(parsingResult.HasErrors(), AsDisplayString(parsingResult));
            return parsingResult;
        }

        string AsDisplayString(ParseTree parsingResult)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var error in parsingResult.ParserMessages)
            {
                sb.Append(error.Location.ToString());
                sb.Append("=>");
                sb.Append(error);
                sb.AppendLine();
            }
            return sb.ToString();
        }
        class PdfDrawerForTestsVisitor : PdfDrawerVisitor
        {
            public IDictionary<string, object> Vars => _variables;
            public PdfDrawerForTestsVisitor()
            {

            }
        }

        [Theory()]
        [InlineData("pdf1-vars.txt", "pdf1-vars-results.txt")]
        public void Variables(string file, string resultFile)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            var resultVars = File.ReadAllLines($"./ValidInputFiles/{resultFile}");

            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.Draw(mock.Object, res);
            //todo test result
            Assert.Equal(resultVars.Length, visitor.Vars.Count);
            foreach (var l in resultVars)
            {
                var splittedLine = l.Split('=');
                var varName = splittedLine[0];
                object varValue = null;
                if (double.TryParse(splittedLine[1], out var dblValue))
                {
                    varValue = dblValue;
                }
                else
                {
                    varValue = splittedLine[1];
                }
                Assert.True(visitor.Vars.ContainsKey(varName));
                Assert.Equal(varValue, visitor.Vars[varName]);
            }
        }
    }
}