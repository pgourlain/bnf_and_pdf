using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfSharpDslCore.Evaluation
{
    internal class CustomFunctionEvaluation : Evaluation<object>
    {
        Func<object[],object> _func;
        IEvaluation<object>[] _arguments;
        public CustomFunctionEvaluation(Func<object[], object> func, IEvaluation<object>[] arguments)
        {
            _func = func;
            _arguments = arguments ?? Array.Empty<IEvaluation<object>>();
        }

        public override object? Value => _func(_arguments.Select(x => x.Value!).ToArray());
    }
}
