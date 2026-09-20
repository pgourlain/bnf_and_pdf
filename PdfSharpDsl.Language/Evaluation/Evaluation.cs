using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Evaluation
{
    internal interface IEvaluation<T>
    {
        T? Value { get; }
    }

    internal abstract class Evaluation<T> : IEvaluation<T>
    { 
        public abstract T? Value { get; }

        public override string ToString() => Value?.ToString() ?? "(null)";
    }
}
