using Irony.Parsing;
using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PdfSharpDslCore.Generator.Evaluation
{
    internal class CSharpEvaluator
    {
        readonly ParseTreeNode _rootNode;
        readonly string _prefix;

        public CSharpEvaluator(string prefix, ParseTreeNode rootNode)
        {
            _rootNode = rootNode;
            _prefix = prefix;
        }

        public EvaluationResult EvaluateForCSharpString(IDictionary<string, EvaluationResult> variables,
            IDictionary<string, IEvaluation<EvaluationResult>[]> customFunctions)
        {
            var result = Evaluate(variables, customFunctions);
            return result;
        }
        public EvaluationResult Evaluate(IDictionary<string, EvaluationResult> variables,
            IDictionary<string, IEvaluation<EvaluationResult>[]> customFunctions)
        {
            var result = PerformEvaluate(_rootNode, variables, customFunctions).Value;
            return result;
        }

        private IEvaluation<EvaluationResult> PerformEvaluate(ParseTreeNode node, 
            IDictionary<string, EvaluationResult> variables,
            IDictionary<string, IEvaluation<EvaluationResult>[]> customFunctions)
        {
            ParseTreeNode opNode;
            ParseTreeNode rightNode;
            IEvaluation<EvaluationResult> right;
            string op;
            switch (node.Term.Name)
            {
                case "UnaryExpression":
                    opNode = node.ChildNodes[0];
                    rightNode = node.ChildNodes[1];
                    right = PerformEvaluate(rightNode, variables, customFunctions);
                    op = opNode.Term.Name;
                    return new UnaryEvaluationGenerator(right, op);
                case "BinaryExpression":
                    var leftNode = node.ChildNodes[0];
                    opNode = node.ChildNodes[1];
                    rightNode = node.ChildNodes[2];
                    IEvaluation<EvaluationResult> left = PerformEvaluate(leftNode, variables, customFunctions);
                    right = PerformEvaluate(rightNode, variables, customFunctions);
                    op = "+";
                    switch (opNode.Term.Name)
                    {
                        case "+":
                        case "-":
                        case "*":
                        case "/":
                        case ">":
                        case "<":
                        case ">=":
                        case "<=":
                        case "==":
                        case "%":
                            op = opNode.Term.Name;
                            break;
                        case "<>":
                            op = "!=";
                            break;
                        case "and":
                            op = "&&";
                            break;
                        case "or":
                            op = "||";
                            break;
                    }
                    return new BinaryEvaluationGenerator(left, right, op);
                case "number":
                    var value = Convert.ToDouble(node.Token.Value);
                    return new ConstantEvaluation<EvaluationResult>(new EvaluationResult
                    {
                        ValueType = typeof(double),
                        StringValue = value.ToString(CultureInfo.InvariantCulture)
                    });
                case "FormulaExpression":
                    if (node.ChildNodes.Count == 1) return PerformEvaluate(node.ChildNodes[0], variables, customFunctions);
                    else
                    {
                        throw new NotImplementedException();
                    }
                case "VarRef":
                    return new VariableEvaluationGenerator((string)node.ChildNodes[1].Token.Value, variables, _prefix);
                case "string":
                case "textstring":
                    return new ConstantEvaluation<EvaluationResult>(new EvaluationResult { ValueType = typeof(string), StringValue = $"\"{Quotify(node.Token.ValueString)}\"" });

                case "auto":
                    return new ConstantEvaluation<EvaluationResult>(EvaluationResult.Null);
                case "CustomFunctionExpression":
                    var fnName = (string)node.ChildNodes[0].Token.Value;
                    var args = node.ChildNode("CallInvokeArgumentslist");
                    var arguments = args?.ChildNodes.Select(n => PerformEvaluate(n, variables, customFunctions)).ToArray();
                    var ret = new CustomFunctionEvaluationGenerator(fnName.ToUpperInvariant(), arguments!);
                    if (!customFunctions.ContainsKey(fnName))
                    {
                        customFunctions.Add(fnName, arguments);
                    }
                    return ret;
            }

            throw new InvalidOperationException($"Unrecognizable term {node.Term.Name}.");
        }

        internal static string Quotify(string valueString)
        {
            if (!string.IsNullOrWhiteSpace(valueString))
            {
                var result = valueString.Replace("\"", "\"\"");
                return result;
            }
            return valueString;
        }
    }
}
