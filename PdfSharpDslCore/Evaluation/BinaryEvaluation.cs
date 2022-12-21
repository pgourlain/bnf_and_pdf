using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Evaluation
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
                var leftValue = this.left.Value;
                var rightValue = this.right.Value;

                if (leftValue == null || rightValue == null)
                {
                    throw new InvalidOperationException("Either left or right value of the binary evaluation has been evaluated to null.");
                }
                bool leftIsNan = false;
                if (double.TryParse(leftValue.ToString(), out var dblValue))
                {
                    leftValue = dblValue;
                }
                else
                {
                    leftIsNan = true;
                }
                bool rightIsNan = false;
                if (double.TryParse(rightValue.ToString(), out dblValue))
                {
                    rightValue = dblValue;
                }
                else
                {
                    rightIsNan = true;
                }
                
                if (BooleanOperator(oper))
                {
                    return BooleanOperation(leftValue, rightValue, oper);
                }

                if (leftIsNan && rightIsNan)
                {
                    return StringOperation(leftValue, rightValue, oper);
                }
                else if (!leftIsNan && !rightIsNan)
                {
                    return DoubleOperation(leftValue, rightValue, oper);
                }
                else
                {
                    return StringOperation(leftValue, rightValue, oper);
                }


                throw new InvalidOperationException("Invalid binary operation.");
            }
        }

        private bool BooleanOperator(BinaryOperation oper)
        {
            switch (oper)
            {
                case BinaryOperation.Equals:
                case BinaryOperation.NotEquals:
                case BinaryOperation.InferiorOrEquals:
                case BinaryOperation.Inferior:
                case BinaryOperation.Superior:
                case BinaryOperation.SuperiorOrEquals:
                case BinaryOperation.And:
                case BinaryOperation.Or:
                    return true;
                default:
                    return false;

            }
        }

        private double DoubleOperation(object leftValue, object rightValue, BinaryOperation oper)
        {
            var l = (double)leftValue;
            var r = (double)rightValue;
            switch (oper)
            {
                case BinaryOperation.Add:
                    return l + r;
                case BinaryOperation.Sub:
                    return l - r;
                case BinaryOperation.Mul:
                    return l * r;
                case BinaryOperation.Div:
                    return l / r;
                case BinaryOperation.Mod:
                    return l % r;
                default:
                    throw new NotSupportedException("Operation not supported on double");
            }
        }

        private object StringOperation(object leftValue, object rightValue, BinaryOperation oper)
        {
            switch (oper)
            {
                case BinaryOperation.Add:
                    return leftValue.ToString() + rightValue.ToString();
                default:
                    throw new NotSupportedException("Operation not supported on string");
            }
        }

        private object BooleanOperation(object leftValue, object rightValue, BinaryOperation oper)
        {
            try
            {


                switch (oper)
                {
                    case BinaryOperation.And:
                        return Convert.ToBoolean(leftValue) && Convert.ToBoolean(rightValue);
                    case BinaryOperation.Or:
                        return Convert.ToBoolean(leftValue) || Convert.ToBoolean(rightValue);

                    case BinaryOperation.Superior:
                        return Convert.ToDouble(leftValue) > Convert.ToDouble(rightValue);
                    case BinaryOperation.SuperiorOrEquals:
                        return Convert.ToDouble(leftValue) >= Convert.ToDouble(rightValue);
                    case BinaryOperation.Inferior:
                        return Convert.ToDouble(leftValue) < Convert.ToDouble(rightValue);
                    case BinaryOperation.InferiorOrEquals:
                        return Convert.ToDouble(leftValue) <= Convert.ToDouble(rightValue);
                    case BinaryOperation.Equals:
                        return leftValue.ToString() == rightValue.ToString();
                    case BinaryOperation.NotEquals:
                        return leftValue.ToString() != rightValue.ToString();
                    default:
                        throw new NotSupportedException("Operation not supported on boolean");
                }
            }
            catch(FormatException)
            {
                throw new NotSupportedException($"'{oper}' is only supported on number expression.");
            }
        }

        public override string ToString() => $"{this.left?.ToString()} {oper} {this.right?.ToString()}";
    }
}
