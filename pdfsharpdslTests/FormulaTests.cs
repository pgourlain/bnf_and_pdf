using PdfSharpDslCore.Parser;/**/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    public class FormulaTests : BaseTests
    {
        [Theory()]
        [InlineData("SET VAR X=180+80+80+80+80;")]
        [InlineData("SET VAR X=180+80;")]
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
    }
}
