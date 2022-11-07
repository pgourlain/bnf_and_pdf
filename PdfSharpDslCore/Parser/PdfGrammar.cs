
using Irony;
using Irony.Parsing;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.BarCodes;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("pdfsharpdslTests")]


namespace PdfSharpDslCore.Parser
{
    [Language("PdfGrammar", "1.0", "Grammar to write PDF with PdfSharp")]
    public class PdfGrammar : Grammar
    {
        public PdfGrammar()
        {
            var sstring = new StringLiteral("string", "\"", StringOptions.AllowsDoubledQuote);
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


            var comment = new CommentTerminal("comment", "#", "\r", "\n", "\u2085", "\u2028", "\u2029");
            //comment must to be added to NonGrammarTerminals list; it is not used directly in grammar rules,
            // so we add it to this list to let Scanner know that it is also a valid terminal. 
            NonGrammarTerminals.Add(comment);

            #region variables
            var PDF = new NonTerminal("PDF");
            var PdfLine = new NonTerminal("PdfLine");
            var PdfLineContent = new NonTerminal("PdfLineContent");
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
            var TextOrientationAndText = new NonTerminal("TextOrientationAndText");
            var HAlign = new NonTerminal("HAlign");
            var VAlign = new NonTerminal("VAlign");
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
            var PointAutoLocation = new NonTerminal("PointAutoLocation");
            var NumberOrAuto = new NonTerminal("NumberOrAuto");
            var ViewSizeSmt = new NonTerminal("ViewSizeSmt");
            var Title = new NonTerminal("TitleSmt");
            var Margin = new NonTerminal("Margin");
            var Parenthesized_Expression = new NonTerminal("Parenthesized_Expression");
            var BinaryExpression = new NonTerminal("BinaryExpression");
            var BinOp = new NonTerminal("BinOp");
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
            
            
            var FormulaExpression = new NonTerminal("FormulaExpression");
            var FormulaTerm = new NonTerminal("FormulaTerm");
            var UnaryExpression = new NonTerminal("UnaryExpression");
            var VarSmt = new NonTerminal("VarSmt");
            var VarRef = new NonTerminal("VarRef");

            FormulaExpression.Rule = FormulaTerm | UnaryExpression | BinaryExpression;
            FormulaTerm.Rule = number_literal | Parenthesized_Expression | VarRef | sstring;
            UnaryExpression.Rule = UnOp + FormulaTerm;
            UnOp.Rule = ToTerm("+") | "-";
            VarRef.Rule = "$" + variable_literal;
            //NumberExpression.Rule = number_literal | MultipleNumberExpression | variable_literal;
            //MultipleNumberExpression.Rule = BinaryExpression | Parenthesized_NumberExpression;
            Parenthesized_Expression.Rule = ToTerm("(") + FormulaExpression + ToTerm(")");
            BinaryExpression.Rule = FormulaExpression + BinOp + FormulaExpression;

            BinOp.Rule = ToTerm("+") | "-" | "*" | "/";
            #endregion

            // set the PROGRAM to be the root node of PDF lines.
            Root = PDF;

            // BNF Rules
            PDF.Rule = MakeStarRule(PDF, PdfLine);

            // A line can be an empty line, or it's a number followed by a statement list ended by a new-line.
            KeyTerm semi = ToTerm(";", "semi");
            semi.ErrorAlias = "';' expected";
            KeyTerm comma = ToTerm(",", "comma");
            comma.ErrorAlias = "',' expected";
            semiOpt.Rule = Empty | semi;

            PdfLine.Rule = PdfLineContent + semiOpt;

            PdfLineContent.Rule = SetSmt
                | RectSmt
                | FillRectSmt
                | EllipseSmt
                | FillEllipseSmt
                | Title
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
            ;

            #region basics rules
            RectLocation.Rule = PointLocation + comma + PointLocation;

            PointLocation.Rule = FormulaExpression + comma + FormulaExpression;
            RectOrPointLocation.Rule = RectLocation | PointLocation;

            #endregion

            SetSmt.Rule = ToTerm("SET") + SetContent;
            SetContent.Rule = PenSmt | BrushSmt | FontSmt | VarSmt;
            RectSmt.Rule = ToTerm("RECT") + RectLocation;
            FillRectSmt.Rule = ToTerm("FILLRECT") + RectLocation;
            EllipseSmt.Rule = ToTerm("ELLIPSE") + RectLocation;
            FillEllipseSmt.Rule = ToTerm("FILLELLIPSE") + RectLocation;
            

            LineSmt.Rule = ToTerm("LINE") + RectLocation;
            MoveToSmt.Rule = ToTerm("MOVETO") + PointLocation;
            LineToSmt.Rule = ToTerm("LINETO") + PointLocation;
            PenSmt.Rule = ToTerm("PEN") + ColorExp + FormulaExpression;
            BrushSmt.Rule = ToTerm("BRUSH") + ColorExp + BrushType;
            FontSmt.Rule = ToTerm("FONT") + sstring + FormulaExpression + styleExpr;
            VarSmt.Rule = ToTerm("VAR") + variable_literal+"="+ FormulaExpression;

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
            HAlign.Rule = Empty | "left" | "right" | "hcenter";
            VAlign.Rule = Empty | "top" | "bottom" | "vcenter";
            TextOrientationAndText.Rule = Empty + FormulaExpression | "vertical" + FormulaExpression | "horizontal" + FormulaExpression | FormulaExpression + FormulaExpression;

            //simple line
            LineTextSmt.Rule = ToTerm("LINETEXT") + RectOrPointLocation + TextAlignment + TextOrientationAndText;
            BrushType.Rule = Empty /* | GradientBrush*/;

            PageSize.Rule = Empty;
            foreach (var prop in Enum.GetNames(typeof(PageSize)))
            {
                PageSize.Rule |= ToTerm(prop, $"pagesize-{prop}");
            }
            
            PageOrientation.Rule = Empty | "portrait" | "landscape";
            NewPageSmt.Rule = ToTerm("NEWPAGE") + PageSize + PageOrientation;

            Title.Rule = ToTerm("TITLE") + Margin + HAlign + FormulaExpression;
            Margin.Rule = Empty | FormulaExpression;


            TableSmt.Rule = ToTerm("TABLE") + TableLocation + TableContent + ToTerm("ENDTABLE");

            TableContent.Rule = TableHead + TableRowList;

            TableHead.Rule = ToTerm("HEAD") + TableHeadStyle + TableColHeadList + ToTerm("ENDHEAD");
            TableRowList.Rule = MakeStarRule(TableRowList, TableRow);
            TableColHeadList.Rule = MakeStarRule(TableColHeadList, TableHeadCol);
            TableHeadCol.Rule = ToTerm("COL") + TableColWidth + TableColFont + TableColColors + sstring + semi;
            //desiredWidth and maxWidth
            TableColWidth.Rule = NumberOrAuto + NumberOrAuto;
            TableColList.Rule = MakeStarRule(TableColList, TableCol);
            TableRow.Rule = ToTerm("ROW") + TableRowStyle + TableColList + ToTerm("ENDROW");
            TableCol.Rule = ToTerm("COL") + sstring + semi;
            TableLocation.Rule = PointLocation /*+ "," + PointAutoLocation*/;
            PointAutoLocation.Rule = NumberOrAuto + "," + NumberOrAuto;
            NumberOrAuto.Rule = FormulaExpression | "auto";
            TableHeadStyle.Rule = Empty | ColorExp;
            TableColFont.Rule = Empty | ToTerm("FONT=") + sstring + "," + number_literal + "," + styleExpr;
            TableColColors.Rule = Empty | ColorExp + ColorExp;
            TableRowStyle.Rule = Empty | FormulaExpression;

            ViewSizeSmt.Rule = ToTerm("VIEWSIZE") + PointLocation;

            PixelOrPoint.Rule = ToTerm("pixel") | "point";
            CropExp.Rule = Empty | "crop" | "fit";
            ImageLocation.Rule = PointLocation | RectLocation + PixelOrPoint + CropExp;
            //PreferShift because
            ImageSmt.Rule = ToTerm("IMAGE") + ImageLocation + ImplyPrecedenceHere(11) + FormulaExpression;

            PieSmt.Rule = ToTerm("PIE") + RectLocation + FormulaExpression + FormulaExpression;
            PolygonSmt.Rule = ToTerm("POLYGON") + PointLocation + PointLocation + PolygonPoint;
            PolygonPoint.Rule = MakePlusRule(PolygonPoint, PointLocation);
            FillPieSmt.Rule = ToTerm("FILLPIE") + RectLocation + FormulaExpression + FormulaExpression;
            FillPolygonSmt.Rule = ToTerm("FILLPOLYGON") + PointLocation + PointLocation + PolygonPoint;

            RegisterOperators(1, "+", "-");
            RegisterOperators(2, "*", "/");
            RegisterBracePair("(", ")");

            MarkPunctuation(";", ",", "(", ")", "TABLE", "ENDTABLE", "HEAD", "ENDHEAD", "ROW", "ENDROW");
            RegisterBracePair("(", ")");
            MarkTransient(PdfLineContent, SetContent, NumberOrAuto, Parenthesized_Expression,
                BinOp, styleExpr, semiOpt, PixelOrPoint, UnOp, FormulaExpression, FormulaTerm);

            this.AddTermsReportGroup("punctuation", comma);
            this.AddTermsReportGroup("operator", "+", "-", "/", "*");
            this.AddTermsReportGroup("constant", number_literal, sstring);
            this.AddTermsReportGroup("constant", "auto");
            this.AddToNoReportGroup(semi);
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
                l.Add(item switch
                {
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