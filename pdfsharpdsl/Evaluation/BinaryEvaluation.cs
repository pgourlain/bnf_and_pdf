using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdsl.Evaluation
{
    internal class BinaryEvaluation : Evaluation
    {
        private readonly Evaluation left;
        private readonly Evaluation right;

        private readonly BinaryOperation oper;

        public BinaryEvaluation(Evaluation left, Evaluation right, BinaryOperation oper)
        {
            this.left = left;
            this.right = right;
            this.oper = oper;
        }

        public override object Value
        {
            get
            {
                var leftValue = 0.0;
                var rightValue = 0.0;
                if (this.left.Value == null || this.right.Value == null)
                {
                    throw new InvalidOperationException("Either left or right value of the binary evaluation has been evaluated to null.");
                }
                if (!double.TryParse(this.left.Value.ToString(), out leftValue) ||
                    !double.TryParse(this.right.Value.ToString(), out rightValue))
                {
                    throw new InvalidOperationException("Either left or right value of the binary evaluation cannot be evaluated as a float value.");
                }
                switch (oper)
                {
                    case BinaryOperation.Add:
                        return leftValue + rightValue;
                    case BinaryOperation.Sub:
                        return leftValue - rightValue;
                    case BinaryOperation.Mul:
                        return leftValue * rightValue;
                    case BinaryOperation.Div:
                        return leftValue / rightValue;
                    default:
                        break;
                }

                throw new InvalidOperationException("Invalid binary operation.");
            }
        }

        public override string ToString() => $"{this.left?.ToString()} {oper} {this.right?.ToString()}";
    }
}
