using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdsl.Evaluation
{
    internal class ConstantEvaluation : Evaluation
    {
        private readonly object value;

        public ConstantEvaluation(object value)
        {
            this.value = value;
        }

        public override object Value => value;
    }
}
