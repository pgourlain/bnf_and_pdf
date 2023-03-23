

using PdfSharpCore;
using PdfSharpCore.Drawing;
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

    public interface IPdfDocumentDrawer
    {
        void DrawRect(double x, double y, double w, double h, bool isFilled);
        void DrawText(string text, double x, double y, double? w, double? h);
        void DrawLineText(string text, double x, double y, double? w, double? h,
            XStringAlignment hAlign, XLineAlignment vAlign, TextOrientation textOrientation);
        void SetViewSize(double w, double h);
        XPen CurrentPen { get; set; }
        XBrush CurrentBrush { get; set; }
        XBrush? HighlightBrush { get; set; }
        XFont CurrentFont { get; set; }
        double PageWidth { get; }
        double PageHeight { get; }
        
        DebugOptions DebugOptions { get; set; }

        void NewPage(PageSize? pageSize = null, PageOrientation? pageOrientation = null);
        void DrawLine(double x, double y, double x1, double y1);
        void DrawTitle(string text, double margin, XStringAlignment hAlign, XLineAlignment vAlign);
        void DrawEllipse(double x, double y, double w, double h, bool isFilled);
        void MoveTo(double x, double y);
        void LineTo(double x, double y);
        void DrawTable(double x, double y, TableDefinition tblDef);
        void DrawImage(XImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage);
        void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle, bool isFilled);
        void DrawPolygon(IEnumerable<XPoint> points, bool isFilled);
        void BeginDrawRowTemplate(int index, double offsetY);
        XRect EndDrawRowTemplate(int index);
        void BeginIterationTemplate(int rowCount);
        void EndIterationTemplate(double drawHeight);
        void SetOffsetY(double offsetY);
        void ResetOffset();
    }
}