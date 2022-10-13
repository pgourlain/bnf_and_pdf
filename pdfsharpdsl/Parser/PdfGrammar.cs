
using Irony.Parsing;
using PdfSharpCore.Drawing;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using System.Linq.Expressions;

namespace Pdf.Parser
{
    [Language("PdfGrammar", "1.0", "Grammar to write PDF with PdfSharp")]
    public class PdfGrammar : Grammar
    {
        public PdfGrammar()
        {
            var sstring = new StringLiteral("string", "\"", StringOptions.AllowsDoubledQuote);
            var textString = new StringLiteral("string", "\"", StringOptions.AllowsDoubledQuote | StringOptions.AllowsAllEscapes | StringOptions.AllowsLineBreak);
            var number_literal = new NumberLiteral("number", NumberOptions.AllowSign);
            var colorNumber = new NumberLiteral("ColorValue");
            colorNumber.AddPrefix("g", NumberOptions.Default | NumberOptions.AllowStartEndDot);
            colorNumber.AddPrefix("0x", NumberOptions.Hex);

            var comment = new CommentTerminal("comment", "#", "\n", "\r");
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
            var ColorExp = new NonTerminal("ColorExp");
            var NamedColor = new NonTerminal("NamedColor");
            var HexColor = new NonTerminal("HexColor");
            var styleExpr = new NonTerminal("styleExpr");
            var RectLocation = new NonTerminal("RectLocation");
            var PointLocation = new NonTerminal("PointLocation");
            var SetContent = new NonTerminal("SetContent");
            var TextSmt = new NonTerminal("TextSmt");
            var TextLocation = new NonTerminal("TextLocation");
            var TextAlignment = new NonTerminal("TextAlignment");
            var TextOrientation = new NonTerminal("TextOrientation");
            var HAlign = new NonTerminal("HAlign");
            var VAlign = new NonTerminal("VAlign");
            var LineTextSmt = new NonTerminal("LineTextSmt");
            var NewPageSmt = new NonTerminal("NewPage");
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
            var PointAutoLocation = new NonTerminal("PointAutoLocation");
            var NumberOrAuto = new NonTerminal("NumberOrAuto");
            var ViewSizeSmt = new NonTerminal("ViewSizeSmt");
            var Title = new NonTerminal("TitleSmt");
            var Margin = new NonTerminal("Margin");
            var NumberExpression = new NonTerminal("NumberExpression");
            var MultipleNumberExpression = new NonTerminal("MultipleNumberExpression");
            var Parenthesized_NumberExpression = new NonTerminal("Parenthesized_NumberExpression");
            var BinaryExpression = new NonTerminal("BinaryExpression");
            var Operator = new NonTerminal("Operator");

            NumberExpression.Rule = number_literal | MultipleNumberExpression;
            MultipleNumberExpression.Rule = BinaryExpression | Parenthesized_NumberExpression;
            Parenthesized_NumberExpression.Rule = ToTerm("(") + NumberExpression + ToTerm(")");
            BinaryExpression.Rule = NumberExpression + Operator + NumberExpression;
            var opPlus = ToTerm("+");
            var opMinus = ToTerm("-");
            var opDivide = ToTerm("/");
            var opMultiply = ToTerm("*");

            Operator.Rule = opPlus | opMinus | opDivide | opMultiply;
            #endregion

            // set the PROGRAM to be the root node of PDF lines.
            Root = PDF;

            // BNF Rules
            PDF.Rule = MakePlusRule(PDF, PdfLine);

            // A line can be an empty line, or it's a number followed by a statement list ended by a new-line.
            PdfLine.Rule = PdfLineContent + NewLine;

            PdfLineContent.Rule = SetSmt | RectSmt | TextSmt | LineTextSmt | NewPageSmt
            | FillRectSmt | TableSmt | ViewSizeSmt | LineSmt | LineToSmt | MoveToSmt
            | Title | EllipseSmt | FillEllipseSmt
            | Empty;

            #region basics rules
            RectLocation.Rule = PointLocation + "," + PointLocation;

            PointLocation.Rule = NumberExpression + "," + NumberExpression;
            TextLocation.Rule = RectLocation | PointLocation;

            #endregion

            SetSmt.Rule = ToTerm("SET") + SetContent;
            SetContent.Rule = PenSmt | BrushSmt | FontSmt;
            RectSmt.Rule = ToTerm("RECT") + RectLocation;
            FillRectSmt.Rule = ToTerm("FILLRECT") + RectLocation;
            EllipseSmt.Rule = ToTerm("ELLIPSE") + RectLocation;
            FillEllipseSmt.Rule = ToTerm("FILLELLIPSE") + RectLocation;

            LineSmt.Rule = ToTerm("LINE") + RectLocation;
            MoveToSmt.Rule = ToTerm("MOVETO") + PointLocation;
            LineToSmt.Rule = ToTerm("LINETO") + PointLocation;
            PenSmt.Rule = ToTerm("PEN") + ColorExp + number_literal;
            BrushSmt.Rule = ToTerm("BRUSH") + ColorExp + BrushType;
            FontSmt.Rule = ToTerm("FONT") + sstring + number_literal + styleExpr;

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
            TextSmt.Rule = ToTerm("TEXT") + TextLocation /*+ TextAlignment */+ textString;

            TextAlignment.Rule = HAlign + VAlign;
            HAlign.Rule = Empty | "left" | "right" | "hcenter";
            VAlign.Rule = Empty | "top" | "bottom" | "vcenter";
            TextOrientation.Rule = Empty | "vertical" | "horizontal" | number_literal; //number for angle in degree

            //simple line
            LineTextSmt.Rule = ToTerm("LINETEXT") + TextLocation + TextAlignment + TextOrientation + sstring;
            BrushType.Rule = Empty /* | GradientBrush*/;

            NewPageSmt.Rule = ToTerm("NEWPAGE");

            Title.Rule = ToTerm("TITLE") + Margin + TextAlignment + sstring;
            Margin.Rule = Empty | number_literal;


            TableContent.Rule = NewLinePlus + TableHead + NewLinePlus + TableRowList;
            TableHead.Rule = ToTerm("HEAD") + TableHeadStyle + NewLine + TableColHeadList + ToTerm("ENDHEAD");
            TableRowList.Rule = MakeStarRule(TableRowList, TableRow);
            TableColHeadList.Rule = MakePlusRule(TableColHeadList, TableHeadCol);
            TableHeadCol.Rule = ToTerm("COL") + TableColWidth + TableColFont + sstring + NewLine;
            //desiredWidth and maxWidth
            TableColWidth.Rule = NumberOrAuto + NumberOrAuto;
            TableColList.Rule = MakeStarRule(TableColList, TableCol);
            TableRow.Rule = ToTerm("ROW") + NewLine + TableColList + ToTerm("ENDROW") + NewLine;
            TableCol.Rule = ToTerm("COL") + sstring + NewLine;
            TableLocation.Rule = PointLocation /*+ "," + PointAutoLocation*/;
            PointAutoLocation.Rule = NumberOrAuto + "," + NumberOrAuto;
            NumberOrAuto.Rule = NumberExpression | "auto";
            TableSmt.Rule = ToTerm("TABLE") + TableLocation + TableContent + ToTerm("ENDTABLE");
            TableSmt.SetFlag(TermFlags.IsMultiline, true);
            TableHeadStyle.Rule = Empty | ColorExp;
            TableColFont.Rule = Empty | ToTerm("FONT=") + sstring + "," + number_literal + "," + styleExpr;


            ViewSizeSmt.Rule = ToTerm("VIEWSIZE") + PointLocation;


            RegisterOperators(10, opDivide, opMultiply);
            RegisterOperators(9, opPlus, opMinus);

            MarkPunctuation(",", "(", ")");
            RegisterBracePair("(", ")");
            MarkTransient(PdfLine, PdfLineContent, SetContent, NumberOrAuto, Parenthesized_NumberExpression,
                Operator, MultipleNumberExpression, styleExpr);
        }
    }
}