using Irony.Parsing;
using PdfSharpDslCore.Drawing;
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

        public static PdfFontStyle ParseFontStyle(this ParseTreeNode? node)
        {
            if (node != null && node.Token != null)
            {
                var styleName = (string?)node.Token.Value;
                if (Enum.TryParse<PdfFontStyle>(styleName, true, out var fontStyle))
                {
                    return fontStyle;
                }
            }
            return PdfFontStyle.Regular;
        }

        public static PdfColor ParseColor(this ParseTreeNode node)
        {
            var executor = (Func<ParseTreeNode, PdfColor>)(node.ChildNodes[0].Term.Name switch
            {
                "NamedColor" => ParseNamedColor,
                _ => ParseHexColor,
            });

            return executor(node.ChildNodes[0]);
        }
        private static PdfColor ParseNamedColor(ParseTreeNode node)
        {
            var color = (string)node.ChildNodes[0].Token.Value;
            return PdfColors.FromName(color);
        }

        private static PdfColor ParseHexColor(ParseTreeNode node)
        {
            var colorValue = node.ChildNodes[0].Token.Value;
            if (colorValue is double)
            {
                return PdfColor.FromGrayScale(Convert.ToDouble(colorValue));
            }
            else
            {
                if (node.ChildNodes[0].Token.Length == 8)
                {
                    uint argb = ((uint)0xff000000) | Convert.ToUInt32(colorValue);
                    return PdfColor.FromArgb(argb);
                }
                else if (node.ChildNodes[0].Token.Length == 10)
                {
                    uint argb = unchecked((uint)Convert.ToInt32(colorValue));
                    return PdfColor.FromArgb(argb);
                }
                return PdfColor.FromArgb(unchecked((uint)Convert.ToInt32(colorValue)));
            }
        }

    }
}
