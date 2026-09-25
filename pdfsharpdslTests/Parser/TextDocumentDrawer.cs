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
        public PdfPen CurrentPen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PdfBrush CurrentBrush { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PdfBrush? HighlightBrush { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public PdfFont CurrentFont
        {
            get => throw new NotImplementedException();
            set
            {
            }
        }

        public double PageWidth => 21 * 28.34;

        public double PageHeight => 29.7 * 28.34;

        public PdfSize MeasureText(string text, double? maxWidth)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<string> WrapText(string text, double maxWidth)
        {
            throw new NotImplementedException();
        }

        public PdfSize MeasureImage(PdfImage image, double? w, double? h, bool sizeInPixel)
        {
            throw new NotImplementedException();
        }

        public DebugOptions DebugOptions { get; set ; }

        public DebugOptions PageDebugOptions { get; set; }

        public void DrawEllipse(double x, double y, double w, double h, bool isFilled)
        {
            throw new NotImplementedException();
        }

        public void DrawImage(PdfImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage)
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

        public void DrawPolygon(IEnumerable<PdfPoint> points, bool isFilled)
        {
            throw new NotImplementedException();
        }

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
        {
            throw new NotImplementedException();
        }

        public PdfRect DrawTable(double x, double y, TableDefinition tblDef)
        {
            throw new NotImplementedException();
        }

        public void DrawBarcode(double x, double y, double w, double h, PdfBarcodeType type, string text)
        {
            throw new NotImplementedException();
        }

        public void DrawText(string text, double x, double y, double? w, double? h)
        {
            throw new NotImplementedException();
        }
        public void DrawLineText(string text, double x, double y, double? w, double? h, PdfHorizontalAlignment hAlign, PdfVerticalAlignment vAlign, TextOrientation textOrientation, TextFitOptions? fitOptions = null)
        {
            var halign = $"HAlign={ToHAlign(hAlign)}";
            OutputRendering.Append($"LINETEXT ");
            FormattableString s = $"{x},{y}";
            OutputRendering.Append(s.ToString(CultureInfo.InvariantCulture));
            if (w is not null)
            {
                s = $",{w},{h}";
                OutputRendering.Append(s.ToString(CultureInfo.InvariantCulture));
            }
            OutputRendering.Append($" HAlign={ToHAlign(hAlign)} VAlign={ToVAlign(vAlign)} ");
            OutputRendering.Append($"Text=\"{text}\"");
            OutputRendering.AppendLine($";");

        }

        public void DrawTitle(string text, double margin, PdfHorizontalAlignment hAlign, PdfVerticalAlignment vAlign)
        {
            var halign = $"HAlign={ToHAlign(hAlign)}";
            OutputRendering.AppendLine($"TITLE Margin={margin.ToString(CultureInfo.InvariantCulture)} {halign} Text=\"{text}\";");
        }

        private string ToHAlign(PdfHorizontalAlignment hAlign)
        {
            switch (hAlign)
            {
                case PdfHorizontalAlignment.Near:
                    return "left";
                case PdfHorizontalAlignment.Center:
                    return "hcenter";
                case PdfHorizontalAlignment.Far:
                    return "right";
                default:
                    return "left";
            }
        }
        private string ToVAlign(PdfVerticalAlignment vAlign)
        {
            switch (vAlign)
            {
                case PdfVerticalAlignment.Near:
                    return "top";
                case PdfVerticalAlignment.Center:
                    return "vcenter";
                case PdfVerticalAlignment.Far:
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

        public void NewPage(PdfPageSize? pageSize = null, PdfPageOrientation? pageOrientation = null)
        {
            throw new NotImplementedException();
        }

        public void SetViewSize(double w, double h)
        {
            throw new NotImplementedException();
        }

        public void BeginDrawRowTemplate(string name, int index, double offsetY, double newPageTopMargin)
        {
            throw new NotImplementedException();
        }

        public DrawingResult EndDrawRowTemplate(int index)
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

        public void RegisterOnNewPage(Action<int> callback)
        {
            //throw new NotImplementedException();
        }

        public void UnRegisterOnNewPage(Action<int> callback)
        {
            //throw new NotImplementedException();
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
