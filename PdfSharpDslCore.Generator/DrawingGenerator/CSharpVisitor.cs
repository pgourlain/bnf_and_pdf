using Irony.Parsing;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Extensions;
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
        int Indentation { get;set; }
    }

    class CSharpVisitor : PdfVisitor<IGeneratorState>
    {
        private readonly string _prefix;
        int vNameIndex = 1;

        public CSharpVisitor(string baseDirectory, string prefix) : base (baseDirectory)
         {
            this._prefix = prefix;
        }

        protected override void CustomVisit(IGeneratorState state, ParseTreeNode node)
        {
            //do nothing ignore all notimplemented node
        }

        protected override void ExecutePen(IGeneratorState state, ParseTreeNode widthNode, ParseTreeNode colorNode, 
            ParseTreeNode styleNode)
        {
            var penvName = $"pen{vNameIndex++}";
            state.AppendLine($"var {penvName} = new XPen({ColorToString(colorNode.ParseColor())}, {1.0});");
            //_code.AppendLine($"var {penvName} = new XPen({ColorToString(value.Color)}, {value.Width});");
            //_code.AppendLine($"{penvName}.DashStyle = XDashStyle.{value.DashStyle};");
            state.AppendLine($"{_prefix}CurrentPen = {penvName};");
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