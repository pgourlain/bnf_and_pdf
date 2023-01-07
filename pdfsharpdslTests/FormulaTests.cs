using PdfSharpDslCore.Parser;/**/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]

    public class FormulaTests : BaseTests
    {
        [Theory()]
        [InlineData("SET VAR X=180+80+80+80+80;")]
        [InlineData("SET VAR X=180+80;")]
        [InlineData("SET VAR X=180+80+80+80+80;SET VAR X=180+80;")]
        public void FormulasCheck(string formula)
        {
            var parsingResult = ParseText(formula);
            Assert.False(parsingResult.HasErrors());
        }


        [Theory()]
        [InlineData("180+80+80+80+80")]
        [InlineData("180+80")]
        public void OnlyFormulasCheck(string formula)
        {
            var parsingResult = ParseText< FormulaExpressionGrammar>(formula);
            Assert.False(parsingResult.HasErrors());
        }

        [Theory()]
        [InlineData("SET VAR X=180+80+80+80+Random();")]
        [InlineData("SET FONT Name=GetSignatureFont() Size=GetSignatureFontSize();")]
        public void FormulasWithCustomFunctionCheck(string formula)
        {
            var parsingResult = ParseText(formula);
            Assert.False(parsingResult.HasErrors());
        }


        [Theory()]
        [InlineData("SET VAR X=180>80;")]
        [InlineData("SET VAR X=180<80;")]
        [InlineData("SET VAR X=180>=80;")]
        [InlineData("SET VAR X=180<=80;")]
        [InlineData("SET VAR X=180<>80;")]
        [InlineData("SET VAR X=180==80;")]
        public void ForumlasWithCondition(string formula)
        {
            var parsingResult = ParseText(formula);
            Assert.False(parsingResult.HasErrors());
        }

        [Fact]
        public void ConvertTests()
        {
            Assert.True(Convert.ToBoolean(1.0));
            Assert.True(Convert.ToBoolean(50.0));
            Assert.False(Convert.ToBoolean(0));
            Assert.True(Convert.ToBoolean(-12));

            Assert.Equal(1.0, Convert.ToDouble(true));
            Assert.Equal(0.0, Convert.ToDouble(false));
        }
    }
}
