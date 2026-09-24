

using System;
using System.Collections.Generic;

namespace PdfSharpDslCore.Drawing
{
    public enum TextOrientationEnum
    {
        Horizontal, Vertical, HorizontalInvert, VerticalInvert,
    }

    public record TextOrientation
    {
        public TextOrientationEnum Orientation { get; set; }
        public double? Angle { get; set; }
    }

    public record DrawingResult
    {
        public PdfRect DrawingRect { get; set; }
        public double PageOffsetY { get; set; }
    }

    public interface IPdfDocumentDrawer
    {
        void DrawRect(double x, double y, double w, double h, bool isFilled);
        void DrawText(string text, double x, double y, double? w, double? h);
        void DrawLineText(string text, double x, double y, double? w, double? h,
            PdfHorizontalAlignment hAlign, PdfVerticalAlignment vAlign, TextOrientation textOrientation,
            TextFitOptions? fitOptions = null);
        void SetViewSize(double w, double h);
        PdfPen CurrentPen { get; set; }
        PdfBrush CurrentBrush { get; set; }
        PdfBrush? HighlightBrush { get; set; }
        PdfFont CurrentFont { get; set; }
        double PageWidth { get; }
        double PageHeight { get; }

        /// <summary>Measures <paramref name="text"/> in points, using the current font (see <see cref="CurrentFont"/>).</summary>
        /// <param name="maxWidth">When set, wraps the text as <see cref="DrawLineText"/> would before measuring.</param>
        PdfSize MeasureText(string text, double? maxWidth);
        
        DebugOptions DebugOptions { get; set; }
        /// <summary>
        /// debug options for the current page, reset on each new page
        /// </summary>
        DebugOptions PageDebugOptions { get; set; }

        void NewPage(PdfPageSize? pageSize = null, PdfPageOrientation? pageOrientation = null);
        void DrawLine(double x, double y, double x1, double y1);
        void DrawTitle(string text, double margin, PdfHorizontalAlignment hAlign, PdfVerticalAlignment vAlign);
        void DrawEllipse(double x, double y, double w, double h, bool isFilled);
        void MoveTo(double x, double y);
        void LineTo(double x, double y);
        void DrawTable(double x, double y, TableDefinition tblDef);
        void DrawImage(PdfImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage);
        void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle, bool isFilled);
        void DrawPolygon(IEnumerable<PdfPoint> points, bool isFilled);
        void BeginDrawRowTemplate(string name, int index, double offsetY, double newPageTopMargin);
        DrawingResult EndDrawRowTemplate(int index);
        void BeginIterationTemplate(int rowCount);
        void EndIterationTemplate(double drawHeight);

        void RegisterOnNewPage(Action<int> callback);
        void UnRegisterOnNewPage(Action<int> callback);
    }
}