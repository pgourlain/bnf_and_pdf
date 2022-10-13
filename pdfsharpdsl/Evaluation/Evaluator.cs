using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdsl.Evaluation
{
    internal class Evaluator
    {
        ParseTreeNode _rootNode;
        public Evaluator(ParseTreeNode rootNode)
        {
            _rootNode = rootNode;
        }

        public double? Execute(/*variables*/)
        {
            var result = PerformEvaluate(_rootNode).Value;
            if (result is null) return null;
            return Convert.ToDouble(result);
        }

        private Evaluation PerformEvaluate(ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "BinaryExpression":
                    var leftNode = node.ChildNodes[0];
                    var opNode = node.ChildNodes[1];
                    var rightNode = node.ChildNodes[2];
                    Evaluation left = PerformEvaluate(leftNode);
                    Evaluation right = PerformEvaluate(rightNode);
                    BinaryOperation op = BinaryOperation.Add;
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
                    }
                    return new BinaryEvaluation(left, right, op);
                case "number":
                    var value = Convert.ToDouble(node.Token.Value);
                    return new ConstantEvaluation(value);
                case "NumberExpression":
                    if (node.ChildNodes.Count == 1) return PerformEvaluate(node.ChildNodes[0]);
                    else
                    {
                        throw new NotImplementedException();
                    }
                case "auto":
                    return new ConstantEvaluation(null);
            }

            throw new InvalidOperationException($"Unrecognizable term {node.Term.Name}.");
        }

    }
}
