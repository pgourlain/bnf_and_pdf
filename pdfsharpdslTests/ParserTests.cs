
using System.Text;
using Irony.Parsing;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    public class ParserTests : BaseTests
    {

        [Fact]
        public void CheckGrammarErrors()
        {
            var p = CreateParser();
            Assert.Empty(p.Language.Errors);
        }


        [Theory()]
        [InlineData("pdf1.txt")]
        [InlineData("pdf1-linetext.txt")]
        public void TestValidFiles(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            ParseText( input );
        }

        [Theory()]
        [InlineData("invalid-pdf1.txt")]
        public void TestInValidFiles(string file)
        {
            var input = File.ReadAllText($"./InvalidInputFiles/{file}");
            var p = CreateParser();

            var parsingResult = p.Parse(input);
            Assert.True(parsingResult.HasErrors());
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