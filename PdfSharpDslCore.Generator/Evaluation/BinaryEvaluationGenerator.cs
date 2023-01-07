using PdfSharpDslCore.Evaluation;
using System;
using System.Globalization;

namespace PdfSharpDslCore.Generator.Evaluation
{
    internal class BinaryEvaluationGenerator : EvaluationGenerator
    {
        private readonly IEvaluation<EvaluationResult> left;
        private readonly IEvaluation<EvaluationResult> right;

        private readonly string oper;

        public BinaryEvaluationGenerator(IEvaluation<EvaluationResult> left, IEvaluation<EvaluationResult> right, string oper)
        {
            this.left = left;
            this.right = right;
            this.oper = oper;
        }

        public override EvaluationResult Value
        {
            get
            {
                var leftValue = this.left.Value;
                var rightValue = this.right.Value;

                if (leftValue == EvaluationResult.Null || rightValue == EvaluationResult.Null)
                {
                    throw new InvalidOperationException("Either left or right value of the binary evaluation has been evaluated to null.");
                }
                bool leftIsNan = leftValue.ValueType != typeof(double);
                bool rightIsNan = rightValue.ValueType != typeof(double);

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

        private bool BooleanOperator(string oper)
        {
            switch (oper)
            {
                case "==":
                case "!=":
                case "<=":
                case "<":
                case ">":
                case ">=":
                case "&&":
                case "||":
                    return true;
                default:
                    return false;

            }
        }

        private EvaluationResult DoubleOperation(EvaluationResult leftValue, EvaluationResult rightValue, string oper)
        {
            var stringValue = $"{leftValue.StringValue} {oper} {rightValue.StringValue}";
            return new EvaluationResult { ValueType = typeof(double), StringValue = stringValue };
        }

        private EvaluationResult StringOperation(EvaluationResult leftValue, EvaluationResult rightValue, string oper)
        {
            switch (oper)
            {
                case "+":
                    return new EvaluationResult { ValueType = typeof(string), 
                            StringValue = $"@{leftValue.StringValue} + @{rightValue.StringValue}" };
                default:
                    throw new NotSupportedException("Operation not supported on string");
            }
        }

        private EvaluationResult BooleanOperation(EvaluationResult leftValue, EvaluationResult rightValue, string oper)
        {
            try
            {

                var stringValue =$"(bool){leftValue.StringValue} {oper} (bool){rightValue.StringValue}";
                return new EvaluationResult {  ValueType = typeof(Boolean), StringValue = stringValue };
            }
            catch (FormatException)
            {
                throw new NotSupportedException($"'{oper}' is only supported on number expression.");
            }
        }       
    }
}
