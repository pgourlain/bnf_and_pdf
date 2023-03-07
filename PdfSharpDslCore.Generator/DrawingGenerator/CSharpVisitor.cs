using Irony.Parsing;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Extensions;
using PdfSharpDslCore.Generator.Evaluation;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Generator.DrawingGenerator
{

    interface IGeneratorState
    {
        StringBuilder Code { get; }

        void AppendLine(string text);
        void AppendMembersLine(string text);
        
        int Indentation { get; set; }
    }

    class CSharpVisitor : PdfVisitor<IGeneratorState>
    {
        private readonly string _prefix;
        int vNameIndex = 1;
        //Dictionary<string, string> _declaredVariables = new Dictionary<string, string>();
        Dictionary<string, EvaluationResult> _declaredVariables = new Dictionary<string, EvaluationResult>();
        Dictionary<string, IEvaluation<EvaluationResult>[]> _declaredFunctions = new Dictionary<string, IEvaluation<EvaluationResult>[]>();


        public CSharpVisitor(string baseDirectory, string prefix) : base(baseDirectory)
        {
            this._prefix = prefix;
        }

        public override void Draw(IGeneratorState state, ParseTree tree)
        {
            //TODO: indentation should be not here
            state.Indentation = 3;
            base.Draw(state, tree);
            state.Indentation = 2;
            //add formula functions and UDF
            foreach (var formulaFn in _declaredFunctions)
            {
                state.AppendMembersLine("//coucou");
            }
            
            foreach (var udf in UserDefinedFunctions)
            {
                state.AppendMembersLine($"//udf {udf.Key}");
                state.AppendMembersLine($"partial void {udf.Key}();");
            }
        }

        protected override void CustomVisit(IGeneratorState state, ParseTreeNode node)
        {
            //do nothing ignore all notimplemented node
        }

        protected override void ExecutePen(IGeneratorState state, ParseTreeNode widthNode, ParseTreeNode colorNode,
            ParseTreeNode styleNode)
        {
            var penvName = $"pen{vNameIndex++}";
            state.AppendLine($"var {penvName} = new XPen({ColorToString(colorNode.ParseColor())}, (double){EvaluateForString(widthNode).StringValue});");
            //_code.AppendLine($"var {penvName} = new XPen({ColorToString(value.Color)}, {value.Width});");
            if (styleNode != null && Enum.TryParse< XDashStyle>(styleNode.Token.ValueString, true, out var penStyle))
            {
                state.AppendLine($"{penvName}.DashStyle = XDashStyle.{penStyle.ToString()};");
            }
            state.AppendLine($"{_prefix}CurrentPen = {penvName};");
        }

        protected override void ExecuteSetVar(IGeneratorState state, ParseTreeNode node)
        {
            var varName = node.ChildNodes[1].Token.ValueString;
            var varResult = EvaluateForString(node.ChildNodes[3]);

            var prefixVar = $"{varResult.ValueType.ToString()} ";
            var value = varResult.StringValue;
            if (_declaredVariables.ContainsKey(varName))
            {
                prefixVar = string.Empty;
            }
            else
            {
                _declaredVariables.Add(varName, varResult);
            }
            var prefixValue = string.Empty;

            if (varResult.ValueType == typeof(string))
            {
                prefixValue = "@";
                //value = CSharpEvaluator.Quotify(value);
            }
            state.AppendLine($"{prefixVar}{varName} = {prefixValue}{value};");
        }

        private EvaluationResult EvaluateForString(ParseTreeNode node)
        {
            return new CSharpEvaluator(_prefix, node).EvaluateForCSharpString(_declaredVariables, _declaredFunctions);
        }

        uint Argb(XColor color)
        {
            var _a = color.A;
            var _r = color.R;
            var _g = color.G;
            var _b = color.B;
            return ((uint)(_a * 255) << 24) | ((uint)_r << 16) | ((uint)_g << 8) | _b;
        }

        private string ColorToString(XColor color)
        {
            if (color.IsKnownColor)
            {
                return $"XColors.{XColorResourceManager.GetKnownColor(Argb(color))}";
            }
            else
            {
                return $"XColor.FromArgb({color.A},{color.R},{color.G},{color.B})";
            }
        }
    }
}