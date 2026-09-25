using Irony.Parsing;
using PdfSharpDslCore.Extensions;
using PdfSharpDslCore.Parser;
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
        IUserFunctionResolver? _userFunctions;
        public Evaluator(ParseTreeNode rootNode, IDictionary<string, Func<object[], object>> funcs,
            IUserFunctionResolver? userFunctions = null)
        {
            _rootNode = rootNode;
            _funcs = funcs;
            _userFunctions = userFunctions;
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
                        case "%":
                            op = BinaryOperation.Mod;
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
                    return new VariableEvaluation((string)node.ChildNodes[1].Token.Value, variables, node.Span.Location);
                case "string":
                case "textstring":
                    return new ConstantEvaluation<object>(node.Token.Value);

                case "ListExpression":
                    var listItems = node.ChildNode("CallInvokeArgumentslist")?.ChildNodes
                        .Select(n => PerformEvaluate(n, variables)).ToArray() ?? Array.Empty<IEvaluation<object>>();
                    return new ListEvaluation(listItems);
                case "AccessExpression":
                    var accessed = PerformEvaluate(node.ChildNodes[0], variables);
                    var steps = node.ChildNodes[1].ChildNodes.Select(suffix => suffix.Term.Name == "MemberSuffix"
                        ? new AccessStep(null, suffix.ChildNodes[0].Token.ValueString, suffix.Span.Location)
                        : new AccessStep(PerformEvaluate(suffix.ChildNodes[0], variables), null, suffix.Span.Location)).ToArray();
                    return new AccessEvaluation(accessed, steps);
                case "NamedColor":
                    return new ConstantEvaluation<object>(node.ChildNodes[0].Token.Text);
                case "auto":
                    return new ConstantEvaluation<object>(null!);
                case "CustomFunctionExpression":
                    var fnName = (string)node.ChildNodes[0].Token.Value;
                    var args = node.ChildNode("CallInvokeArgumentslist");
                    var arguments = args?.ChildNodes.Select(n => PerformEvaluate(n, variables)).ToArray();
                    if (!_funcs.TryGetValue(fnName.ToUpperInvariant(), out var func))
                    {
                        //not a registered function: a UDF of the script may be called as a function (it RETURNs a value)
                        func = _userFunctions?.Resolve(fnName.ToUpperInvariant());
                        if (func == null)
                        {
                            var known = _funcs.Keys.Concat(_userFunctions?.Names ?? Enumerable.Empty<string>());
                            throw new PdfParserException(PdfDslDiagnostics.UnknownFunction(fnName, node.Span.Location, known));
                        }
                    }
                    return new CustomFunctionEvaluation(func, arguments!);
            }

            throw new InvalidOperationException($"Unrecognizable term {node.Term.Name}.");
        }

    }
}
