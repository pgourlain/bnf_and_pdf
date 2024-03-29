﻿using Irony.Parsing;
using PdfSharpDslCore.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Evaluation
{
    internal class Evaluator
    {
        ParseTreeNode _rootNode;
        IDictionary<string, Func<object[], object>> _funcs;
        public Evaluator(ParseTreeNode rootNode, IDictionary<string, Func<object[], object>> funcs)
        {
            _rootNode = rootNode;
            _funcs = funcs;
        }

        public double? EvaluateForDouble(IDictionary<string, object?> variables)
        {
            var result = Evaluate(variables);
            if (result is null) return null;
            return Convert.ToDouble(result);
        }


        public object? Evaluate(IDictionary<string, object?> variables)
        {

            var result = PerformEvaluate(_rootNode, variables).Value;
            return result;
        }


        private IEvaluation<object> PerformEvaluate(ParseTreeNode node, IDictionary<string, object?> variables)
        {
            ParseTreeNode? opNode;
            ParseTreeNode? rightNode;
            IEvaluation<object> right;
            BinaryOperation op;
            switch (node.Term.Name)
            {
                case "UnaryExpression":
                    opNode = node.ChildNodes[0];
                    rightNode = node.ChildNodes[1];
                    right = PerformEvaluate(rightNode, variables);
                    op = BinaryOperation.Add;
                    switch (opNode.Term.Name)
                    {
                        case "+":
                            op = BinaryOperation.Add;
                            break;
                        case "-":
                            op = BinaryOperation.Sub;
                            break;
                    }
                    return new UnaryEvaluation(right, op);
                case "BinaryExpression":
                    var leftNode = node.ChildNodes[0];
                    opNode = node.ChildNodes[1];
                    rightNode = node.ChildNodes[2];
                    IEvaluation<object> left = PerformEvaluate(leftNode, variables);
                    right = PerformEvaluate(rightNode, variables);
                    op = BinaryOperation.Add;
                    switch (opNode.Term.Name)
                    {
                        case "+":
                            op = BinaryOperation.Add;
                            break;
                        case "-":
                            op = BinaryOperation.Sub;
                            break;
                        case "*":
                            op = BinaryOperation.Mul;
                            break;
                        case "/":
                            op = BinaryOperation.Div;
                            break;
                        case ">":
                            op = BinaryOperation.Superior;
                            break;
                        case "<":
                            op = BinaryOperation.Inferior;
                            break;
                        case ">=":
                            op = BinaryOperation.SuperiorOrEquals;
                            break;
                        case "<=":
                            op = BinaryOperation.InferiorOrEquals;
                            break;
                        case "<>":
                            op = BinaryOperation.NotEquals;
                            break;
                        case "==":
                            op = BinaryOperation.Equals;
                            break;
                        case "and":
                            op = BinaryOperation.And;
                            break;
                        case "or":
                            op = BinaryOperation.Or;
                            break;
                    }
                    return new BinaryEvaluation(left, right, op);
                case "number":
                    var value = Convert.ToDouble(node.Token.Value);
                    return new ConstantEvaluation<object>(value);
                case "FormulaExpression":
                    if (node.ChildNodes.Count == 1) return PerformEvaluate(node.ChildNodes[0], variables);
                    else
                    {
                        throw new NotImplementedException();
                    }
                case "VarRef":
                    return new VariableEvaluation((string)node.ChildNodes[1].Token.Value, variables);
                case "string":
                case "textstring":
                    return new ConstantEvaluation<object>(node.Token.Value);

                case "auto":
                    return new ConstantEvaluation<object>(null!);
                case "CustomFunctionExpression":
                    var fnName = (string)node.ChildNodes[0].Token.Value;
                    var args = node.ChildNode("CallInvokeArgumentslist");
                    var arguments = args?.ChildNodes.Select(n => PerformEvaluate(n, variables)).ToArray();
                    return new CustomFunctionEvaluation(_funcs[fnName.ToUpperInvariant()], arguments!);
            }

            throw new InvalidOperationException($"Unrecognizable term {node.Term.Name}.");
        }

    }
}
