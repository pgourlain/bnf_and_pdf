using Irony.Parsing;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        public override void Draw(IPdfDocumentDrawer state, ParseTree tree)
        {
            Variables = new VariablesDictionary(k => SystemVariableGet(state, k));
            base.Draw(state, tree);
        }

        protected override void CustomVisit(IPdfDocumentDrawer drawer, ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "TableSmt":
                    ExecuteTable(drawer, node);
                    break;
                default:
                    throw new NotImplementedException($"{node.Term.Name} is not yet implemented");
            }
        }

        #region overrides

        protected override void ExecuteRect(IPdfDocumentDrawer state, ParseTreeNode rectNode, bool isFilled)
        {
            (double x, double y, double? w, double? h) = ParseRectLocation(rectNode);
            state.DrawRect(x, y, w ?? 0, h ?? 0, isFilled);
        }
        protected override void ExecutePen(IPdfDocumentDrawer state, ParseTreeNode widthNode, ParseTreeNode colorNode, ParseTreeNode styleNode)
        {
            var width = EvaluateForDouble(widthNode) ?? 0;
            var color = colorNode.ParseColor();
            XDashStyle style = XDashStyle.Solid;
            if (styleNode != null)
            {
                Enum.TryParse<XDashStyle>(styleNode.Token.ValueString, true, out style);
            }
            var pen = new XPen(color, width)
            {
                DashStyle = style
            };
            state.CurrentPen = pen;
        }
        protected override void ExecuteHBrush(IPdfDocumentDrawer drawer, ParseTreeNode colorNode)
        {
            var color = colorNode.ParseColor();
            drawer.HighlightBrush = color.A == 0 ? null : new XSolidBrush(color);
        }

        protected override void ExecuteFont(IPdfDocumentDrawer drawer, ParseTreeNode fontNode)
        {
            drawer.CurrentFont = ExtractFont(fontNode);
        }

        protected override void ExecuteNewPage(IPdfDocumentDrawer drawer,
            ParseTreeNode? sizeNode,
            ParseTreeNode? orientationNode)
        {
            var nSize = sizeNode;
            var nOrientation = orientationNode;
            PageSize? pageSize = null;
            if (nSize != null && Enum.TryParse<PageSize>(nSize.Token.Text, out var size))
            {
                pageSize = size;
            }
            PageOrientation? pageOrientation = null;
            if (nOrientation != null && Enum.TryParse<PageOrientation>(nOrientation.Token.Text, true, out var orientation))
            {
                pageOrientation = orientation;
            }
            drawer.NewPage(pageSize, pageOrientation);
        }

        protected override void ExecuteIfStatement(IPdfDocumentDrawer state, ParseTreeNode condNode,
            ParseTreeNode? ifNode, ParseTreeNode? elseNode)
        {
            var condition = Convert.ToBoolean(EvaluateForObject(condNode));
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

        protected override void ExecuteTitle(IPdfDocumentDrawer drawer, ParseTreeNode marginNode,
           ParseTreeNode alignmentsNode,
           ParseTreeNode contentNode)
        {
            var text = Convert.ToString(EvaluateForObject(contentNode));
            var margin = ParseMargin(marginNode);
            var (hAlign, vAlign) = ParseTextAlignment(alignmentsNode);

            drawer.DrawTitle(text, margin ?? 0, hAlign, vAlign);
        }

        protected override void ExecutePolygon(IPdfDocumentDrawer state,
                IEnumerable<ParseTreeNode> pointNodes, bool isFilled)
        {
            var points = new List<XPoint>();
            foreach (var ptNode in pointNodes)
            {
                var (x, y) = ParsePointLocation(ptNode);
                points.Add(new XPoint(x, y));
            }
            state.DrawPolygon(points, isFilled);

        }

        protected override void ExecuteLineTo(IPdfDocumentDrawer state, ParseTreeNode node)
        {
            (double x, double y) = ParsePointLocation(node);
            state.LineTo(x, y);
        }

        protected override void ExecuteMoveTo(IPdfDocumentDrawer state, ParseTreeNode node)
        {
            (double x, double y) = ParsePointLocation(node);
            state.MoveTo(x, y);
        }

        protected override void ExecuteText(IPdfDocumentDrawer drawer,
            ParseTreeNode nodeLocation,
            ParseTreeNode? optMaxWidth,
            ParseTreeNode contentNode)
        {
            var text = Convert.ToString(EvaluateForObject(contentNode));

            if (!string.IsNullOrEmpty(text))
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);
                if (optMaxWidth != null)
                {
                    var maxWidth = EvaluateForDouble(optMaxWidth);
                    if (maxWidth is not null)
                    {
                        w = maxWidth;
                    }
                }

                drawer.DrawText(text, x, y, w, h);
            }
        }

        protected override void ExecuteLineText(IPdfDocumentDrawer state,
            ParseTreeNode nodeLocation,
            ParseTreeNode nodeAlignment,
            ParseTreeNode? nodeOrientation,
            ParseTreeNode contentNode)
        {
            var text = Convert.ToString(EvaluateForObject(contentNode));
            TextOrientation textOrientation = new TextOrientation { Orientation = TextOrientationEnum.Horizontal, Angle = null };

            if (nodeOrientation != null)
            {
                if (nodeOrientation.Term.Name == "number" || nodeOrientation.Token is null)
                {
                    textOrientation = textOrientation with { Angle = EvaluateForDouble(nodeOrientation) };
                }
                else if (Enum.TryParse<TextOrientationEnum>((string)nodeOrientation.Token.Value, true, out var specifiedOrientation))
                {
                    textOrientation = textOrientation with { Orientation = specifiedOrientation };
                }
            }
            if (!string.IsNullOrEmpty(text))
            {
                (double x, double y, double? w, double? h) = ParseTextLocation(nodeLocation.ChildNodes[0]);
                var (hAlign, vAlign) = ParseTextAlignment(nodeAlignment.ChildNodes[0], nodeAlignment.ChildNodes[1]);
                state.DrawLineText(text, x, y, w, h, hAlign, vAlign, textOrientation);
            }

        }

        protected override void ExecuteLine(IPdfDocumentDrawer drawer, ParseTreeNode nodeLocation)
        {
            (double x, double y, double? x1, double? y1) = ParseRectLocation(nodeLocation);
            drawer.DrawLine(x, y, x1 ?? x, y1 ?? y);
        }

        protected override void ExecuteForStatement(IPdfDocumentDrawer state,
            ParseTreeNode varNameNode,
            ParseTreeNode fromNode,
            ParseTreeNode toNode,
            ParseTreeNode forbody)
        {
            var varName = InternalSetVar(varNameNode, fromNode);
            var from = Convert.ToInt32(Variables[varName]);
            var to = Convert.ToInt32(EvaluateForObject(toNode));
            if (forbody != null)
            {
                for (int i = from; i <= to; i++)
                {
                    Variables.Add(varName, i);
                    Visit(state, forbody.ChildNodes);
                }
            }
        }

        protected override void ExecuteImage(IPdfDocumentDrawer drawer, ParseTreeNode locationNode,
            bool isEmbedded,
            ParseTreeNode imagePathNode,
            ParseTreeNode? unitNode,
            ParseTreeNode? cropNode)
        {
            string unit = "point";
            bool crop = false;
            var imagePath = ((string?)imagePathNode.Token?.Value) ?? string.Empty;
            var (x, y, w, h) = ParseTextLocation(locationNode);
            if (w is not null && unitNode != null)
            {
                //try to parse unit and cropping
                unit = unitNode.Term.Name;
                crop = cropNode?.ChildNodes.Count > 0;
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

        protected override void ExecuteUdfInvokeStatement(IPdfDocumentDrawer state, string fnName,
            ParseTreeNode args,
            ParseTreeNode defArgs,
            ParseTreeNode defBody)
        {
            var evaluatedArgs = args.ChildNodes.Select(EvaluateForObject).ToArray();

            if (defArgs != null)
            {
                var vars = Variables;
                if (vars is IVariablesDictionary savable) savable.SaveVariables();
                try
                {
                    for (int i = 0; i < defArgs.ChildNodes.Count; i++)
                    {
                        var defVar = defArgs.ChildNodes[i];
                        Variables.Add(defVar.Token.ValueString, evaluatedArgs[i] ?? null!);
                    }

                    Visit(state, defBody.ChildNodes);
                }
                finally
                {
                    //restore variables before call
                    if (vars is IVariablesDictionary restorable) restorable.RestoreVariables();
                }
            }
            else if (!UdfCustomCall(state, fnName, evaluatedArgs))
            {
                throw new PdfParserException($"UDF {fnName} is not found.");
            }
        }

        protected override void ExecuteBrush(IPdfDocumentDrawer state, ParseTreeNode colorNode)
        {

            var color = colorNode.ParseColor();

            state.CurrentBrush = new XSolidBrush(color);
        }

        protected override void ExecuteRowTemplateStatement(IPdfDocumentDrawer state,
            ParseTreeNode rowCountNode, ParseTreeNode offsetYNode, ParseTreeNode? borderSizeNode, 
            ParseTreeNode? newPageTopMarginNode,
            ParseTreeNode? nameNode,
            ParseTreeNode body)
        {
            string templateName = EvaluateForObject(nameNode)?.ToString()??string.Empty;
            var borderSize = borderSizeNode != null ? EvaluateForDouble(borderSizeNode) ?? 0 : 0;
            
            var rowCount = EvaluateForDouble(rowCountNode)??0;
            var offsetY = (EvaluateForDouble(offsetYNode)??0) + borderSize;
            var newPageTopMargin = newPageTopMarginNode!=null?(EvaluateForDouble(newPageTopMarginNode) ?? 0) : 0;

            //double pageOffsetY = 0;
            double drawHeight = borderSize;
            var vars = Variables;
            if (vars is IVariablesDictionary savable) savable.SaveVariables();
            try
            {
                state.BeginIterationTemplate((int)rowCount);
                for (int i = 0; i < rowCount; i++)
                {
                    state.BeginDrawRowTemplate(templateName, i, offsetY, newPageTopMargin);
                    //set row index
                    vars.Add("ROWINDEX", i);

                    Visit(state, body.ChildNodes);
                    var drawingRect = state.EndDrawRowTemplate(i);

                    if (drawingRect.PageOffsetY > 0)
                    {
                        //pageOffsetY = drawingRect.PageOffsetY;
                        //new page
                        var newHeight = drawingRect.DrawingRect.Bottom - drawingRect.PageOffsetY;
                        offsetY = newHeight + borderSize;
                        drawHeight = newHeight + borderSize;
                    }
                    else
                    {
                        drawHeight += drawingRect.DrawingRect.Height+ borderSize;
                        offsetY += drawingRect.DrawingRect.Height + borderSize;
                    }
                }
                state.EndIterationTemplate(drawHeight);

            }
            finally
            {
                //restore variables before call
                if (vars is IVariablesDictionary restorable) restorable.RestoreVariables();
            }
            //only available if no breaking page
            Variables.Add("LASTTEMPLATEHEIGHT", drawHeight);
        }

        protected override void ExecuteDebugOptions(IPdfDocumentDrawer state, IEnumerable<string> options)
        {
            foreach (var option in options.Select(MapToDebugOption))
            {
                state.DebugOptions |= option;
            }
        }

        private DebugOptions MapToDebugOption(string s) => s switch
        {
            "DEBUG_TEXT" => DebugOptions.DebugText,
            "DEBUG_RECT" => DebugOptions.DebugRect,
            "DEBUG_ROWTEMPLATE" => DebugOptions.DebugRowTemplate,
            "DEBUG_ALL" => DebugOptions.DebugAll,
            _ => DebugOptions.None
        };

        #endregion


        protected virtual bool UdfCustomCall(IPdfDocumentDrawer drawer, string udfName, object?[] arguments)
        {
            return false;
        }

        private object SystemVariableGet(IPdfDocumentDrawer state, string key)
        {
            return key switch
            {
                "PAGEHEIGHT" => state.PageHeight,
                "PAGEWIDTH" => state.PageWidth,
                _ => null!
            };
        }

        private double? ParseMargin(ParseTreeNode node)
        {
            return node.ChildNodes.Count > 0 ? EvaluateForDouble(node.ChildNodes[2]) : null;
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
                var color = headStyle.ChildNodes[0].ParseColor();
                result.HeaderBackColor = new XSolidBrush(color);
            }
            GenerateTableHead(node.ChildNodes("TableHeadCol"), result);
            var rowTemplate = node.ChildNodes("TableRowTemplate");
            if (rowTemplate != null && rowTemplate.Any())
            {
                GenerateTableRowsTemplate(rowTemplate, result);
            }
            else
            {
                GenerateTableRows(node.ChildNodes("TableRow"), result);
            }
            return result;
        }

        private void GenerateTableRowsTemplate(IEnumerable<ParseTreeNode> nodes, TableDefinition tbl)
        {
            foreach (var row in nodes)
            {
                var rowCount = EvaluateForDouble(row.ChildNodes[0]);
                var vars = Variables;
                if (vars is IVariablesDictionary savable) savable.SaveVariables();
                try
                {
                    for (var i = 0; i < rowCount; i++)
                    {
                        var rowDef = new RowDefinition();
                        var cols = row.ChildNodes("TableCol").SelectMany(x => x.ChildNodes).Where(x => x.Term?.Name != "COL").ToArray();

                        vars.Add("ROWINDEX", i);
                        var rowData = cols.Select(x => EvaluateForObject(x)?.ToString()!).ToList();

                        while (rowData.Count < tbl.Columns.Count)
                        {
                            rowData.Add(string.Empty);
                        }
                        rowDef.Data = rowData.ToArray();
                        tbl.Rows.Add(rowDef);
                    }
                }
                finally
                {
                    //restore variables before call
                    if (vars is IVariablesDictionary restorable) restorable.RestoreVariables();
                }
            }
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
                if (colWidthNode.ChildNodes.Count == 6)
                {
                    var desiredWidth = EvaluateForDouble(colWidthNode.ChildNodes[2]);
                    var maxWidth = EvaluateForDouble(colWidthNode.ChildNodes[5]);
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
                    colDef.Brush = new XSolidBrush(colors.ChildNodes[0].ParseColor());
                    colDef.BackColor = new XSolidBrush(colors.ChildNodes[1].ParseColor());
                }
                //name
                colDef.ColumnHeaderName = col.ChildNodes.Last().Token.ValueString;

                tbl.Columns.Add(colDef);
            }
        }

        private static (XStringAlignment, XLineAlignment) ParseTextAlignment(ParseTreeNode alignNode)
        {
            var hNode = alignNode.Term.Name == "HAlign" ? alignNode : null;
            var vNode = alignNode.Term.Name == "VAlign" ? alignNode : null;
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

            if (vNode == null || vNode.ChildNodes.Count <= 2) return (hAlign, vAlign);
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
            return EvaluateForDouble(node, Variables, CustomFunctions);
        }
        private static double? EvaluateForDouble(ParseTreeNode node, IDictionary<string, object?> variables,
            IDictionary<string, Func<object[], object>> funcs)
        {
            return new Evaluator(node, funcs).EvaluateForDouble(variables);
        }

        private object? EvaluateForObject(ParseTreeNode? node)
        {
            if (node is null) return null;
            return EvaluateForObject(node, Variables, CustomFunctions);
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

        private string InternalSetVar(ParseTreeNode varNameNode,
                    ParseTreeNode fromNode)
        {
            var v = EvaluateForObject(fromNode);
            var varName = varNameNode.Token.ValueString;
            Variables.Add(varName, v);
            return varName;
        }

        private string InternalSetVar(ParseTreeNode node)
        {
            return InternalSetVar(node.ChildNodes[1], node.ChildNodes[3]);
        }

        private XFont ExtractFont(ParseTreeNode node)
        {
            ParseTreeNode styleNode = null!;
            string fontName = string.Empty;
            double fontSize = 0;
            if (node.Term.Name == "FontSmt")
            {
                fontName = (string)EvaluateForObject(node.ChildNodes[3])!;
                fontSize = EvaluateForDouble(node.ChildNodes[6]) ?? 0;
                styleNode = node.ChildNodes.Count > 7 ? node.ChildNodes[7] : null!;
            }
            else
            {
                fontName = (string)node.ChildNodes[2].Token.Value;
                fontSize = EvaluateForDouble(node.ChildNodes[3]) ?? 0;
                styleNode = node.ChildNodes.Count > 4 ? node.ChildNodes[4] : null!;

            }
            var style = styleNode.ParseFontStyle();
            return new XFont(fontName, fontSize, style, XPdfFontOptions.UnicodeDefault);
        }


    }
}
