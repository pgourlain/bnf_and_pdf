using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdsl.Evaluation
{
    internal abstract class Evaluation
    {
        public abstract object Value { get; }

        public override string ToString() => Value?.ToString();
    }
}
