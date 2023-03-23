using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    internal class TextDocumentDrawer : IPdfDocumentDrawer
    {
        public StringBuilder OutputRendering { get; private set; } = new StringBuilder();
        public XPen CurrentPen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public XBrush CurrentBrush { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public XBrush? HighlightBrush { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public XFont CurrentFont
        {
            get => throw new NotImplementedException();
            set
            {
            }
        }

        public double PageWidth => 21 * 28.34;

        public double PageHeight => 29.7 * 28.34;

        public DebugOptions DebugOptions { get; set ; }

        public void DrawEllipse(double x, double y, double w, double h, bool isFilled)
        {
            throw new NotImplementedException();
        }

        public void DrawImage(XImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage)
        {
            throw new NotImplementedException();
        }

        public void DrawLine(double x, double y, double x1, double y1)
        {
            throw new NotImplementedException();
        }

        public void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle, bool isFilled)
        {
            throw new NotImplementedException();
        }

        public void DrawPolygon(IEnumerable<XPoint> points, bool isFilled)
        {
            throw new NotImplementedException();
        }

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
        {
            throw new NotImplementedException();
        }

        public void DrawTable(double x, double y, TableDefinition tblDef)
        {
            throw new NotImplementedException();
        }

        public void DrawText(string text, double x, double y, double? w, double? h)
        {
            throw new NotImplementedException();
        }
        public void DrawLineText(string text, double x, double y, double? w, double? h, XStringAlignment hAlign, XLineAlignment vAlign, TextOrientation textOrientation)
        {
            var halign = $"HAlign={ToHAlign(hAlign)}";
            OutputRendering.Append($"LINETEXT ");
            OutputRendering.Append($"{x},{y}");
            if (w is not null)
            {
                OutputRendering.Append($",{w},{h}");
            }
            OutputRendering.Append($" HAlign={ToHAlign(hAlign)} VAlign={ToVAlign(vAlign)} ");
            OutputRendering.Append($"Text=\"{text}\"");
            OutputRendering.AppendLine($";");

        }

        public void DrawTitle(string text, double margin, XStringAlignment hAlign, XLineAlignment vAlign)
        {
            var halign = $"HAlign={ToHAlign(hAlign)}";
            OutputRendering.AppendLine($"TITLE Margin={margin.ToString(CultureInfo.InvariantCulture)} {halign} Text=\"{text}\";");
        }

        private string ToHAlign(XStringAlignment hAlign)
        {
            switch (hAlign)
            {
                case XStringAlignment.Near:
                    return "left";
                case XStringAlignment.Center:
                    return "hcenter";
                case XStringAlignment.Far:
                    return "right";
                default:
                    return "left";
            }
        }
        private string ToVAlign(XLineAlignment vAlign)
        {
            switch (vAlign)
            {
                case XLineAlignment.Near:
                    return "top";
                case XLineAlignment.Center:
                    return "vcenter";
                case XLineAlignment.Far:
                    return "bottom";
                default:
                    return "top";
            }
        }

        public void LineTo(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void MoveTo(double x, double y)
        {
            throw new NotImplementedException();
        }

        public void NewPage(PageSize? pageSize = null, PageOrientation? pageOrientation = null)
        {
            throw new NotImplementedException();
        }

        public void SetViewSize(double w, double h)
        {
            throw new NotImplementedException();
        }

        public void BeginDrawRowTemplate(int index, double offsetY)
        {
            throw new NotImplementedException();
        }

        public XRect EndDrawRowTemplate(int index)
        {
            throw new NotImplementedException();
        }

        public void BeginIterationTemplate(int rowCount)
        {
            throw new NotImplementedException();
        }

        public void EndIterationTemplate(double drawHeight)
        {
            throw new NotImplementedException();
        }

        public void SetOffsetY(double offsetY)
        {
            throw new NotImplementedException();
        }

        public void ResetOffset()
        {
            throw new NotImplementedException();
        }
    }
}
