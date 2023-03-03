
using Irony;
using Irony.Parsing;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.BarCodes;
using PdfSharpDslCore.Drawing;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("pdfsharpdslTests")]


namespace PdfSharpDslCore.Parser
{
    [Language("PdfGrammar", "1.0", "Grammar to write PDF with PdfSharp")]
    public class PdfGrammar : Grammar
    {
        protected NonTerminal FormulaRoot;
        public PdfGrammar()
        {
            var sstring = new StringLiteral("string", "\"", StringOptions.AllowsDoubledQuote | StringOptions.AllowsAllEscapes);
            sstring.Priority = TerminalPriority.High;
            var textString = new StringLiteral("textstring", "\"", StringOptions.AllowsDoubledQuote | StringOptions.AllowsAllEscapes | StringOptions.AllowsLineBreak);
            var number_literal = new NumberLiteral("number", NumberOptions.AllowSign);
            var pixel_literal = new NumberLiteral("pixel", NumberOptions.IntOnly);
            pixel_literal.DefaultFloatType = TypeCode.Double;
            pixel_literal.AddSuffix("px", TypeCode.Int64);
            pixel_literal.DefaultIntTypes = new TypeCode[3]
            {
                TypeCode.Int32,
                TypeCode.Int64,
                (TypeCode)30
            };
            var colorNumber = new NumberLiteral("ColorValue");
            colorNumber.AddPrefix("g", NumberOptions.Default | NumberOptions.AllowStartEndDot);
            colorNumber.AddPrefix("0x", NumberOptions.Hex);
            var variable_literal = new IdentifierTerminal("var");
            KeyTerm lpar = ToTerm("(");
            KeyTerm rpar = ToTerm(")");
            KeyTerm comma = ToTerm(",", "comma");

            var comment = new CommentTerminal("comment", "#", "\r", "\n", "\u2085", "\u2028", "\u2029");
            //comment must to be added to NonGrammarTerminals list; it is not used directly in grammar rules,
            // so we add it to this list to let Scanner know that it is also a valid terminal. 
            NonGrammarTerminals.Add(comment);

            #region variables
            var PDF = new NonTerminal("PDF");
            var PdfLine = new NonTerminal("PdfLine");
            var PdfInstruction = new NonTerminal("PdfInstruction");
            var PdfPrimaryInstruction = new NonTerminal("PdfPrimaryInstruction");
            var SetSmt = new NonTerminal("SetSmt");
            var RectSmt = new NonTerminal("RectSmt");
            var LineSmt = new NonTerminal("LineSmt");
            var EllipseSmt = new NonTerminal("EllipseSmt");
            var LineToSmt = new NonTerminal("LineToSmt");
            var MoveToSmt = new NonTerminal("MoveToSmt");
            var FillRectSmt = new NonTerminal("FillRectSmt");
            var FillEllipseSmt = new NonTerminal("FillEllipseSmt");
            var PenSmt = new NonTerminal("PenSmt");
            var BrushSmt = new NonTerminal("BrushSmt");
            var HBrushSmt = new NonTerminal("HBrushSmt");
            var BrushType = new NonTerminal("BrushType");
            var FontSmt = new NonTerminal("FontSmt");
            var ImageSmt = new NonTerminal("ImageSmt");

            var ColorExp = new NonTerminal("ColorExp");
            var NamedColor = new NonTerminal("NamedColor");
            var HexColor = new NonTerminal("HexColor");
            var styleExpr = new NonTerminal("styleExpr");
            var RectLocation = new NonTerminal("RectLocation");
            var PointLocation = new NonTerminal("PointLocation");
            var SetContent = new NonTerminal("SetContent");
            var TextSmt = new NonTerminal("TextSmt");
            var RectOrPointLocation = new NonTerminal("RectOrPointLocation");
            var TextAlignment = new NonTerminal("TextAlignment");
            var TextOrientation = new NonTerminal("TextOrientation");
            var TextOrientationValue = new NonTerminal("TextOrientationValue");
            var HAlign = new NonTerminal("HAlign");
            var VAlign = new NonTerminal("VAlign");
            var VAlignValue = new NonTerminal("VAlignValue");
            var HAlignValue = new NonTerminal("HAlignValue");
            var LineTextSmt = new NonTerminal("LineTextSmt");
            var NewPageSmt = new NonTerminal("NewPage");
            var PageSize = new NonTerminal("PageSize");
            var PageOrientation = new NonTerminal("PageOrientation");
            var TableSmt = new NonTerminal("TableSmt");
            var TableContent = new NonTerminal("TableContent");
            var TableHead = new NonTerminal("TableHead");
            var TableRowList = new NonTerminal("TableRowList");
            var TableColList = new NonTerminal("TableColList");
            var TableRow = new NonTerminal("TableRow");
            var TableCol = new NonTerminal("TableCol");
            var TableLocation = new NonTerminal("TableLocation");
            var TableHeadStyle = new NonTerminal("TableHeadStyle");
            var TableColHeadList = new NonTerminal("TableColHeadList");
            var TableHeadCol = new NonTerminal("TableHeadCol");
            var TableColWidth = new NonTerminal("TableColWidth");
            var TableColFont = new NonTerminal("TableColFont");
            var TableColColors = new NonTerminal("TableColColors");
            var TableRowStyle = new NonTerminal("TableRowStyle");
            var TableRowListOrRowTemplate = new NonTerminal("TableRowListOrRowTemplate");
            var TableRowTemplate = new NonTerminal("TableRowTemplate");
            var TableRowTemplateCount = new NonTerminal("TableRowTemplateCount");

            var PointAutoLocation = new NonTerminal("PointAutoLocation");
            var NumberOrAuto = new NonTerminal("NumberOrAuto");
            var ViewSizeSmt = new NonTerminal("ViewSizeSmt");
            var TitleSmt = new NonTerminal("TitleSmt");
            var MarginArg = new NonTerminal("MarginArg");
            var Parenthesized_Expression = new NonTerminal("Parenthesized_Expression");
            var BinaryExpression = new NonTerminal("BinaryExpression");
            var BinOp = new NonTerminal("BinOp", "operator");
            var UnOp = new NonTerminal("UnOp");
            var semiOpt = new NonTerminal("semiOpt");
            var PixelOrPoint = new NonTerminal("PixelOrPoint");
            var CropExp = new NonTerminal("CropExp");
            var ImageLocation = new NonTerminal("ImageLocation");
            var PieSmt = new NonTerminal("PieSmt");
            var FillPieSmt = new NonTerminal("FillPieSmt");
            var PolygonSmt = new NonTerminal("PolygonSmt");
            var PolygonPoint = new NonTerminal("PolygonPoint");
            var FillPolygonSmt = new NonTerminal("FillPolygonSmt");
            var ForSmt = new NonTerminal("ForSmt");
            var UdfSmt = new NonTerminal("UdfSmt");
            var UdfInvokeSmt = new NonTerminal("UdfInvokeSmt");
            var IfSmt = new NonTerminal("IfSmt");
            var Else_clause_opt = new NonTerminal("Else_clause_opt");
            var then_clause = new NonTerminal("then_clause");

            var FormulaExpression = new NonTerminal("FormulaExpression");
            var LiteralExpression = new NonTerminal("LiteralExpression");
            var FormulaPrimary = new NonTerminal("FormulaTerm");
            var UnaryExpression = new NonTerminal("UnaryExpression");
            var VarSmt = new NonTerminal("VarSmt");
            var VarRef = new NonTerminal("VarRef");
            #endregion

            #region Formula rules
            RegisterOperators(20, "and", "or");
            RegisterOperators(30, "==", "<=", ">=", "<", ">", "<>");
            RegisterOperators(40, "+", "-");
            RegisterOperators(50, "*", "/", "%");
            RegisterOperators(60, "^");

            FormulaRoot = FormulaExpression;
            var CustomFunctionExpression = new NonTerminal("CustomFunctionExpression");
            FormulaExpression.Rule = BinaryExpression | FormulaPrimary;
            FormulaPrimary.Rule = LiteralExpression | UnaryExpression | Parenthesized_Expression | CustomFunctionExpression;

            LiteralExpression.Rule = number_literal | VarRef | sstring;
            UnaryExpression.Rule = UnOp + FormulaExpression;
            Parenthesized_Expression.Rule = lpar + FormulaExpression + rpar;
            BinaryExpression.Rule = FormulaExpression + BinOp + FormulaExpression;

            var CallInvokeArgumentslist = new NonTerminal("CallInvokeArgumentslist");
            CallInvokeArgumentslist.Rule = MakePlusRule(CallInvokeArgumentslist, comma, FormulaExpression);

            var CustomFunctionArgs = new NonTerminal("CustomFunctionArgs");
            CustomFunctionExpression.Rule = variable_literal + CustomFunctionArgs;
            var CustomFunctionArgsOpt = new NonTerminal("CustomFunctionArgsOpt");
            CustomFunctionArgs.Rule = lpar + CustomFunctionArgsOpt + rpar;
            CustomFunctionArgsOpt.Rule = Empty | CallInvokeArgumentslist;

            UnOp.Rule = ToTerm("+") | "-";
            VarRef.Rule = "$" + variable_literal;
            BinOp.Rule = ToTerm("+") | "-" | "*" | "/" | "%" | "==" | "<=" | ">=" | "<" | ">" | "<>" | "and" | "or";
            MarkTransient(FormulaExpression, LiteralExpression, BinOp, FormulaPrimary,
                Parenthesized_Expression, UnOp);
            #endregion

            // set the PROGRAM to be the root node of PDF lines.
            Root = PDF;

            // BNF Rules
            PDF.Rule = MakeStarRule(PDF, PdfLine);

            // A line can be an empty line, or it's a number followed by a statement list ended by a new-line.
            KeyTerm semi = ToTerm(";", "semi");
            semi.ErrorAlias = "';' expected";

            comma.ErrorAlias = "',' expected";
            semiOpt.Rule = Empty | semi;

            PdfLine.Rule = UdfSmt | PdfInstruction;

            PdfInstruction.Rule = PdfPrimaryInstruction + semiOpt;

            PdfPrimaryInstruction.Rule = SetSmt
                | RectSmt
                | FillRectSmt
                | EllipseSmt
                | FillEllipseSmt
                | TitleSmt
                | NewPageSmt
                | ViewSizeSmt
                | LineSmt
                | LineToSmt
                | MoveToSmt
                | LineTextSmt
                | TextSmt
                | TableSmt
                | ImageSmt
                | PieSmt
                | PolygonSmt
                | FillPolygonSmt
                | FillPieSmt
                | ForSmt
                | UdfInvokeSmt
                | IfSmt
            ;

            #region basics rules
            RectLocation.Rule = PointLocation + comma + PointLocation;

            PointLocation.Rule = FormulaExpression + comma + FormulaExpression;
            RectOrPointLocation.Rule = RectLocation | PointLocation;

            #endregion

            SetSmt.Rule = ToTerm("SET") + SetContent;
            SetContent.Rule = PenSmt | BrushSmt | FontSmt | VarSmt | HBrushSmt;
            RectSmt.Rule = ToTerm("RECT") + RectLocation;
            FillRectSmt.Rule = ToTerm("FILLRECT") + RectLocation;
            EllipseSmt.Rule = ToTerm("ELLIPSE") + RectLocation;
            FillEllipseSmt.Rule = ToTerm("FILLELLIPSE") + RectLocation;


            LineSmt.Rule = ToTerm("LINE") + RectLocation;
            MoveToSmt.Rule = ToTerm("MOVETO") + PointLocation;
            LineToSmt.Rule = ToTerm("LINETO") + PointLocation;
            var stylePenOpt = new NonTerminal("StylePenOpt");
            var stylePen = new NonTerminal("StylePen");
            stylePenOpt.Rule = Empty | stylePen;
            stylePen.Rule = ToTerm("solid") | "dash" | "dot" | "dashdot" | "dashdotdot";
            PenSmt.Rule = ToTerm("PEN") + ColorExp + FormulaExpression + stylePenOpt;
            BrushSmt.Rule = ToTerm("BRUSH") + ColorExp + BrushType;
            //TODO: how to deactivate HBRUSH...
            HBrushSmt.Rule = ToTerm("HBRUSH") + ColorExp + BrushType;
            FontSmt.Rule = ToTerm("FONT") + Arg("Name") + FormulaExpression + Arg("Size") + FormulaExpression + styleExpr;

            VarSmt.Rule = ToTerm("VAR") + variable_literal + "=" + FormulaExpression + semi;

            ColorExp.Rule = NamedColor | HexColor;
            foreach (var prop in typeof(XColors).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                var name = prop.Name.ToLowerInvariant();
                if (NamedColor.Rule == null)
                {
                    NamedColor.Rule = ToTerm(name, $"color-{name}");
                }
                else
                {
                    NamedColor.Rule |= ToTerm(name, $"color-{name}");
                }
            }


            HexColor.Rule = colorNumber;
            styleExpr.Rule = Empty;
            foreach (var enumName in Enum.GetNames(typeof(XFontStyle)))
            {
                var styleName = enumName.ToLowerInvariant();
                styleExpr.Rule |= ToTerm(styleName, $"style-{styleName}");
            }


            //TextAlignment is not yet supported on multiline text (only top left is provided by pdfsharpcore)
            //multiline
            TextSmt.Rule = ToTerm("TEXT") + RectOrPointLocation /*+ TextAlignment */+ textString;

            TextAlignment.Rule = HAlign + VAlign;
            HAlign.Rule = Empty | Arg("HAlign") + HAlignValue;
            VAlign.Rule = Empty | Arg("VAlign") + VAlignValue;
            HAlignValue.Rule = ToTerm("left") | "right" | "hcenter";
            VAlignValue.Rule = ToTerm("top") | "bottom" | "vcenter";

            TextOrientation.Rule = Empty | Arg("Orientation") + TextOrientationValue;
            TextOrientationValue.Rule = FormulaExpression
                | "vertical"
                | "horizontal";

            //simple line
            LineTextSmt.Rule = ToTerm("LINETEXT") + RectOrPointLocation + TextAlignment + TextOrientation
                + Arg("Text") + FormulaExpression;
            BrushType.Rule = Empty /* | GradientBrush*/;

            PageSize.Rule = Empty;

            var names = Enum.GetNames(typeof(PageSize));
            var firstSize = names.First();
            PageSize.Rule |= ToTerm(firstSize, $"pagesize-{firstSize}");
            foreach (var prop in names.Skip(1))
            {
                PageSize.Rule |= ToTerm(prop, $"pagesize-{prop}");
            }

            PageOrientation.Rule = Empty | "portrait" | "landscape";
            NewPageSmt.Rule = ToTerm("NEWPAGE") + PageSize + PageOrientation;

            TitleSmt.Rule = ToTerm("TITLE") + MarginArg + HAlign + Arg("Text") + FormulaExpression;
            MarginArg.Rule = Empty | Arg("Margin") + FormulaExpression;


            TableSmt.Rule = ToTerm("TABLE") + TableLocation + TableContent + ToTerm("ENDTABLE");

            TableContent.Rule = TableHead + TableRowListOrRowTemplate;

            TableHead.Rule = ToTerm("HEAD") + TableHeadStyle + TableColHeadList + ToTerm("ENDHEAD");
            TableRowList.Rule = MakeStarRule(TableRowList, TableRow);
            TableRowListOrRowTemplate.Rule = TableRowList | TableRowTemplate;
            TableRowTemplateCount.Rule = FormulaExpression;
            TableRowTemplate.Rule = ToTerm("ROWTEMPLATE") + TableRowTemplateCount + TableColList + ToTerm("ENDROW");
            TableColHeadList.Rule = MakeStarRule(TableColHeadList, TableHeadCol);
            TableHeadCol.Rule = ToTerm("COL") + TableColWidth + TableColFont + TableColColors + sstring + semi;
            //desiredWidth and maxWidth
            TableColWidth.Rule = Arg("Width") + NumberOrAuto + Arg("MaxWidth") + NumberOrAuto;
            TableColList.Rule = MakeStarRule(TableColList, TableCol);
            TableRow.Rule = ToTerm("ROW") + TableRowStyle + TableColList + ToTerm("ENDROW");
            TableCol.Rule = ToTerm("COL") + FormulaExpression + semi;
            TableLocation.Rule = PointLocation /*+ "," + PointAutoLocation*/;
            PointAutoLocation.Rule = NumberOrAuto + "," + NumberOrAuto;
            NumberOrAuto.Rule = FormulaExpression | "auto";
            TableHeadStyle.Rule = Empty | ColorExp;
            TableColFont.Rule = Empty | Arg("FONT") + sstring + "," + number_literal + "," + styleExpr;
            TableColColors.Rule = Empty | ColorExp + ColorExp;
            TableRowStyle.Rule = Empty | FormulaExpression;

            ViewSizeSmt.Rule = ToTerm("VIEWSIZE") + PointLocation;

            PixelOrPoint.Rule = ToTerm("pixel") | "point";
            CropExp.Rule = Empty | "crop" | "fit";
            ImageLocation.Rule = PointLocation | RectLocation + PixelOrPoint + CropExp;
            //PreferShift because
            var ImageRawOrSource = new NonTerminal("ImageRawOrSource");
            ImageSmt.Rule = ToTerm("IMAGE") + ImageLocation + ImageRawOrSource + FormulaExpression;
            ImageRawOrSource.Rule = Arg("Source") | Arg("Data");

            PieSmt.Rule = ToTerm("PIE") + RectLocation + Arg("Start") + FormulaExpression + Arg("Angle") + FormulaExpression;
            PolygonSmt.Rule = ToTerm("POLYGON") + PointLocation + comma + PointLocation + comma + PolygonPoint;
            PolygonPoint.Rule = MakePlusRule(PolygonPoint, comma, PointLocation);
            FillPieSmt.Rule = ToTerm("FILLPIE") + RectLocation + Arg("Start") + FormulaExpression + Arg("Angle") + FormulaExpression;
            FillPolygonSmt.Rule = ToTerm("FILLPOLYGON") + PointLocation + comma + PointLocation + comma + PolygonPoint;

            var EmbbededSmtList = new NonTerminal("EmbbededSmtList");
            var ForBlock = new NonTerminal("ForBlock");
            ForSmt.Rule = ToTerm("FOR") + variable_literal + "=" + FormulaExpression + "TO" + FormulaExpression + ForBlock;
            var EmbbededSmtListOpt = new NonTerminal("EmbbededSmtListOpt");
            ForBlock.Rule = ToTerm("DO") + EmbbededSmtListOpt + "ENDFOR";
            EmbbededSmtListOpt.Rule = Empty + EmbbededSmtList;
            EmbbededSmtList.Rule = MakePlusRule(EmbbededSmtList, null, PdfInstruction);

            var UdfArgumentslistOpt = new NonTerminal("UdfArgumentslistOpt");
            var UdfArguments = new NonTerminal("UdfArguments");
            var UdfArgumentslist = new NonTerminal("UdfArgumentslist");
            var UdfBlock = new NonTerminal("UdfBlock");
            UdfArguments.Rule = lpar + UdfArgumentslistOpt + rpar;
            UdfArgumentslistOpt.Rule = Empty | UdfArgumentslist;
            UdfSmt.Rule = ToTerm("UDF") + variable_literal + PreferShiftHere() + UdfArguments + UdfBlock;
            UdfArgumentslist.Rule = MakePlusRule(UdfArgumentslist, comma, variable_literal);
            UdfBlock.Rule = EmbbededSmtListOpt + "ENDUDF";


            var UdfInvokeArguments = new NonTerminal("UdfInvokeArguments");
            var UdfInvokeArgumentslistOpt = new NonTerminal("UdfInvokeArgumentslistOpt");
            UdfInvokeSmt.Rule = ToTerm("CALL") + variable_literal + PreferShiftHere() + UdfInvokeArguments;
            UdfInvokeArguments.Rule = lpar + UdfInvokeArgumentslistOpt + rpar;
            UdfInvokeArgumentslistOpt.Rule = Empty | CallInvokeArgumentslist;

            IfSmt.Rule = "IF" + FormulaExpression + then_clause + Else_clause_opt + "ENDIF";
            then_clause.Rule = "THEN" + EmbbededSmtListOpt;
            Else_clause_opt.Rule = Empty | PreferShiftHere() + "ELSE" + EmbbededSmtListOpt;


            RegisterBracePair("(", ")");

            MarkPunctuation(";", ",", "(", ")", "TABLE", "ENDTABLE", "HEAD", "ENDHEAD", "ROW", "ROWTEMPLATE ", "ENDROW", "ENDFOR", "UDF", "ENDUDF", "IF", "THEN", "ELSE", "ENDIF");
            RegisterBracePair("(", ")");
            MarkTransient(PdfLine, PdfPrimaryInstruction, SetContent, NumberOrAuto,
                 styleExpr, semiOpt, PixelOrPoint, HAlignValue, TextOrientationValue, VAlignValue,
                 EmbbededSmtListOpt,
                 UdfArguments, UdfArgumentslistOpt,
                 UdfInvokeArguments, UdfInvokeArgumentslistOpt, stylePenOpt,
                 CustomFunctionArgs, CustomFunctionArgsOpt, TableRowTemplateCount);

            this.AddTermsReportGroup("punctuation", comma);
            this.AddToNoReportGroup("(", "++", "--");
            this.AddOperatorReportGroup("operator");
            this.AddTermsReportGroup("constant", number_literal, sstring);
            this.AddTermsReportGroup("constant", "auto");
            this.AddToNoReportGroup(semi);
        }

        /// <summary>
        /// for argument in grammar
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        BnfExpression Arg(string name)
        {
            return name + PreferShiftHere() + "=";
        }

        public override string ConstructParserErrorMessage(ParsingContext context, StringSet expectedTerms)
        {
            if (context.CurrentParserState.ExpectedTerminals.Count > 0 && expectedTerms.Count == 0)
            {
                expectedTerms.AddRange(TerminalToString(context.CurrentParserState.ExpectedTerminals));
                return base.ConstructParserErrorMessage(context, expectedTerms);
            }
            return base.ConstructParserErrorMessage(context, expectedTerms);
        }

        private string[] TerminalToString(TerminalSet expectedTerminals)
        {
            var l = new List<string>();
            foreach (var item in expectedTerminals)
            {
                l.Add(item switch {
                    KeyTerm k => k.Text,
                    _ => item.ToString(),
                });
            }
            return l.ToArray();
        }

        public override void ReportParseError(ParsingContext context)
        {
            base.ReportParseError(context);
        }
    }
}