using Irony.Parsing;
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

    public class BaseTests
    {
        protected ParseTree ParseText(string text)
        {
            return ParseText<PdfGrammar>(text);
        }
        protected Parser CreateParser()
        {
            return CreateParser<PdfGrammar>();
        }

        protected ParseTree ParseText<T>(string text) where T : Grammar, new()
        {
            var p = CreateParser<T>();

            var parsingResult = p.Parse($"{text}\r\n");
            Assert.False(parsingResult.HasErrors(), AsDisplayString(parsingResult));
            return parsingResult;
        }

        protected Parser CreateParser<T>() where T : Grammar, new() 
        {
            return new Irony.Parsing.Parser(new T());
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
    }
}
