using Irony.Parsing;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    [Language("FormulaExpressionGrammar", "1.0", "Grammar for PDF Grammar")]
    public class FormulaExpressionGrammar : PdfGrammar
    {
        public FormulaExpressionGrammar()
        {
            var formulaRoot = new NonTerminal("FormulaRoot");
            //formulaRoot.Rule = FormulaRoot + FormulaRoot | FormulaRoot;
            formulaRoot.Rule = FormulaRoot;
            this.Root = formulaRoot;
        }
    }
}
