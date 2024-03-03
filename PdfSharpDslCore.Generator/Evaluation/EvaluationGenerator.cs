using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Evaluation;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Generator.Evaluation
{
    internal abstract class EvaluationGenerator : IEvaluation<EvaluationResult>
    {
        public abstract EvaluationResult Value { get; }
    }

    internal class UnaryEvaluationGenerator : EvaluationGenerator
    {
        private readonly IEvaluation<EvaluationResult> right;

        private readonly string oper;
        public UnaryEvaluationGenerator(IEvaluation<EvaluationResult> right, string oper)
        {
            this.right = right;
            this.oper = oper;
        }

        public override EvaluationResult Value
        {
            get
            {
                var rightValue = this.right.Value.StringValue;
                if (rightValue == null)
                {
                    if (this.right.Value.ValueType == typeof(string))
                    {
                        rightValue = string.Empty;
                    }
                    else
                    {
                        rightValue = "null";
                    }
                }

                switch (oper)
                {
                    case "+":
                        return new EvaluationResult() { StringValue = rightValue, ValueType = typeof(string) };
                    case "-":
                        return new EvaluationResult() { StringValue = $"-{rightValue}", ValueType = typeof(string) };
                    default:
                        break;
                }

                throw new InvalidOperationException("Invalid unary operation.");

            }
        }
    }

    internal class VariableEvaluationGenerator : EvaluationGenerator
    {
        readonly string _varName;
        readonly private IDictionary<string, EvaluationResult> variables;
        readonly string _prefix;

        public VariableEvaluationGenerator(string varName, IDictionary<string, EvaluationResult> variables, string prefix)
        {
            this._varName = varName;
            this.variables = variables;
            _prefix = prefix;
        }

        public override EvaluationResult Value
        {
            get
            {
                if (SystemVariableGet(_varName, out var result))
                {
                    return result;
                }
                if (variables.TryGetValue(_varName, out result))
                {
                    return new EvaluationResult { ValueType = result.ValueType, StringValue = _varName };
                }
                return new EvaluationResult()
                {
                    ValueType = typeof(string),
                    StringValue = $"Variable '{_varName}' is not declared"
                };
            }
        }

        private bool SystemVariableGet(string key, out EvaluationResult result)
        {
            result = EvaluationResult.Null;

            switch (key)
            {
                case "PAGEHEIGHT":
                    result = new EvaluationResult() { ValueType = typeof(double), StringValue = $"{_prefix}PageHeight" };
                    return true;
                case "PAGEWIDTH":
                    result = new EvaluationResult() { ValueType = typeof(double), StringValue = $"{_prefix}PageWidth" };
                    return true;
            }
            return false;
        }
    }

    internal class CustomFunctionEvaluationGenerator : EvaluationGenerator
    {
        readonly string _fnName;
        IEvaluation<EvaluationResult>[] _arguments;
        public CustomFunctionEvaluationGenerator(string fnName, IEvaluation<EvaluationResult>[] arguments)
        {
            _fnName = fnName;
            _arguments = arguments;
        }
        public override EvaluationResult Value
        {
            get
            {
                return new EvaluationResult {  StringValue = _fnName+"()", ValueType = typeof(object) };
            }
        }
    }
}
