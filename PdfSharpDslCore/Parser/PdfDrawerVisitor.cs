using Irony.Parsing;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Extensions;
using SixLabors.ImageSharp;
using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Linq;

namespace PdfSharpDslCore.Parser
{
    /// <summary>
    /// Visitor of parsed tree to generate PDF
    /// </summary>
    public class PdfDrawerVisitor : PdfVisitor<IPdfDocumentDrawer>
    {

        public PdfDrawerVisitor() : this(Environment.CurrentDirectory) { }
        public PdfDrawerVisitor(string baseDirectory) : base(baseDirectory)
        {
        }

        public void Draw(IPdfDocumentDrawer drawer, ParseTree tree)
        {
            if (tree == null) return;
            if (drawer == null) throw new ArgumentNullException(nameof(drawer));
            _variables = new VariablesDictionary(drawer);

            //define each udf before visiting in order to accept call before definition
            tree.Root.ChildNodes.Where(x => x.Term?.Name == "UdfSmt").ToList().ForEach(ExecuteUdfStatement);
            Visit(drawer, tree.Root.ChildNodes);
        }

        protected override void CustomVisit(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "TextSmt":
                    ExecuteText(drawer, node);
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
                case "MoveToSmt":
                    ExecuteMoveTo(drawer, node);
                    break;
                case "LineToSmt":
                    ExecuteLineTo(drawer, node);
                    break;
                case "ImageSmt":
                    ExecuteImage(drawer, node);
                    break;
                case "PolygonSmt":
                    ExecutePolygon(drawer, node, false);
                    break;
                case "FillPolygonSmt":
                    ExecutePolygon(drawer, node, true);
                    break;
                case "ForSmt":
                    ExecuteForStatement(drawer, node);
                    break;
                case "UdfSmt":
                    //nothing to do, it's already done before
                    break;
                case "UdfInvokeSmt":
                    ExecuteUdfInvokeStatement(drawer, node);
                    break;
                // case "IfSmt":
                //     ExecuteIfStatement(drawer, node);
                //     break;
                default:
                    throw new NotImplementedException($"{node.Term.Name} is not yet implemented");
            }
        }

        protected override void ExecuteRect(IPdfDocumentDrawer state, ParseTreeNode rectNode, bool isFilled)
        {
            (double x, double y, double? w, double? h) = ParseRectLocation(rectNode);
            state.DrawRect(x, y, w ?? 0, h ?? 0, isFilled);
        }
        protected override void ExecutePen(IPdfDocumentDrawer state, ParseTreeNode widthNode, ParseTreeNode colorNode, ParseTreeNode styleNode)
        {
            var width = EvaluateForDouble(widthNode) ?? 0;
            var color = ParseColor(colorNode);
            XDashStyle style = XDashStyle.Solid;
            if (styleNode != null)
            {
                Enum.TryParse<XDashStyle>(styleNode.Token.ValueString, true, out style);
            }
            var pen = new XPen(color, width);
            pen.DashStyle = style;
            state.CurrentPen = pen;
        }
        protected override void ExecuteHBrush(IPdfDocumentDrawer drawer, ParseTreeNode colorNode)
        {
            var color = ParseColor(colorNode);
            if (color.A == 0)
            {
                drawer.HighlightBrush = null;
            }
            else
            {
                drawer.HighlightBrush = new XSolidBrush(color);
            }
        }

        protected override void ExecuteFont(IPdfDocumentDrawer drawer, ParseTreeNode fontNode)
        {
            drawer.CurrentFont = ExtractFont(fontNode);
        }

        protected override void ExecuteNewPage(IPdfDocumentDrawer drawer,
            ParseTreeNode sizeNode,
            ParseTreeNode orientationNode)
        {
            var nSize = sizeNode;
            var nOrientation = orientationNode;
            PageSize? pageSize = null;
            if (nSize.ChildNodes.Count > 0 && Enum.TryParse<PageSize>(nSize.ChildNodes[0].Token.Text, out var size))
            {
                pageSize = size;
            }
            PageOrientation? pageOrientation = null;
            if (nOrientation.ChildNodes.Count > 0 && Enum.TryParse<PageOrientation>(nOrientation.ChildNodes[0].Token.Text, true, out var orientation))
            {
                pageOrientation = orientation;
            }
            drawer.NewPage(pageSize, pageOrientation);
        }

        protected override void ExecuteIfStatement(IPdfDocumentDrawer state, ParseTreeNode condNode,
            ParseTreeNode? ifNode, ParseTreeNode? elseNode)
        {
            var condition = Convert.ToBoolean(EvaluateForObject(condNode, _variables, _customFunctions));
            ParseTreeNode? nodeToVisit = condition ? ifNode : elseNode;
            if (nodeToVisit != null)
            {
                Visit(state, nodeToVisit.ChildNodes);
            }
        }
        protected override void ExecutePie(IPdfDocumentDrawer state, ParseTreeNode locationNode,
            ParseTreeNode startAngleNode,
            ParseTreeNode sweepAngleNode,
            bool isFilled)
        {
            var (x, y, w, h) = ParseRectLocation(locationNode);
            var startAngle = EvaluateForDouble(startAngleNode) ?? 0;
            var sweepAngle = EvaluateForDouble(sweepAngleNode) ?? 0;

            state.DrawPie(x, y, w, h, startAngle, sweepAngle, isFilled);
        }

        protected override void ExecuteViewSize(IPdfDocumentDrawer state, ParseTreeNode sizeNode)
        {
            (double w, double h) = ParsePointLocation(sizeNode);
            state.SetViewSize(w, h);
        }

        protected override void ExecuteEllipse(IPdfDocumentDrawer state, ParseTreeNode node, bool filled)
        {
            (double x, double y, double? x1, double? y1) = ParseRectLocation(node.ChildNodes[1]);
            state.DrawEllipse(x, y, x1 ?? 0, y1 ?? 0, filled);
        }




        private void ExecuteUdfInvokeStatement(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var fnName = node.ChildNodes[1].Token.ValueString;
            var arguments = node.ChildNode("CallInvokeArgumentslist");
            var evaluatedArgs = arguments.ChildNodes.Select(x => EvaluateForObject(x, _variables, _customFunctions)).ToArray();

            if (_udfs.TryGetValue(fnName, out var defNode))
            {
                var defArgs = defNode.ChildNode("UdfArgumentslist");
                var defBody = defNode.ChildNode("UdfBlock").ChildNode("EmbbededSmtList");
                if (defArgs.ChildNodes.Count != evaluatedArgs.Length)
                {
                    throw new PdfParserException($"UDF '{fnName}' arguments count not match, provided ${evaluatedArgs.Length}, expected ${defArgs.ChildNodes.Count}.");
                }
                var vars = _variables;
                if (vars is IVariablesDictionary savable) savable.SaveVariables();
                try
                {
                    for (int i = 0; i < defArgs.ChildNodes.Count; i++)
                    {
                        var defVar = defArgs.ChildNodes[i];
                        _variables.Add(defVar.Token.ValueString, evaluatedArgs[i] ?? null!);
                    }

                    Visit(drawer, defBody.ChildNodes);
                }
                finally
                {
                    //replace IT
                    if (vars is IVariablesDictionary restorable) restorable.RestoreVariables();
                }
            }
            else if (!UdfCustomCall(drawer, fnName, evaluatedArgs))
            {
                throw new PdfParserException($"UDF {fnName} is not found.");
            }

        }

        protected virtual bool UdfCustomCall(IPdfDocumentDrawer drawer, string udfName, object?[] arguments)
        {
            return false;
        }

        private void ExecuteUdfStatement(ParseTreeNode node)
        {
            var fnName = node.ChildNodes[0].Token.ValueString;
            if (_udfs.ContainsKey(fnName))
            {
                throw new PdfParserException($"An another UDF '{fnName}' is already defined.");
            }
            _udfs.Add(fnName, node);
        }

        private void ExecuteForStatement(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var varName = InternalSetVar(node);
            var from = Convert.ToInt32(_variables[varName]);
            var to = Convert.ToInt32(EvaluateForObject(node.ChildNodes[5], _variables, _customFunctions));
            var forbody = node.ChildNode("ForBlock").ChildNode("EmbbededSmtList");
            if (forbody != null)
            {
                for (int i = from; i <= to; i++)
                {
                    _variables.Add(varName, i);
                    Visit(drawer, forbody.ChildNodes);
                }
            }
        }

        private void ExecutePolygon(IPdfDocumentDrawer drawer, ParseTreeNode node, bool isFilled)
        {
            List<XPoint> points = new List<XPoint>();

            var (x, y) = ParsePointLocation(node.ChildNodes[1]);
            points.Add(new XPoint(x, y));
            (x, y) = ParsePointLocation(node.ChildNodes[2]);
            points.Add(new XPoint(x, y));
            var polygonPoint = node.ChildNode("PolygonPoint");
            foreach (var ptNode in polygonPoint.ChildNodes)
            {
                (x, y) = ParsePointLocation(ptNode);
                points.Add(new XPoint(x, y));
            }
            drawer.DrawPolygon(points, isFilled);

        }

        private void ExecuteImage(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            string unit = "point";
            bool crop = false;
            var imageLocation = node.ChildNode("ImageLocation");
            var isEmbedded = node.ChildNode("ImageRawOrSource").ChildNodes[0].Token.ValueString == "Data";
            var imagePath = ((string?)node.ChildNodes[3].Token?.Value) ?? string.Empty;
            var (x, y, w, h) = ParseTextLocation(imageLocation.ChildNodes[0]);
            if (w is not null && imageLocation.ChildNodes.Count > 1)
            {
                //try to parse unit and cropping
                unit = imageLocation.ChildNodes[1].Term.Name;
                crop = imageLocation.ChildNodes[2].ChildNodes.Count > 0;
            }
            XImage image;
            if (isEmbedded && !string.IsNullOrWhiteSpace(imagePath))
            {
                if (imagePath.StartsWith("data:image"))
                {
                    imagePath = imagePath.Split(',')[1];
                }
                using var stream = new MemoryStream(System.Convert.FromBase64String(imagePath));
                image = XImage.FromStream(() => stream);
            }
            else
            {
                if (Directory.Exists(this.BaseDirectory) && !Path.IsPathRooted(imagePath))
                {
                    imagePath = Path.Combine(this.BaseDirectory, imagePath);
                }
                image = XImage.FromFile(imagePath);
            }
            using (image)
            {
                drawer.DrawImage(image, x, y, w, h, unit == "pixel", crop);
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

        private void ExecuteTitle(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var text = Convert.ToString(EvaluateForObject(node.ChildNodes[4], _variables, _customFunctions));
            var margin = ParseMargin(node.ChildNodes[1]);
            var (hAlign, vAlign) = ParseTextAlignment(node.ChildNodes[2]);

            drawer.DrawTitle(text, margin ?? 0, hAlign, vAlign);
        }

        private double? ParseMargin(ParseTreeNode node)
        {
            if (node.ChildNodes.Count > 0)
            {
                return EvaluateForDouble(node.ChildNodes[2]);
            }
            return null;
        }

        private void ExecuteTable(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            //another way is to use MigraDoc to draw Table
            //it can be used later
            //rows that contains data, desired height and width
            //
            var (x, y) = ParsePointLocation(node.ChildNodes[0].ChildNodes[0]);
            var tblDef = GenerateTableDefinition(node.ChildNodes[1]);
            drawer.DrawTable(x, y, tblDef);
        }

        private TableDefinition GenerateTableDefinition(ParseTreeNode node)
        {
            var result = new TableDefinition();
            var headStyle = node.ChildNodes("TableHeadStyle").FirstOrDefault();
            if (headStyle != null && headStyle.ChildNodes.Count > 0)
            {
                var color = ParseColor(headStyle.ChildNodes[0]);
                result.HeaderBackColor = new XSolidBrush(color);
            }
            GenerateTableHead(node.ChildNodes("TableHeadCol"), result);
            GenerateTableRows(node.ChildNodes("TableRow"), result);
            return result;
        }

        private void GenerateTableRows(IEnumerable<ParseTreeNode> nodes, TableDefinition tbl)
        {
            foreach (var row in nodes)
            {
                var rowDef = new RowDefinition();

                var styleNode = row.ChildNode("TableRowStyle");
                if (styleNode?.ChildNodes.Count > 0)
                {
                    var rowHeight = EvaluateForDouble(styleNode.ChildNodes[0]);
                    rowDef.DesiredHeight = rowHeight;
                }
                var cols = row.ChildNodes("TableCol").SelectMany(x => x.ChildNodes).Where(x => x.Term?.Name != "COL").ToArray();


                var rowData = cols.Select(x => x.Token.ValueString).ToList();
                while (rowData.Count < tbl.Columns.Count)
                {
                    rowData.Add(string.Empty);
                }
                rowDef.Data = rowData.ToArray();
                tbl.Rows.Add(rowDef);
            }
        }

        private void GenerateTableHead(IEnumerable<ParseTreeNode> nodes, TableDefinition tbl)
        {
            foreach (var col in nodes)
            {
                var colDef = new ColumnDefinition();
                //width
                var colWidthNode = col.ChildNodes[1];
                if (colWidthNode.ChildNodes.Count == 2)
                {
                    var desiredWidth = EvaluateForDouble(colWidthNode.ChildNodes[0]);
                    var maxWidth = EvaluateForDouble(colWidthNode.ChildNodes[1]);
                    colDef.MaxWidth = maxWidth;
                    colDef.DesiredWidth = desiredWidth;

                }
                //font
                var colFontNode = col.ChildNode("TableColFont");
                if (colFontNode?.ChildNodes.Count > 0)
                {
                    colDef.Font = ExtractFont(colFontNode);
                }
                var colors = col.ChildNode("TableColColors");
                if (colors?.ChildNodes.Count > 0)
                {
                    colDef.Brush = new XSolidBrush(ParseColor(colors.ChildNodes[0]));
                    colDef.BackColor = new XSolidBrush(ParseColor(colors.ChildNodes[1]));
                }
                //name
                colDef.ColumnHeaderName = col.ChildNodes.Last().Token.ValueString;

                tbl.Columns.Add(colDef);
            }
        }

        private void ExecuteLine(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            (double x, double y, double? x1, double? y1) = ParseRectLocation(node.ChildNodes[1]);
            drawer.DrawLine(x, y, x1 ?? x, y1 ?? y);
        }


        protected override void ExecuteLineText(IPdfDocumentDrawer state,
            ParseTreeNode nodeLocation,
            ParseTreeNode nodeAlignment,
            ParseTreeNode? nodeOrientation,
            ParseTreeNode contentNode)
        {
            var text = Convert.ToString(EvaluateForObject(contentNode, _variables, _customFunctions));
            TextOrientation textOrientation = new TextOrientation { Orientation = TextOrientationEnum.Horizontal, Angle = null };

            if (nodeOrientation != null && nodeOrientation.ChildNodes.Count > 2)
            {
                nodeOrientation = nodeOrientation.ChildNodes[2];
                if (nodeOrientation.Term.Name == "number" || nodeOrientation.Token is null)
                {
                    textOrientation = textOrientation with { Angle = EvaluateForDouble(nodeOrientation) };
                }
                else if (Enum.TryParse<TextOrientationEnum>((string)nodeOrientation.Token.Value, true, out var specifiedOrientation))
                {
                    textOrientation = textOrientation with { Orientation = specifiedOrientation };
                }
            }
            if (text is not null)
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);
                var (hAlign, vAlign) = ParseTextAlignment(nodeAlignment.ChildNodes[0], nodeAlignment.ChildNodes[1]);
                state.DrawLineText(text, x, y, w, h, hAlign, vAlign, textOrientation);
            }

        }

        private void ExecuteText(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNodes[1];
            var contentNode = node.ChildNodes[2];
            var text = Convert.ToString(EvaluateForObject(contentNode, _variables, _customFunctions));

            if (text is not null)
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);

                drawer.DrawText(text, x, y, w, h);
            }
        }

        private static (XStringAlignment, XLineAlignment) ParseTextAlignment(ParseTreeNode alignNode)
        {
            ParseTreeNode? hNode = alignNode.Term.Name == "HAlign" ? alignNode : null;
            ParseTreeNode? vNode = alignNode.Term.Name == "VAlign" ? alignNode : null;
            return ParseTextAlignment(hNode, vNode);
        }
        private static (XStringAlignment, XLineAlignment) ParseTextAlignment(ParseTreeNode? hNode, ParseTreeNode? vNode)
        {
            var hAlign = XStringAlignment.Near;
            var vAlign = XLineAlignment.Near;
            if (hNode != null && hNode.ChildNodes.Count > 2)
            {
                switch (hNode.ChildNodes[2].Token.Value)
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
            if (vNode != null && vNode.ChildNodes.Count > 2)
            {
                switch (vNode.ChildNodes[2].Token.Value)
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
            var x = EvaluateForDouble(node.ChildNodes[0]) ?? 0;
            var y = EvaluateForDouble(node.ChildNodes[1]) ?? 0;

            return (x, y);
        }

        private double? EvaluateForDouble(ParseTreeNode node)
        {
            return EvaluateForDouble(node, _variables, _customFunctions);
        }
        private static double? EvaluateForDouble(ParseTreeNode node, IDictionary<string, object?> variables,
            IDictionary<string, Func<object[], object>> funcs)
        {
            return new Evaluator(node, funcs).EvaluateForDouble(variables);
        }

        private static object? EvaluateForObject(ParseTreeNode node, IDictionary<string, object?> variables,
            IDictionary<string, Func<object[], object>> funcs)
        {
            return new Evaluator(node, funcs).Evaluate(variables);
        }

        protected override void ExecuteSetVar(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            InternalSetVar(node);
        }

        private string InternalSetVar(ParseTreeNode node)
        {
            var v = EvaluateForObject(node.ChildNodes[3], _variables, _customFunctions);
            var varName = node.ChildNodes[1].Token.ValueString;
            _variables.Add(varName, v);
            return varName;
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
                else if (node.ChildNodes[0].Token.Length == 10)
                {
                    int argb = Convert.ToInt32(colorValue);
                    return XColor.FromArgb(argb);
                }
                return XColor.FromArgb(Convert.ToInt32(colorValue));
            }
        }
        protected override void ExecuteBrush(IPdfDocumentDrawer drawer, ParseTreeNode colorNode)
        {

            var color = ParseColor(colorNode);

            drawer.CurrentBrush = new XSolidBrush(color);
        }


        private XFont ExtractFont(ParseTreeNode node)
        {
            int index = 0;
            var fontName = (string)node.ChildNodes[1].Token.Value;
            if (node.Term.Name != "FontSmt")
            {
                index++;
            }
            var fontSize = EvaluateForDouble(node.ChildNodes[2 + index], _variables, _customFunctions) ?? 0;
            var style = ParseStyle(node.ChildNodes.Count > (3 + index) ? node.ChildNodes[3 + index] : null);
            return new XFont(fontName, fontSize, style, XPdfFontOptions.UnicodeDefault);
        }

        private static XFontStyle ParseStyle(ParseTreeNode? node)
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
    }
}
