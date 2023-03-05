using Irony.Parsing;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("PdfSharpDslCore.Generator")]

namespace PdfSharpDslCore.Extensions
{
    internal static class ParseTreeNodeExtensions
    {
        public static IEnumerable<ParseTreeNode> ChildNodes(this ParseTreeNode node, string termName)
        {
            List<ParseTreeNode> result = new List<ParseTreeNode>();
            Queue<ParseTreeNode> queue = new Queue<ParseTreeNode>();

            queue.Enqueue(node);

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (n.Term != null && n.Term.Name == termName)
                {
                    result.Add(n);
                }
                foreach (var item in n.ChildNodes)
                {
                    queue.Enqueue(item);
                }
            }
            return result.AsReadOnly();
        }

        public static ParseTreeNode? ChildNode(this ParseTreeNode node, string termName)
        {
            return node.ChildNodes.FirstOrDefault(n => n.Term != null && n.Term.Name == termName);
        }

        public static XFontStyle ParseFontStyle(this ParseTreeNode? node)
        {
            if (node != null && node.Token != null)
            {
                var styleName = (string?)node.Token.Value;
                if (Enum.TryParse<XFontStyle>(styleName, true, out var fontStyle))
                {
                    return fontStyle;
                }
            }
            return XFontStyle.Regular;
        }

        public static XColor ParseColor(this ParseTreeNode node)
        {
            var executor = (Func<ParseTreeNode, XColor>)(node.ChildNodes[0].Term.Name switch
            {
                "NamedColor" => ParseNamedColor,
                _ => ParseHexColor,
            });

            return executor(node.ChildNodes[0]);
        }
        private static XColor ParseNamedColor(ParseTreeNode node)
        {
            var color = (string)node.ChildNodes[0].Token.Value;

            var staticColor = typeof(XColors)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => string.Compare(x.Name, color, StringComparison.OrdinalIgnoreCase) == 0);
            return ((XColor?)staticColor?.GetValue(null)) ?? XColors.Black;
        }

        private static XColor ParseHexColor(ParseTreeNode node)
        {
            var colorValue = node.ChildNodes[0].Token.Value;
            if (colorValue is double)
            {
                return XColor.FromGrayScale(Convert.ToDouble(colorValue));
            }
            else
            {
                if (node.ChildNodes[0].Token.Length == 8)
                {
                    uint argb = ((uint)0xff000000) | Convert.ToUInt32(colorValue);
                    return XColor.FromArgb(argb);
                }
                else if (node.ChildNodes[0].Token.Length == 10)
                {
                    int argb = Convert.ToInt32(colorValue);
                    return XColor.FromArgb(argb);
                }
                return XColor.FromArgb(Convert.ToInt32(colorValue));
            }
        }

    }
}
