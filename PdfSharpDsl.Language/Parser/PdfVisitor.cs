using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Irony.Parsing;
using Microsoft.Extensions.Logging;
using PdfSharpDslCore.Extensions;

namespace PdfSharpDslCore.Parser
{

    public class PdfVisitor<TState>
    {
        protected readonly ILogger? Logger;
        protected IDictionary<string, object?> Variables { get; set; } = new Dictionary<string, object?>();

        protected IDictionary<string, Func<object[], object>> CustomFunctions { get; set; } = new Dictionary<string, Func<object[], object>>();

        protected IDictionary<string, ParseTreeNode> UserDefinedFunctions { get; set; } = new Dictionary<string, ParseTreeNode>();

        /// <summary>Statements (and UDF/MASTER bodies) that come from an INCLUDEd file, with that file's name.</summary>
        private readonly Dictionary<ParseTreeNode, string> _nodeFiles = new Dictionary<ParseTreeNode, string>();
        private readonly Dictionary<ParseTreeNode, string> _nodeDirectories = new Dictionary<ParseTreeNode, string>();
        private string? _currentDirectory;

        /// <summary>
        /// Folder relative paths (IMAGE Source=...) are resolved from: the folder of the INCLUDEd file being executed,
        /// or <see cref="BaseDirectory"/> for the main file.
        /// </summary>
        protected string CurrentDirectory => _currentDirectory ?? BaseDirectory;

        protected IDictionary<string, ParseTreeNode> Styles { get; set; } = new Dictionary<string, ParseTreeNode>();

        protected IDictionary<string, ParseTreeNode> Masters { get; set; } = new Dictionary<string, ParseTreeNode>();

        protected string BaseDirectory { get; }

        public PdfVisitor(ILogger? logger) : this(Environment.CurrentDirectory, logger) { }

        protected PdfVisitor(string baseDirectory, ILogger? logger)
        {
            Logger = logger;
            this.BaseDirectory = baseDirectory;
        }

        public virtual void Draw(TState state, ParseTree tree)
        {
            if (tree == null) return;
            if (state == null) throw new ArgumentNullException(nameof(state));

            //INCLUDE "file"; is replaced by the statements of that file, before anything else
            var rootNodes = ExpandIncludes(tree.Root.ChildNodes, BaseDirectory, null, new List<string>(), new HashSet<string>());
            //define each udf before visiting in order to accept call before definition
            rootNodes.Where(x => x.Term?.Name == "UdfSmt").ToList().ForEach(ExecuteUdfStatement);
            //and for styles, so USE works before the STYLE definition too
            rootNodes.Where(x => x.Term?.Name == "StyleSmt").ToList().ForEach(ExecuteStyleStatement);
            //same for masters, so NEWPAGE Master=name works regardless of source order
            rootNodes.Where(x => x.Term?.Name == "MasterSmt").ToList().ForEach(ExecuteMasterStatement);
            //check for global debug options, page scoped ones are executed in order while visiting
            var debugOptions = rootNodes.Where(x => x.Term?.Name == "DebugOptionsSmt")
                .Where(x => !IsPageScoped(x))
                .SelectMany(x => ParseDebugOptions(x.ChildNodes("debugOption")));
            ExecuteDebugOptions(state, debugOptions);
            foreach (var node in rootNodes)
            {
                using (UseFileOf(node))
                {
                    Visit(state, node);
                }
            }
        }
        
        /// <summary>
        /// register a custom function 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public void RegisterFormulaFunction(string name, Func<object[], object> func)
        {
            var fnName = name.ToUpperInvariant();
            if (CustomFunctions.ContainsKey(fnName))
            {
                CustomFunctions[fnName] = func;
            }
            else
            {
                CustomFunctions.Add(fnName, func);
            }
        }

        protected void Visit(TState state, ParseTreeNodeList nodes)
        {
            foreach (var node in nodes)
            {
                //a RETURN ends the statements of a UDF body, wherever it is nested
                if (StopVisiting) return;
                Visit(state, node);
            }
        }

        /// <summary>True when the statements being visited must not go on (a UDF executed RETURN).</summary>
        protected virtual bool StopVisiting => false;

        protected void Visit(TState state, ParseTreeNode node)
        {
            switch (node.Term.Name)
            {
                case "PdfInstruction":
                    Visit(state, node.ChildNodes[0]);
                    break;
                case "StyleInstruction":
                    Visit(state, node.ChildNodes[0]);
                    break;
                case "SetSmt":
                    VisitSet(state, node.ChildNodes[1]);
                    break;
                case "RectSmt":
                    VisitRect(state, node.ChildNodes[1], false);
                    break;
                case "FillRectSmt":
                    VisitRect(state, node.ChildNodes[1], true);
                    break;
                case "LineTextSmt":
                    VisitLinetext(state, node);
                    break;
                case "NewPage":
                    VisitNewpage(state, node);
                    break;
                case "IfSmt":
                    VisitIf(state, node);
                    break;
                case "PieSmt":
                    VisitPie(state, node, false);
                    break;
                case "FillPieSmt":
                    VisitPie(state, node, true);
                    break;
                case "ViewSizeSmt":
                    VisitViewsize(state, node.ChildNodes[1]);
                    break;
                case "EllipseSmt":
                    VisitEllipse(state, node, false);
                    break;
                case "FillEllipseSmt":
                    VisitEllipse(state, node, true);
                    break;
                case "TitleSmt":
                    VisitTitle(state, node);
                    break;
                case "PolygonSmt":
                    VisitPolygon(state, node, false);
                    break;
                case "FillPolygonSmt":
                    VisitPolygon(state, node, true);
                    break;
                case "MoveToSmt":
                    VisitMoveto(state, node);
                    break;
                case "LineToSmt":
                    VisitLineto(state, node);
                    break;
                case "TextSmt":
                    VisitText(state, node);
                    break;
                case "LineSmt":
                    VisitLine(state, node);
                    break;
                case "ForSmt":
                    VisitFor(state, node);
                    break;
                case "WhileSmt":
                    VisitWhile(state, node);
                    break;
                case "ForEachSmt":
                    VisitForEach(state, node);
                    break;
                case "ChartSmt":
                    ExecuteChart(state, node.ChildNode("ChartType")!.ChildNodes[0].Token.ValueString, node.ChildNode("RectLocation")!,
                        node.ChildNodes[5], GetOptArg(node, "Opt-Labels"), GetOptArg(node, "Opt-Colors"));
                    break;
                case "ReturnSmt":
                    ExecuteReturn(state, node.ChildNodes[1]);
                    break;
                case "BarcodeSmt":
                    ExecuteBarcode(state, node.ChildNodes[1], node.ChildNode("BarcodeType")!.ChildNodes[0].Token.ValueString, node.ChildNodes.Last());
                    break;
                case "ImageSmt":
                    VisitImage(state, node);
                    break;
                case "DebugOptionsSmt":
                    if (IsPageScoped(node))
                    {
                        ExecutePageDebugOptions(state, ParseDebugOptions(node.ChildNodes("debugOption")));
                    }
                    //global options are already done before
                    break;
                case "UdfSmt":
                    //nothing to do, it's already done before
                    break;
                case "MasterSmt":
                    //nothing to do, it's already done before
                    break;
                case "StyleSmt":
                    //nothing to do, it's already done before
                    break;
                case "IncludeSmt":
                    //nothing to do, includes are expanded before visiting
                    break;
                case "UseSmt":
                    VisitUse(state, node);
                    break;
                case "UdfInvokeSmt":
                    VisitCalludf(state, node);
                    break;
                case "RowTemplateSmt":
                    VisitRowtemplate(state, node);
                    break;
                case "FlowSmt":
                    VisitFlow(state, node);
                    break;
                case "ParagraphSmt":
                    ExecuteParagraph(state, node.ChildNode("HAlign")!, node.ChildNodes.Last());
                    break;
                case "SpaceSmt":
                    ExecuteSpace(state, node.ChildNodes.Last());
                    break;
                default:
                    CustomVisit(state, node);
                    break;
            }
        }

        #region to be override

        protected virtual void ExecuteDebugOptions(TState state, IEnumerable<string> options)
        { }

        /// <summary>
        /// debug options only available until the next page
        /// </summary>
        protected virtual void ExecutePageDebugOptions(TState state, IEnumerable<string> options)
        { }
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
        { }

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
            bool shrinkToFit,
            bool ellipsisOverflow,
            ParseTreeNode contentNode)
        { }

        protected virtual void ExecuteNewPage(TState state,
            ParseTreeNode? sizeNode,
            ParseTreeNode? orientationNode,
            ParseTreeNode? masterNameNode)
        { }

        /// <param name="elseIfList">Node whose children are the ELSE IF clauses, each "condition, body"; may be null.</param>
        protected virtual void ExecuteIfStatement(TState state, ParseTreeNode condNode,
            ParseTreeNode? ifNode, ParseTreeNode? elseIfList, ParseTreeNode? elseNode)
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
            ParseTreeNode? stepNode,
            ParseTreeNode forbody)
        { }

        /// <param name="type">Lower case type name as written after <c>Type=</c>, e.g. "code128".</param>
        protected virtual void ExecuteBarcode(TState state, ParseTreeNode locationNode, string type, ParseTreeNode contentNode)
        { }

        /// <param name="type">"bar", "line" or "pie".</param>
        protected virtual void ExecuteChart(TState state, string type, ParseTreeNode locationNode, ParseTreeNode dataNode,
            ParseTreeNode? labelsNode, ParseTreeNode? colorsNode)
        { }

        protected virtual void ExecuteReturn(TState state, ParseTreeNode valueNode)
        { }

        protected virtual void ExecuteForEachStatement(TState state, ParseTreeNode varNameNode, ParseTreeNode listNode, ParseTreeNode? body)
        { }

        protected virtual void ExecuteWhileStatement(TState state, ParseTreeNode condNode, ParseTreeNode? body)
        { }

        protected virtual void ExecuteImage(TState state, ParseTreeNode locationNode,
            bool isEmbedded,
            ParseTreeNode imagePathNode,
            ParseTreeNode? unitNode,
            ParseTreeNode? cropNode)
        { }

        protected virtual void ExecuteUdfInvokeStatement(TState drawer,
            string fnName,
            ParseTreeNode? args,
            ParseTreeNode defArgs,
            ParseTreeNode? defBody)
        { }
        protected virtual void ExecuteRowTemplateStatement(TState drawer,
            ParseTreeNode rowCountNode,
            ParseTreeNode offsetYNode,
            ParseTreeNode? borderSizeNode,
            ParseTreeNode? newPageTopMarginNode,
            ParseTreeNode? nameNode,
            ParseTreeNode body)
        { }


        protected virtual void ExecuteFlow(TState drawer,
            ParseTreeNode? marginNode,
            ParseTreeNode? topNode,
            ParseTreeNode body)
        { }

        protected virtual void ExecuteParagraph(TState drawer, ParseTreeNode alignmentNode, ParseTreeNode contentNode)
        { }

        protected virtual void ExecuteSpace(TState drawer, ParseTreeNode heightNode)
        { }

        #endregion

        private void ExecuteUdfStatement(ParseTreeNode node)
        {
            var fnName = node.ChildNodes[0].Token.ValueString;
            if (UserDefinedFunctions.ContainsKey(fnName))
            {
                throw new PdfParserException($"An another UDF '{fnName}' is already defined.");
            }
            UserDefinedFunctions.Add(fnName, node);
        }

        /// <summary>
        /// Makes run-time error messages name the INCLUDEd file <paramref name="node"/> comes from (nothing for the main
        /// file). Wrap the execution of a UDF or MASTER body in it, since it may live in another file than its caller.
        /// </summary>
        protected IDisposable UseFileOf(ParseTreeNode node)
        {
            var previousDirectory = _currentDirectory;
            _currentDirectory = _nodeDirectories.TryGetValue(node, out var directory) ? directory : null;
            var fileScope = PdfDslDiagnostics.UseFile(_nodeFiles.TryGetValue(node, out var file) ? file : null);
            return new ActionDisposable(() =>
            {
                fileScope.Dispose();
                _currentDirectory = previousDirectory;
            });
        }

        private sealed class ActionDisposable : IDisposable
        {
            private readonly Action _action;
            public ActionDisposable(Action action) { _action = action; }
            public void Dispose() => _action();
        }

        /// <summary>Includes nested deeper than this are refused, in addition to the circular check.</summary>
        private const int MaxIncludeDepth = 16;

        /// <summary>
        /// Replaces every top-level INCLUDE node by the statements of the file it names, recursively. Paths are
        /// relative to the including file. The included file is parsed on its own, so its errors carry its own
        /// line numbers and file name. A file is only included once per document: later INCLUDEs of it are skipped, so
        /// every file can include the shared files it needs without defining their UDFs twice.
        /// </summary>
        /// <param name="fileName">Name of the file <paramref name="nodes"/> come from, null for the main file.</param>
        private List<ParseTreeNode> ExpandIncludes(IEnumerable<ParseTreeNode> nodes, string directory, string? fileName,
            List<string> chain, HashSet<string> included)
        {
            var result = new List<ParseTreeNode>();
            foreach (var node in nodes)
            {
                if (node.Term?.Name != "IncludeSmt")
                {
                    result.Add(node);
                    if (fileName != null) RegisterFile(node, fileName, directory);
                    continue;
                }

                var pathNode = node.ChildNodes[1];
                var relativePath = Convert.ToString(pathNode.Token.Value) ?? string.Empty;
                var where = PdfDslDiagnostics.AtLocation(pathNode.Span.Location);
                var fullPath = Path.GetFullPath(Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(directory, relativePath));

                if (chain.Contains(fullPath))
                {
                    var cycle = string.Join(" -> ", chain.SkipWhile(x => x != fullPath).Concat(new[] { fullPath }).Select(Path.GetFileName));
                    throw new PdfParserException($"Circular INCLUDE{where}: {cycle}.");
                }

                if (!included.Add(fullPath))
                {
                    continue;
                }

                if (chain.Count >= MaxIncludeDepth)
                {
                    throw new PdfParserException($"INCLUDE nested more than {MaxIncludeDepth} levels deep{where}.");
                }

                if (!File.Exists(fullPath))
                {
                    throw new PdfParserException($"INCLUDE file '{relativePath}' not found{where}.");
                }

                var includedTree = new Irony.Parsing.Parser(new PdfGrammar()).Parse(File.ReadAllText(fullPath), fullPath);
                if (includedTree.HasErrors())
                {
                    var errors = PdfDslDiagnostics.FormatParseErrors(includedTree).Select(e => $"{Path.GetFileName(fullPath)}: {e}");
                    throw new PdfParserException($"INCLUDE file '{relativePath}'{where} has errors:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
                }

                chain.Add(fullPath);
                try
                {
                    result.AddRange(ExpandIncludes(includedTree.Root.ChildNodes, Path.GetDirectoryName(fullPath) ?? directory,
                        Path.GetFileName(fullPath), chain, included));
                }
                finally
                {
                    chain.RemoveAt(chain.Count - 1);
                }
            }

            return result;
        }

        private void RegisterFile(ParseTreeNode node, string fileName, string directory)
        {
            _nodeFiles[node] = fileName;
            _nodeDirectories[node] = directory;
            //the body of a UDF or MASTER runs when it is called, from any file
            foreach (var body in new[]
            {
                node.ChildNode("UdfBlock")?.ChildNode("EmbbededSmtList"),
                node.ChildNode("MasterBlock")?.ChildNode("EmbbededSmtList"),
            })
            {
                if (body == null) continue;
                _nodeFiles[body] = fileName;
                _nodeDirectories[body] = directory;
            }
        }

        private void ExecuteStyleStatement(ParseTreeNode node)
        {
            var styleName = node.ChildNodes[0].Token.ValueString;
            if (Styles.ContainsKey(styleName))
            {
                throw new PdfParserException($"An another STYLE '{styleName}' is already defined.");
            }
            Styles.Add(styleName, node);
        }

        private void VisitUse(TState state, ParseTreeNode node)
        {
            var nameNode = node.ChildNodes[1];
            var styleName = nameNode.Token.ValueString;
            if (!Styles.TryGetValue(styleName, out var styleNode))
            {
                throw new PdfParserException($"Unknown style '{styleName}'{PdfDslDiagnostics.AtLocation(nameNode.Span.Location)}."
                    + SuggestionFor(styleName, Styles.Keys));
            }
            //pen, brush and font live in the drawer, so replaying the SET statements is all a style is
            Visit(state, styleNode.ChildNode("StyleBody")!.ChildNodes);
        }

        private static string SuggestionFor(string name, IEnumerable<string> candidates)
        {
            var suggestion = PdfDslDiagnostics.Suggest(name, candidates);
            return suggestion == null ? string.Empty : $" Did you mean '{suggestion}'?";
        }

        private void ExecuteMasterStatement(ParseTreeNode node)
        {
            var masterName = node.ChildNodes[0].Token.ValueString;
            if (Masters.ContainsKey(masterName))
            {
                throw new PdfParserException($"An another MASTER '{masterName}' is already defined.");
            }
            Masters.Add(masterName, node);
        }

        #region private visit methods

        private static bool IsPageScoped(ParseTreeNode debugOptionsNode)
        {
            var scopeNode = debugOptionsNode.ChildNode("DebugScope");
            return scopeNode?.ChildNodes.Count > 0 && scopeNode.ChildNodes[0].Token?.Text == "PAGE";
        }

        private IEnumerable<string> ParseDebugOptions(IEnumerable<ParseTreeNode> nodes)
        {
            foreach (var optionNode in nodes)
            {
                var optionName = optionNode.Token.Text;
                if (optionName.StartsWith("DEBUG_"))
                {
                    yield return optionName;
                }
            }
        }

        private void VisitRowtemplate(TState state, ParseTreeNode node)
        {
            var rowCount = node.ChildNodes[2];
            var offset = node.ChildNodes[5];
            var borderSizeNode = GetOptArg(node, "Opt-BorderSize");
            var topMarginNode = GetOptArg(node, "Opt-NewPageTopMargin");
            var nameNode = GetOptArg(node, "Opt-Name");

            var body = node.ChildNode("RowTemplateBlock")?.ChildNode("EmbbededSmtList")!;
            ExecuteRowTemplateStatement(state, rowCount, offset, borderSizeNode, topMarginNode, nameNode, body);
        }

        private void VisitFlow(TState state, ParseTreeNode node)
        {
            var marginNode = GetOptArg(node, "Opt-Margin");
            var topNode = GetOptArg(node, "Opt-Top");
            var body = node.ChildNode("FlowBlock")?.ChildNode("EmbbededSmtList")!;
            ExecuteFlow(state, marginNode, topNode, body);
        }

        private void VisitCalludf(TState state, ParseTreeNode node)
        {
            var fnName = node.ChildNodes[1].Token.ValueString;
            var arguments = node.ChildNode("CallInvokeArgumentslist")!;
            ExecuteUdfByName(state, fnName, arguments);
        }

        protected void ExecuteUdfByName(TState state, string fnName, ParseTreeNode? arguments)
        {
            ParseTreeNode? defArgs = null!;
            ParseTreeNode? defBody = null!;

            if (UserDefinedFunctions.TryGetValue(fnName, out var defNode))
            {
                defArgs = defNode.ChildNode("UdfArgumentslist")!;
                defBody = defNode.ChildNode("UdfBlock")?.ChildNode("EmbbededSmtList")!;
                var expectedArgumentCount = defArgs?.ChildNodes.Count ?? 0;
                var providedArgumentCount = arguments?.ChildNodes.Count ?? 0;
                if (expectedArgumentCount != providedArgumentCount)
                {
                    throw new PdfParserException($"UDF '{fnName}' arguments count does not match, provided {providedArgumentCount}, expected {expectedArgumentCount}.");
                }

                if (defArgs == null)
                {
                    //to avoid calling udfCustom
                    defArgs = new ParseTreeNode(new NonTerminal("noArg"), new SourceSpan(new SourceLocation(0,0,0),1 ));
                }
            }
            ExecuteUdfInvokeStatement(state, fnName, arguments, defArgs, defBody);
        }

        private void VisitImage(TState state, ParseTreeNode node)
        {
            var locationNode = node.ChildNode("ImageLocation")!;
            var isEmbedded = node.ChildNode("ImageRawOrSource")?.ChildNodes[0].Token.ValueString == "Data";
            var imagePathNode = node.ChildNodes[3];
            var unitNode = locationNode.ChildNodes.Count > 1 ? locationNode.ChildNodes[1] : null;
            var cropNode = locationNode.ChildNodes.Count > 1 ? locationNode.ChildNodes[2] : null;
            locationNode = locationNode.ChildNodes[0];
            ExecuteImage(state, locationNode, isEmbedded, imagePathNode, unitNode, cropNode);
        }


        private void VisitFor(TState state, ParseTreeNode node)
        {
            var varNameNode = node.ChildNodes[1];
            var fromNode = node.ChildNodes[3];
            var toNode = node.ChildNodes[5];
            var forbody = node.ChildNode("ForBlock")?.ChildNode("EmbbededSmtList")!;

            var stepNode = node.ChildNode("ForStep");
            stepNode = stepNode?.ChildNodes.Count > 0 ? stepNode.ChildNodes[1] : null;

            ExecuteForStatement(state, varNameNode, fromNode, toNode, stepNode, forbody);
        }

        private void VisitForEach(TState state, ParseTreeNode node)
        {
            var varNameNode = node.ChildNodes[1];
            var listNode = node.ChildNodes[3];
            var body = node.ChildNode("ForEachBlock")?.ChildNode("EmbbededSmtList");
            ExecuteForEachStatement(state, varNameNode, listNode, body);
        }

        private void VisitWhile(TState state, ParseTreeNode node)
        {
            var condNode = node.ChildNodes[1];
            var body = node.ChildNode("WhileBlock")?.ChildNode("EmbbededSmtList");
            ExecuteWhileStatement(state, condNode, body);
        }

        private void VisitLine(TState state, ParseTreeNode node)
        {
            ExecuteLine(state, node.ChildNodes[1]);
        }

        private void VisitText(TState state, ParseTreeNode node)
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

        private void VisitLineto(TState state, ParseTreeNode node)
        {
            ExecuteLineTo(state, node.ChildNodes[1]);
        }

        private void VisitMoveto(TState state, ParseTreeNode node)
        {
            ExecuteMoveTo(state, node.ChildNodes[1]);
        }

        private void VisitPolygon(TState state, ParseTreeNode node, bool isFilled)
        {
            List<ParseTreeNode> pointNodes = new List<ParseTreeNode>();

            pointNodes.Add(node.ChildNodes[1]);
            pointNodes.Add(node.ChildNodes[2]);
            var polygonPoint = node.ChildNode("PolygonPoint")!;
            pointNodes.AddRange(polygonPoint.ChildNodes);
            ExecutePolygon(state, pointNodes, isFilled);
        }

        private void VisitTitle(TState state, ParseTreeNode node)
        {
            var marginNode = node.ChildNodes[1];
            var alignmentsNode = node.ChildNodes[2];
            var contentNode = node.ChildNodes[5];
            ExecuteTitle(state, marginNode, alignmentsNode, contentNode);
        }
        private void VisitEllipse(TState state, ParseTreeNode node, bool isFilled)
        {
            ExecuteEllipse(state, node, isFilled);
        }

        private void VisitViewsize(TState state, ParseTreeNode node)
        {
            ExecuteViewSize(state, node);
        }

        private void VisitPie(TState state, ParseTreeNode node, bool isFilled)
        {
            var locationNode = node.ChildNodes[1];
            var startAngleNode = node.ChildNodes[4];
            var sweepAngleNode = node.ChildNodes[7];
            ExecutePie(state, locationNode, startAngleNode, sweepAngleNode, isFilled);
        }

        private void VisitIf(TState state, ParseTreeNode node)
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
            var elseIfList = node.ChildNode("ElseIfList");
            ExecuteIfStatement(state, condNode, ifNode, elseIfList, elseNode);
        }

        private void VisitNewpage(TState state, ParseTreeNode node)
        {
            var sizeNode = node.ChildNode("PageSize");
            var orientationNode = node.ChildNode("PageOrientation")!;
            sizeNode = sizeNode?.ChildNodes.Count > 0 ? sizeNode.ChildNodes[0] : null;
            orientationNode = orientationNode.ChildNodes.Count > 0 ? orientationNode.ChildNodes[0] : null;
            var masterNameNode = GetOptArg(node, "Opt-Master");
            ExecuteNewPage(state, sizeNode, orientationNode, masterNameNode);
        }
        private void VisitLinetext(TState state, ParseTreeNode node)
        {
            var nodeLocation = node.ChildNode("RectOrPointLocation")!;
            var nodeAlignment = node.ChildNode("TextAlignment")!;
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
            var shrinkToFit = node.ChildNode("Opt-Fit")?.ChildNodes.Count > 0;
            var ellipsisOverflow = node.ChildNode("Opt-Overflow")?.ChildNodes.Count > 0;
            ExecuteLineText(state, nodeLocation, nodeAlignment, nodeOrientation, shrinkToFit, ellipsisOverflow, contentNode);
        }

        private void VisitRect(TState state, ParseTreeNode node, bool isFilled)
        {
            ExecuteRect(state, node, isFilled);
        }

        private void VisitSet(TState state, ParseTreeNode node)
        {
            var executor = (Action<TState, ParseTreeNode>)(node.Term.Name switch {
                "PenSmt" => VisitSetPen,
                "BrushSmt" => VisitSetBrush,
                "HBrushSmt" => VisitSethBrush,
                "FontSmt" => VisitSetFont,
                "VarSmt" => VisitSetVar,
                _ => NotImplemented
            });

            executor(state, node);
        }

        private void VisitSetPen(TState state, ParseTreeNode node)
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

        private void VisitSetBrush(TState state, ParseTreeNode node)
        {
            ExecuteBrush(state, node.ChildNodes[1]);
        }

        private void VisitSethBrush(TState state, ParseTreeNode node)
        {
            ExecuteHBrush(state, node.ChildNodes[1]);
        }
        private void VisitSetFont(TState state, ParseTreeNode node)
        {
            ExecuteFont(state, node);
        }
        private void VisitSetVar(TState state, ParseTreeNode node)
        {
            ExecuteSetVar(state, node);
        }

        private ParseTreeNode? GetOptArg(ParseTreeNode node, string optArgName)
        {
            var resultNode = node.ChildNode(optArgName);
            if (resultNode != null && resultNode.ChildNodes.Count > 0)
            {
                resultNode = resultNode.ChildNodes[2];
            }
            else
            {
                resultNode = null;
            }

            return resultNode;
        }
        #endregion
    }
}