using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Evaluation
{
    internal class VariableEvaluation : Evaluation
    {
        private readonly string _varName;
        private readonly IDictionary<string, object> _variables;

        public VariableEvaluation(string varName, IDictionary<string, object> variables)
        {
            this._varName = varName;
            this._variables = variables;
        }

        public override object Value
        {
            get{
                if (_variables == null) throw new NotSupportedException("Any variables store was provided.");
                if (_variables.TryGetValue(_varName, out var result))
                {
                    return result;
                }
                throw new ArgumentOutOfRangeException($"variable '{_varName}' is not define");

            }
        }
    }
}
