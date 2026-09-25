using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using PdfSharpDslCore.Parser;

namespace PdfSharpDslCore.Evaluation
{
    internal class VariableEvaluation : Evaluation<object>
    {
        private readonly string _varName;
        private readonly IDictionary<string, object?> _variables;

        private readonly SourceLocation? _location;

        public VariableEvaluation(string varName, IDictionary<string, object?> variables, SourceLocation? location = null)
        {
            this._varName = varName;
            this._variables = variables;
            this._location = location;
        }

        private IEnumerable<string> KnownVariableNames()
        {
            // system variables are computed, they are not part of Keys
            var names = new List<string> { "PAGEWIDTH", "PAGEHEIGHT", "PAGECOUNT", "CURSORY" };
            try { names.AddRange(_variables.Keys); } catch (NotSupportedException) { }
            return names;
        }

        public override object? Value
        {
            get{
                if (_variables == null) throw new NotSupportedException("Any variables store was provided.");
                if (_variables.TryGetValue(_varName, out var result))
                {
                    return result;
                }
                throw new PdfParserException(PdfDslDiagnostics.UndefinedVariable(_varName, _location, KnownVariableNames()));

            }
        }
    }
}
