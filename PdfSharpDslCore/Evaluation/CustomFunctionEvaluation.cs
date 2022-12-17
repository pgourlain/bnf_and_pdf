using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfSharpDslCore.Evaluation
{
    internal class CustomFunctionEvaluation : Evaluation
    {
        Func<object[],object> _func;
        Evaluation[] _arguments;
        public CustomFunctionEvaluation(Func<object[], object> func, Evaluation[] arguments)
        {
            _func = func;
            _arguments = arguments ?? Array.Empty<Evaluation>();
        }

        public override object? Value => _func(_arguments.Select(x => x.Value!).ToArray());
    }
}
