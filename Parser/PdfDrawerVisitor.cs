using Irony.Parsing;
using Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;


namespace pdfsharpdsl.Parser
{
    internal class PdfDrawerVisitor
    {
        public PdfDrawerVisitor() { }

        public void Draw(PdfDocument pdf, ParseTree tree)
        {
            if (tree == null) return;

            using var drawer = new PdfDocumentDrawer(pdf);
            foreach (var node in tree.Root.ChildNodes)
            {
                Visit(drawer, node);
            }
        }

        private void Visit(PdfDocumentDrawer drawer, ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "SetSmt":
                    ExecuteSet(drawer, node.ChildNodes[1]);
                    break;
                case "RectSmt":
                    ExecuteRect(drawer, node.ChildNodes[1], false);
                    break;
                case "TextSmt":
                    ExecuteText(drawer, node);
                    break;
                case "LineTextSmt":
                    ExecuteLineText(drawer, node);
                    break;
                case "FillRectSmt":
                    ExecuteRect(drawer, node.ChildNodes[1], true) ;
                    break;
                default:
                    throw new NotImplementedException($"{node.Term.Name} is not yet implemented");
            }
        }

        private void ExecuteLineText(PdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNodes[1];
            var nodeAlignment = node.ChildNodes[2];
            var contentNode = node.ChildNodes[3];
            var text = (string?)contentNode.Token?.Value;

            if (text is not null)
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);
                var (hAlign, vAlign) = ParseTextAlignment(nodeAlignment.ChildNodes[0], nodeAlignment.ChildNodes[1]);
                drawer.DrawLineText(text, x, y, w, h, hAlign, vAlign);
            }
        }

        private void ExecuteText(PdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNodes[1];
            var contentNode = node.ChildNodes[2];
            var text = (string?)contentNode.Token?.Value;

            if (text is not null)
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);

                drawer.DrawText(text, x, y, w, h);
            }


            //sample to see how drawing text
            //drawer.DrawRect(5, 5, 10, 1);
            //drawer.DrawText("XLineAlignment.Near", 5, 5, null, null, PdfSharpCore.Drawing.XStringAlignment.Near, PdfSharpCore.Drawing.XLineAlignment.Near);
            //drawer.DrawRect(5, 50, 10, 1);
            //drawer.DrawText("XLineAlignment.BaseLine", 5, 50, null, null, PdfSharpCore.Drawing.XStringAlignment.Near, PdfSharpCore.Drawing.XLineAlignment.BaseLine);
            //drawer.DrawRect(5, 100, 10, 1);
            //drawer.DrawText("XLineAlignment.Far", 5, 100, null, null, PdfSharpCore.Drawing.XStringAlignment.Near, PdfSharpCore.Drawing.XLineAlignment.Far);
            //drawer.DrawRect(5, 150, 10, 1);
            //drawer.DrawText("XLineAlignment.Center", 5, 150, null, null, PdfSharpCore.Drawing.XStringAlignment.Near, PdfSharpCore.Drawing.XLineAlignment.Center);
        }

        private (XStringAlignment, XLineAlignment) ParseTextAlignment(ParseTreeNode hNode, ParseTreeNode vNode)
        {
            var hAlign = XStringAlignment.Near;
            var vAlign = XLineAlignment.Near;
            if (hNode.ChildNodes.Count > 0)
            {
                switch (hNode.ChildNodes[0].Token.Value)
                {
                    case "left":
                        break;
                    case "hcenter":
                        hAlign = XStringAlignment.Center;
                        break;
                    case "right":
                        hAlign = XStringAlignment.Far;
                        break;
                }
            }
            if (vNode.ChildNodes.Count > 0)
            {
                switch (vNode.ChildNodes[0].Token.Value)
                {
                    case "top":
                        break;
                    case "vcenter":
                        vAlign = XLineAlignment.Center;
                        break;
                    case "bottom":
                        vAlign = XLineAlignment.Far;
                        break;
                }
            }
            return (hAlign, vAlign);
        }

        private void ExecuteRect(PdfDocumentDrawer drawer, ParseTreeNode node, bool isFilled)
        {
            (double x, double y, double? w, double? h) = ParseRectLocation(node);
            drawer.DrawRect(x, y, w ?? 0, h ?? 0, isFilled);
            //drawer.DrawSample();
        }

        (double, double, double?, double?) ParseTextLocation(ParseTreeNode node)
        {
            if (node.Term.Name == "PointLocation")
            {
                (double x, double y) = ParsePointLocation(node);
                return (x, y, null, null);
            }
            else
            {
                return ParseRectLocation(node);
            }
        }
        private (double, double, double?, double?) ParseRectLocation(ParseTreeNode node)
        {
            (double? w, double? h) = (null, null);
            (double x, double y) = ParsePointLocation(node.ChildNodes[0]);
            if (node.ChildNodes.Count > 1)
            {
                (w, h) = ParsePointLocation(node.ChildNodes[1]);
            }
            return (x, y, w, h);
        }

        private (double x, double y) ParsePointLocation(ParseTreeNode node)
        {
            return (Convert.ToDouble(node.ChildNodes[0].Token.Value), Convert.ToDouble(node.ChildNodes[1].Token.Value));
        }

        private void ExecuteSet(PdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var executor = (Action<PdfDocumentDrawer, ParseTreeNode>)(node.Term.Name switch
            {
                "PenSmt" => ExecutePen,
                "BrushSmt" => ExecuteBrush,
                "FontSmt" => ExecuteFont,
                _ => NotImplemented
            });

            executor(drawer, node);
        }

        private void ExecutePen(PdfDocumentDrawer drawer, ParseTreeNode node)
        {

            var width = Convert.ToDouble(node.ChildNodes[2].Token.Value);
            var color = ParseColor(node.ChildNodes[1]);

            drawer.CurrentPen = new XPen(color, width);
        }

        private XColor ParseColor(ParseTreeNode node)
        {
            var executor = (Func<ParseTreeNode, XColor>)(node.ChildNodes[0].Term.Name switch
            {
                "NamedColor" => ParseNamedColor,
                _ => ParseHexColor,
            });

            return executor(node.ChildNodes[0]);
        }
        private XColor ParseNamedColor(ParseTreeNode node)
        {
            var color = (string)node.ChildNodes[0].Token.Value;

            var staticColor = typeof(XColors).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(x => string.Compare(x.Name, color, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
            return ((XColor?)staticColor?.GetValue(null)) ?? XColors.Black;
        }
        private XColor ParseHexColor(ParseTreeNode node)
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
                return XColor.FromArgb(Convert.ToInt32(colorValue));
            }
        }
        private void ExecuteBrush(PdfDocumentDrawer drawer, ParseTreeNode node)
        {

            var color = ParseColor(node.ChildNodes[1]);

            drawer.CurrentBrush = new XSolidBrush(color);
        }

        private void ExecuteFont(PdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var fontName = (string)node.ChildNodes[1].Token.Value;
            var fontSize = Convert.ToDouble(node.ChildNodes[2].Token.Value);
            var style = ParseStyle(node.ChildNodes[3]);
            drawer.CurrentFont = new XFont(fontName, fontSize, style);
        }

        private XFontStyle ParseStyle(ParseTreeNode node)
        {
            if (node.ChildNodes.Count > 0)
            {
                var styleName = (string?)node.ChildNodes[0].Token.Value;
                if (Enum.TryParse<XFontStyle>(styleName, true, out var fontStyle))
                {
                    return fontStyle;
                }
            }
            return XFontStyle.Regular;
        }

        private void NotImplemented(PdfDocumentDrawer drawer, ParseTreeNode node)
        {
            throw new NotImplementedException();
        }
    }
}
