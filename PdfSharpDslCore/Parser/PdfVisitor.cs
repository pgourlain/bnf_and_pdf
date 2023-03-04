using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Irony.Parsing;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Extensions;

namespace PdfSharpDslCore.Parser
{

    public class PdfVisitor<TState>
    {
        protected IDictionary<string, object?> _variables = new Dictionary<string, object?>();
        protected IDictionary<string, ParseTreeNode> _udfs = new Dictionary<string, ParseTreeNode>();
        protected IDictionary<string, Func<object[], object>> _customFunctions = new Dictionary<string, Func<object[], object>>();
        protected string BaseDirectory { get; }

        public PdfVisitor() : this(Environment.CurrentDirectory) { }
        public PdfVisitor(string baseDirectory)
        {
            this.BaseDirectory = baseDirectory;
        }

        public virtual void Draw(TState state, ParseTree tree)
        {
            if (tree == null) return;
            if (state == null) throw new ArgumentNullException(nameof(state));

            //define each udf before visiting in order to accept call before definition
            tree.Root.ChildNodes.Where(x => x.Term?.Name == "UdfSmt").ToList().ForEach(ExecuteUdfStatement);
            Visit(state, tree.Root.ChildNodes);
        }

        /// <summary>
        /// register a custom function 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public void RegisterFormulaFunction(string name, Func<object[], object> func)
        {
            var fnName = name.ToUpperInvariant();
            if (_customFunctions.ContainsKey(fnName))
            {
                _customFunctions[fnName] = func;
            }
            else
            {
                _customFunctions.Add(fnName, func);
            }
        }

        protected void Visit(TState state, ParseTreeNodeList nodes)
        {
            foreach (var node in nodes)
            {
                Visit(state, node);
            }
        }

        protected void Visit(TState state, ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "PdfInstruction":
                    Visit(state, node.ChildNodes[0]);
                    break;
                case "SetSmt":
                    VisitSET(state, node.ChildNodes[1]);
                    break;
                case "RectSmt":
                    VisitRECT(state, node.ChildNodes[1], false);
                    break;
                case "FillRectSmt":
                    VisitRECT(state, node.ChildNodes[1], true);
                    break;
                case "LineTextSmt":
                    VisitLINETEXT(state, node);
                    break;
                case "NewPage":
                    VisitNEWPAGE(state, node);
                    break;
                case "IfSmt":
                    VisitIF(state, node);
                    break;
                case "PieSmt":
                    VisitPIE(state, node, false);
                    break;
                case "FillPieSmt":
                    VisitPIE(state, node, true);
                    break;
                case "ViewSizeSmt":
                    VisitVIEWSIZE(state, node.ChildNodes[1]);
                    break;
                case "EllipseSmt":
                    VisitELLIPSE(state, node, false);
                    break;
                case "FillEllipseSmt":
                    VisitELLIPSE(state, node, true);
                    break;
                case "TitleSmt":
                    VisitTITLE(state, node);
                    break;
                case "PolygonSmt":
                    VisitPOLYGON(state, node, false);
                    break;
                case "FillPolygonSmt":
                    VisitPOLYGON(state, node, true);
                    break;
                case "MoveToSmt":
                    VisitMOVETO(state, node);
                    break;
                case "LineToSmt":
                    VisitLINETO(state, node);
                    break;
                case "TextSmt":
                    VisitTEXT(state, node);
                    break;
                case "LineSmt":
                    VisitLINE(state, node);
                    break;
                case "ForSmt":
                    VisitFOR(state, node);
                    break;
                case "ImageSmt":
                    VisitIMAGE(state, node);
                    break;
                case "UdfSmt":
                    //nothing to do, it's already done before
                    break;
                case "UdfInvokeSmt":
                    VisitCALLUDF(state, node);
                    break;
                case "RowTemplateSmt":
                    VisitROWTEMPLATE(state, node);
                    break;
                default:
                    CustomVisit(state, node);
                    break;
            }
        }

        #region to be override 
        protected virtual void CustomVisit(TState state, ParseTreeNode node)
        {
            throw new NotImplementedException($"{node.Term.Name} is not yet implemented");
        }

        protected void NotImplemented(TState state, ParseTreeNode node)
        {
            throw new NotImplementedException();
        }

        protected virtual void ExecutePen(TState state, ParseTreeNode widthNode,
            ParseTreeNode colorNode,
            ParseTreeNode styleNode)
        {
        }

        protected virtual void ExecuteSetVar(TState state, ParseTreeNode node)
        { }

        protected virtual void ExecuteBrush(TState state, ParseTreeNode colorNode)
        { }

        protected virtual void ExecuteHBrush(TState state, ParseTreeNode colorNode)
        { }

        protected virtual void ExecuteFont(TState state, ParseTreeNode fontNode)
        { }
        protected virtual void ExecuteRect(TState state, ParseTreeNode rectNode, bool isFilled)
        { }

        protected virtual void ExecuteLineText(TState state,
            ParseTreeNode nodeLocation,
            ParseTreeNode nodeAlignment,
            ParseTreeNode? nodeOrientation,
            ParseTreeNode contentNode)
        { }

        protected virtual void ExecuteNewPage(TState state,
            ParseTreeNode? sizeNode,
            ParseTreeNode? orientationNode)
        { }

        protected virtual void ExecuteIfStatement(TState state, ParseTreeNode condNode,
            ParseTreeNode? ifNode, ParseTreeNode? elseNode)
        { }
        protected virtual void ExecutePie(TState state, ParseTreeNode locationNode,
            ParseTreeNode startAngleNode,
            ParseTreeNode sweepAngleNode,
            bool isFilled)
        { }
        protected virtual void ExecuteViewSize(TState drawer, ParseTreeNode sizeNode)
        { }

        protected virtual void ExecuteEllipse(TState state, ParseTreeNode rectNode, bool filled)
        { }

        protected virtual void ExecuteTitle(TState state, ParseTreeNode marginNode,
           ParseTreeNode alignmentsNode,
           ParseTreeNode contentNode)
        { }

        protected virtual void ExecutePolygon(TState state,
            IEnumerable<ParseTreeNode> pointNodes, bool isFilled)
        { }

        protected virtual void ExecuteLineTo(TState state, ParseTreeNode nodeLocation)
        { }

        protected virtual void ExecuteMoveTo(TState state, ParseTreeNode nodeLocation)
        { }
        protected virtual void ExecuteText(TState drawer,
           ParseTreeNode nodeLocation,
           ParseTreeNode? optMaxWidth,
           ParseTreeNode contentNode)
        { }

        protected virtual void ExecuteLine(TState drawer, ParseTreeNode nodeLocation)
        { }

        protected virtual void ExecuteForStatement(TState state,
            ParseTreeNode varNameNode,
            ParseTreeNode fromNode,
            ParseTreeNode toNode,
            ParseTreeNode forbody)
        { }

        protected virtual void ExecuteImage(TState state, ParseTreeNode locationNode,
            bool isEmbedded,
            ParseTreeNode imagePathNode,
            ParseTreeNode? unitNode,
            ParseTreeNode? cropNode)
        { }

        protected virtual void ExecuteUdfInvokeStatement(TState drawer,
            string fnName,
            ParseTreeNode args,
            ParseTreeNode defArgs,
            ParseTreeNode defBody)
        { }
        protected virtual void ExecuteRowTemplateStatement(TState drawer,
            ParseTreeNode rowCountNode,
            ParseTreeNode offsetYNode,
            ParseTreeNode? borderSizeNode,
            ParseTreeNode body)
        { }

        #endregion

        private void ExecuteUdfStatement(ParseTreeNode node)
        {
            var fnName = node.ChildNodes[0].Token.ValueString;
            if (_udfs.ContainsKey(fnName))
            {
                throw new PdfParserException($"An another UDF '{fnName}' is already defined.");
            }
            _udfs.Add(fnName, node);
        }

        #region private visit methods

        private void VisitROWTEMPLATE(TState state, ParseTreeNode node)
        {
            var rowCount = node.ChildNodes[2];
            var offset = node.ChildNodes[5];
            var borderSizeNode = node.ChildNode("Opt-BorderSize");
            if (borderSizeNode != null && borderSizeNode.ChildNodes.Count > 0)
            {
                borderSizeNode = borderSizeNode.ChildNodes[2];
            }
            else
            {
                borderSizeNode = null;
            }
            var body = node.ChildNode("RowTemplateBlock").ChildNode("EmbbededSmtList");
            ExecuteRowTemplateStatement(state, rowCount, offset, borderSizeNode, body);
        }

        private void VisitCALLUDF(TState state, ParseTreeNode node)
        {
            var fnName = node.ChildNodes[1].Token.ValueString;
            var arguments = node.ChildNode("CallInvokeArgumentslist");
            ParseTreeNode defArgs = null!;
            ParseTreeNode defBody = null!;

            if (_udfs.TryGetValue(fnName, out var defNode))
            {
                defArgs = defNode.ChildNode("UdfArgumentslist");
                defBody = defNode.ChildNode("UdfBlock").ChildNode("EmbbededSmtList");
                if (defArgs.ChildNodes.Count != arguments.ChildNodes.Count)
                {
                    throw new PdfParserException($"UDF '{fnName}' arguments count not match, provided ${arguments.ChildNodes.Count}, expected ${defArgs.ChildNodes.Count}.");
                }
            }
            ExecuteUdfInvokeStatement(state, fnName, arguments, defArgs, defBody);
        }

        private void VisitIMAGE(TState state, ParseTreeNode node)
        {
            var locationNode = node.ChildNode("ImageLocation");
            var isEmbedded = node.ChildNode("ImageRawOrSource").ChildNodes[0].Token.ValueString == "Data";
            var imagePathNode = node.ChildNodes[3];
            var unitNode = locationNode.ChildNodes.Count > 1 ? locationNode.ChildNodes[1] : null;
            var cropNode = locationNode.ChildNodes.Count > 1 ? locationNode.ChildNodes[2] : null;
            locationNode = locationNode.ChildNodes[0];
            ExecuteImage(state, locationNode, isEmbedded, imagePathNode, unitNode, cropNode);
        }


        private void VisitFOR(TState state, ParseTreeNode node)
        {
            var varNameNode = node.ChildNodes[1];
            var fromNode = node.ChildNodes[3];
            var toNode = node.ChildNodes[5];
            var forbody = node.ChildNode("ForBlock").ChildNode("EmbbededSmtList");

            ExecuteForStatement(state, varNameNode, fromNode, toNode, forbody);
        }

        private void VisitLINE(TState state, ParseTreeNode node)
        {
            ExecuteLine(state, node.ChildNodes[1]);
        }

        private void VisitTEXT(TState state, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNodes[1];
            var optMaxWidth = node.ChildNode("Opt-MaxWidth");
            if (optMaxWidth != null && optMaxWidth.ChildNodes.Count > 0)
            {
                optMaxWidth = optMaxWidth.ChildNodes[2];
            }
            else
            {
                optMaxWidth = null;
            }
            var contentNode = node.ChildNodes[5];
            ExecuteText(state, nodeLocation, optMaxWidth, contentNode);
        }

        private void VisitLINETO(TState state, ParseTreeNode node)
        {
            ExecuteLineTo(state, node.ChildNodes[1]);
        }

        private void VisitMOVETO(TState state, ParseTreeNode node)
        {
            ExecuteMoveTo(state, node.ChildNodes[1]);
        }

        private void VisitPOLYGON(TState state, ParseTreeNode node, bool isFilled)
        {
            List<ParseTreeNode> pointNodes = new List<ParseTreeNode>();

            pointNodes.Add(node.ChildNodes[1]);
            pointNodes.Add(node.ChildNodes[2]);
            var polygonPoint = node.ChildNode("PolygonPoint");
            pointNodes.AddRange(polygonPoint.ChildNodes);
            ExecutePolygon(state, pointNodes, isFilled);
        }

        private void VisitTITLE(TState state, ParseTreeNode node)
        {
            var marginNode = node.ChildNodes[1];
            var alignmentsNode = node.ChildNodes[2];
            var contentNode = node.ChildNodes[5];
            ExecuteTitle(state, marginNode, alignmentsNode, contentNode);
        }
        private void VisitELLIPSE(TState state, ParseTreeNode node, bool isFilled)
        {
            ExecuteEllipse(state, node, isFilled);
        }

        private void VisitVIEWSIZE(TState state, ParseTreeNode node)
        {
            ExecuteViewSize(state, node);
        }

        private void VisitPIE(TState state, ParseTreeNode node, bool isFilled)
        {
            var locationNode = node.ChildNodes[1];
            var startAngleNode = node.ChildNodes[4];
            var sweepAngleNode = node.ChildNodes[7];
            ExecutePie(state, locationNode, startAngleNode, sweepAngleNode, isFilled);
        }

        private void VisitIF(TState state, ParseTreeNode node)
        {
            var condNode = node.ChildNodes[0];
            var ifNode = node.ChildNode("then_clause");
            var elseNode = node.ChildNode("Else_clause_opt");
            if (ifNode != null)
            {
                ifNode = ifNode.ChildNode("EmbbededSmtList");
            }
            if (elseNode != null)
            {
                elseNode = elseNode.ChildNode("EmbbededSmtList");
            }
            ExecuteIfStatement(state, condNode, ifNode, elseNode);
        }

        private void VisitNEWPAGE(TState state, ParseTreeNode node)
        {
            var sizeNode = node.ChildNode("PageSize");
            var orientationNode = node.ChildNode("PageOrientation");
            sizeNode = sizeNode.ChildNodes.Count > 0 ? sizeNode.ChildNodes[0] : null;
            orientationNode = orientationNode.ChildNodes.Count > 0 ? orientationNode.ChildNodes[0] : null;
            ExecuteNewPage(state, sizeNode, orientationNode);
        }
        private void VisitLINETEXT(TState state, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNode("RectOrPointLocation");
            var nodeAlignment = node.ChildNode("TextAlignment");
            var nodeOrientation = node.ChildNode("TextOrientation");
            var contentNode = node.ChildNodes.Last();
            if (nodeOrientation != null && nodeOrientation.ChildNodes.Count > 2)
            {
                nodeOrientation = nodeOrientation.ChildNodes[2];
            }
            else
            {
                nodeOrientation = null;
            }
            ExecuteLineText(state, nodeLocation, nodeAlignment, nodeOrientation, contentNode);
        }

        private void VisitRECT(TState state, ParseTreeNode node, bool isFilled)
        {
            ExecuteRect(state, node, isFilled);
        }

        private void VisitSET(TState state, ParseTreeNode node)
        {
            var executor = (Action<TState, ParseTreeNode>)(node.Term.Name switch {
                "PenSmt" => VisitSETPen,
                "BrushSmt" => VisitSETBrush,
                "HBrushSmt" => VisitSETHBrush,
                "FontSmt" => VisitSETFont,
                "VarSmt" => VisitSETVar,
                _ => NotImplemented
            });

            executor(state, node);
        }

        private void VisitSETPen(TState state, ParseTreeNode node)
        {
            var width = node.ChildNodes[2];
            var color = node.ChildNodes[1];
            var n = node.ChildNode("StylePen");
            if (n != null && n.ChildNodes.Count > 0)
            {
                n = n.ChildNodes[0];
            }
            ExecutePen(state, width, color, n!);
        }

        private void VisitSETBrush(TState state, ParseTreeNode node)
        {
            ExecuteBrush(state, node.ChildNodes[1]);
        }

        private void VisitSETHBrush(TState state, ParseTreeNode node)
        {
            ExecuteHBrush(state, node.ChildNodes[1]);
        }
        private void VisitSETFont(TState state, ParseTreeNode node)
        {
            ExecuteFont(state, node);
        }
        private void VisitSETVar(TState state, ParseTreeNode node)
        {
            ExecuteSetVar(state, node);
        }
        #endregion
    }
}