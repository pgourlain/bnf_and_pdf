using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Evaluation
{
    internal class ConstantEvaluation<T> : Evaluation<T>
    {
        private readonly T value;

        public ConstantEvaluation(T value)
        {
            this.value = value;
        }

        public override T? Value => value;
    }
}
