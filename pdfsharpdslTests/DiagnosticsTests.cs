using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class DiagnosticsTests : BaseTests
    {
        private string[] Errors(string source)
        {
            var tree = CreateParser().Parse(source);
            Assert.True(tree.HasErrors(), "source was expected to be invalid");
            return PdfDslDiagnostics.FormatParseErrors(tree).ToArray();
        }

        [Fact]
        public void UnknownInstructionSuggestsClosestKeyword()
        {
            var errors = Errors("SET PEN red 1;\nLINETXT 1,2,3,4 Text=\"a\";\n");

            Assert.Equal("Unknown instruction 'LINETXT' at line 2, col 1. Did you mean 'LINETEXT'?", Assert.Single(errors));
        }

        [Fact]
        public void UnknownInstructionInsideBlockIsReported()
        {
            var errors = Errors("FOR i = 1 TO 3 DO\n  RECTT 1,2,3,4;\nENDFOR\n");

            Assert.Equal("Unknown instruction 'RECTT' at line 2, col 3. Did you mean 'RECT'?", Assert.Single(errors));
        }

        [Fact]
        public void UnknownInstructionWithoutCloseKeywordHasNoSuggestion()
        {
            var errors = Errors("XYZZYPLUGH 1,2;\n");

            Assert.Equal("Unknown instruction 'XYZZYPLUGH' at line 1, col 1.", Assert.Single(errors));
        }

        [Fact]
        public void MissingSemicolonIsReportedAfterPreviousToken()
        {
            var errors = Errors("SET VAR x = 1\nRECT 1,2,3,4;\n");

            Assert.Equal("Missing ';' after '1' at line 1, col 14.", Assert.Single(errors));
        }

        [Theory]
        [InlineData("SET VAR x = 1;\nFOR i = 1 TO 3 DO\n RECT 1,2,3,4;\n", "Missing 'ENDFOR' for 'FOR' opened at line 2, col 1.")]
        [InlineData("UDF a()\n RECT 1,2,3,4;\n", "Missing 'ENDUDF' for 'UDF' opened at line 1, col 1.")]
        [InlineData("IF 1 THEN\n RECT 1,2,3,4;\n", "Missing 'ENDIF' for 'IF' opened at line 1, col 1.")]
        public void MissingBlockEndNamesBlockAndOpeningLine(string source, string expected)
        {
            Assert.Equal(expected, Errors(source)[0]);
        }

        [Fact]
        public void MissingRowTemplateEndNamesTheBlock()
        {
            var errors = Errors("ROWTEMPLATE Count=2 Y=10\n LINETEXT 1,1 Text=\"a\";\n");

            Assert.Equal("Missing 'ENDROWTEMPLATE' for 'ROWTEMPLATE' opened at line 1, col 1.", errors[0]);
        }

        [Fact]
        public void MismatchedBlockEndIsReported()
        {
            var errors = Errors("IF 1 THEN\n RECT 1,2,3,4;\nENDFOR\n");

            Assert.Equal("'ENDFOR' at line 3, col 1 does not match 'IF' opened at line 1, col 1. Expected 'ENDIF'.", Assert.Single(errors));
        }

        [Fact]
        public void StrayBlockEndIsReported()
        {
            var errors = Errors("RECT 1,2,3,4;\nENDIF\n");

            Assert.Equal("'ENDIF' at line 2, col 1 has no matching block to close.", Assert.Single(errors));
        }

        [Fact]
        public void OtherSyntaxErrorsKeepIronyMessageWithPosition()
        {
            var errors = Errors("RECT 1,2,3;\n");

            Assert.StartsWith("Syntax error, expected:", Assert.Single(errors));
            Assert.EndsWith("at line 1, col 11.", errors[0]);
        }

        [Fact]
        public void ValidSourceHasNoDiagnostics()
        {
            var tree = CreateParser().Parse("RECT 1,2,3,4;\n");

            Assert.Empty(PdfDslDiagnostics.FromParseTree(tree));
        }

        [Fact]
        public void UndefinedVariableSuggestsClosestNameWithLocation()
        {
            var tree = ParseText("SET VAR TOTAL = 3;\nLINETEXT 10,10 Text=$TOTL;\n");
            var visitor = new PdfDrawerVisitor();

            var ex = Assert.Throws<PdfParserException>(() => visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.Equal("Variable '$TOTL' is not defined at line 2, col 21. Did you mean '$TOTAL'?", ex.Message);
        }

        [Fact]
        public void UndefinedVariableWithoutCloseNameHasNoSuggestion()
        {
            var tree = ParseText("SET VAR TOTAL = 3;\nLINETEXT 10,10 Text=$QQQQQQ;\n");
            var visitor = new PdfDrawerVisitor();

            var ex = Assert.Throws<PdfParserException>(() => visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.Equal("Variable '$QQQQQQ' is not defined at line 2, col 21.", ex.Message);
        }

        [Fact]
        public void UnknownFunctionSuggestsBuiltIn()
        {
            var tree = ParseText("LINETEXT 10,10 Text=Uppr(\"a\");\n");
            var visitor = new PdfDrawerVisitor();

            var ex = Assert.Throws<PdfParserException>(() => visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.StartsWith("Unknown function 'Uppr' at line 1, col 21.", ex.Message);
            Assert.Contains("Did you mean 'UPPER'?", ex.Message);
        }
    }
}
