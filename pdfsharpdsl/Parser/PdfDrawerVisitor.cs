using Irony.Parsing;
using Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using pdfsharpdsl.Evaluation;
using System.Data;

namespace pdfsharpdsl.Parser
{
    public class PdfDrawerVisitor
    {
        public PdfDrawerVisitor() { }

        public void Draw(IPdfDocumentDrawer drawer, ParseTree tree)
        {
            if (tree == null) return;
            if (drawer == null) throw new ArgumentNullException(nameof(drawer));

            foreach (var node in tree.Root.ChildNodes)
            {
                Visit(drawer, node);
            }
        }

        private void Visit(IPdfDocumentDrawer drawer, ParseTreeNode node)
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
                    ExecuteRect(drawer, node.ChildNodes[1], true);
                    break;
                case "ViewSizeSmt":
                    ExecuteViewSize(drawer, node.ChildNodes[1]);
                    break;
                case "NewPage":
                    ExecuteNewPage(drawer);
                    break;
                case "LineSmt":
                    ExecuteLine(drawer, node);
                    break;
                case "TableSmt":
                    ExecuteTable(drawer, node);
                    break;
                case "TitleSmt":
                    ExecuteTitle(drawer, node);
                    break;
                case "EllipseSmt":
                    ExecuteEllipse(drawer, node, false);
                    break;
                case "FillEllipseSmt":
                    ExecuteEllipse(drawer, node, true);
                    break;
                case "MoveToSmt":
                    ExecuteMoveTo(drawer, node);
                    break;
                case "LineToSmt":
                    ExecuteLineTo(drawer, node);
                    break;
                default:
                    throw new NotImplementedException($"{node.Term.Name} is not yet implemented");
            }
        }

        private void ExecuteLineTo(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            (double x, double y) = ParsePointLocation(node.ChildNodes[1]);
            drawer.LineTo(x, y);
        }

        private void ExecuteMoveTo(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            (double x, double y) = ParsePointLocation(node.ChildNodes[1]);
            drawer.MoveTo(x, y);
        }

        private void ExecuteEllipse(IPdfDocumentDrawer drawer, ParseTreeNode node, bool filled)
        {
            (double x, double y, double? x1, double? y1) = ParseRectLocation(node.ChildNodes[1]);
            drawer.DrawEllipse(x, y, x1 ?? 0, y1 ?? 0, filled);
        }

        private void ExecuteTitle(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var text = (string)node.ChildNodes[3].Token.Value;
            var margin = ParseNumber(node.ChildNodes[1]);
            var (hAlign, vAlign) = ParseTextAlignment(node.ChildNodes[2]);

            drawer.DrawTitle(text, margin ?? 0, hAlign, vAlign);
        }

        private double? ParseNumber(ParseTreeNode node)
        {
            if (node.ChildNodes.Count > 0)
            {
                return Convert.ToDouble(node.ChildNodes[0].Token.Value);
            }
            return null;
        }

        private void ExecuteTable(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            //throw new NotImplementedException();
        }

        private void ExecuteLine(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            (double x, double y, double? x1, double? y1) = ParseRectLocation(node.ChildNodes[1]);
            drawer.DrawLine(x, y, x1 ?? x, y1 ?? y);
        }


        private void ExecuteNewPage(IPdfDocumentDrawer drawer)
        {
            //throw new NotImplementedException();
            drawer.NewPage();
        }

        private void ExecuteViewSize(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            (double w, double h) = ParsePointLocation(node);
            drawer.SetViewSize(w, h);
        }

        private void ExecuteLineText(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNodes[1];
            var nodeAlignment = node.ChildNodes[2];
            var nodeOrientation = node.ChildNodes[3];
            var contentNode = node.ChildNodes[4];
            var text = (string?)contentNode.Token?.Value;
            TextOrientation textOrientation = new TextOrientation(TextOrientationEnum.Horizontal, null);

            if (nodeOrientation.ChildNodes.Count > 0)
            {
                if (nodeOrientation.ChildNodes[0].Term.Name == "number")
                {
                    textOrientation = textOrientation with { Angle = Convert.ToDouble(nodeOrientation.ChildNodes[0].Token.Value)};
                }
                else if (Enum.TryParse<TextOrientationEnum>((string)nodeOrientation.ChildNodes[0].Token.Value, true, out var specifiedOrientation))
                {
                    textOrientation = textOrientation with { Orientation = specifiedOrientation};
                }
            }
            if (text is not null)
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);
                var (hAlign, vAlign) = ParseTextAlignment(nodeAlignment.ChildNodes[0], nodeAlignment.ChildNodes[1]);
                drawer.DrawLineText(text, x, y, w, h, hAlign, vAlign, textOrientation);
            }
        }

        private void ExecuteText(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNodes[1];
            var contentNode = node.ChildNodes[2];
            var text = (string?)contentNode.Token?.Value;

            if (text is not null)
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);

                drawer.DrawText(text, x, y, w, h);
            }
        }

        private (XStringAlignment, XLineAlignment) ParseTextAlignment(ParseTreeNode alignNode)
        {
            ParseTreeNode? hNode = null;
            ParseTreeNode? vNode = null;
            if (alignNode.ChildNodes.Count > 0)
            {
                hNode = alignNode.ChildNodes[0];
            }
            if (alignNode.ChildNodes.Count > 1)
            {
                vNode = alignNode.ChildNodes[1];
            }
            return ParseTextAlignment(hNode, vNode);
        }
        private (XStringAlignment, XLineAlignment) ParseTextAlignment(ParseTreeNode? hNode, ParseTreeNode? vNode)
        {
            var hAlign = XStringAlignment.Near;
            var vAlign = XLineAlignment.Near;
            if (hNode != null && hNode.ChildNodes.Count > 0)
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
            if (vNode != null && vNode.ChildNodes.Count > 0)
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

        private void ExecuteRect(IPdfDocumentDrawer drawer, ParseTreeNode node, bool isFilled)
        {
            (double x, double y, double? w, double? h) = ParseRectLocation(node);
            drawer.DrawRect(x, y, w ?? 0, h ?? 0, isFilled);
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
            var x = Evaluate(node.ChildNodes[0]);
            var y = Evaluate(node.ChildNodes[1]);

            return (x, y);
        }

        private double Evaluate(ParseTreeNode node)
        {
            return new Evaluator(node).Execute();
        }

        private void ExecuteSet(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var executor = (Action<IPdfDocumentDrawer, ParseTreeNode>)(node.Term.Name switch
            {
                "PenSmt" => ExecutePen,
                "BrushSmt" => ExecuteBrush,
                "FontSmt" => ExecuteFont,
                _ => NotImplemented
            });

            executor(drawer, node);
        }

        private void ExecutePen(IPdfDocumentDrawer drawer, ParseTreeNode node)
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
        private void ExecuteBrush(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {

            var color = ParseColor(node.ChildNodes[1]);

            drawer.CurrentBrush = new XSolidBrush(color);
        }

        private void ExecuteFont(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var fontName = (string)node.ChildNodes[1].Token.Value;
            var fontSize = Convert.ToDouble(node.ChildNodes[2].Token.Value);
            var style = ParseStyle(node.ChildNodes[3]);
            //XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode);
            drawer.CurrentFont = new XFont(fontName, fontSize, style, XPdfFontOptions.UnicodeDefault);
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

        private void NotImplemented(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            throw new NotImplementedException();
        }
    }
}
