using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Evaluation
{
    internal class UnaryEvaluation : Evaluation<object>
    {
        private readonly IEvaluation<object> right;

        private readonly BinaryOperation oper;

        public UnaryEvaluation(IEvaluation<object> right, BinaryOperation oper)
        {
            this.right = right;
            this.oper = oper;
        }

        public override object? Value
        {
            get
            {
                var rightValue = this.right.Value;
                var isNan = false;
                if (rightValue == null)
                {
                    throw new InvalidOperationException("Right value of the unary evaluation has been evaluated to null.");
                }
                if (double.TryParse(rightValue.ToString(), out var dblValue))
                {
                    rightValue = dblValue;
                } else
                {
                    isNan = true;
                }

                switch (oper)
                {
                    case BinaryOperation.Add:
                        if (isNan)
                        {
                            return rightValue.ToString();
                        }
                        else
                        {
                            return (rightValue);
                        }
                    case BinaryOperation.Sub:
                        if (isNan) 
                        {
                            return "-" + rightValue.ToString();
                        }
                        else
                        {
                            double left = 0.0;
                            return (left - (double)rightValue);
                        }
                    default:
                        break;
                }

                throw new InvalidOperationException("Invalid unary operation.");
            }
        }

        public override string ToString() => $"{oper} {this.right?.ToString()}";
    }
}
