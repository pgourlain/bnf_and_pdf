using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Generator.DrawingGenerator
{
    internal class CSharpDrawer : IPdfDocumentDrawer
    {
        StringBuilder _code;
        string _drawerPrefix;
        int vNameIndex = 1;
        public CSharpDrawer(StringBuilder code, string drawerPrefix)
        {
            _code = code;
            _drawerPrefix = drawerPrefix;
        }
        public XPen CurrentPen
        {
            get => throw new NotImplementedException("CurrentPen");
            set
            {
                var penvName = $"pen{vNameIndex++}";
                _code.AppendLine($"var {penvName} = new XPen({ColorToString(value.Color)}, {value.Width});");
                _code.AppendLine($"{penvName}.DashStyle = XDashStyle.{value.DashStyle};");
                _code.AppendLine($"{_drawerPrefix}CurrentPen = {penvName};");
            }
        }

        uint Argb(XColor color)
        {
            var _a = color.A;
            var _r = color.R;
            var _g = color.G;
            var _b = color.B;
            return ((uint)(_a * 255) << 24) | ((uint)_r << 16) | ((uint)_g << 8) | _b;
        }

        private string ColorToString(XColor color)
        {
            if (color.IsKnownColor)
            {
                return $"XColors.{XColorResourceManager.GetKnownColor(Argb(color))}";
            }
            else
            {
                return $"XColor.FromArgb({color.A},{color.R},{color.G},{color.B})";
            }
        }

        public XBrush CurrentBrush
        {
            get => throw new NotImplementedException("CurrentBrush");
            set { }
        }

        public XBrush HighlightBrush
        {
            get => throw new NotImplementedException();
            set { }
        }
        public XFont CurrentFont
        {
            get => throw new NotImplementedException("CurrentFont");
            set { }
        }

        public double PageWidth => 100;//throw new NotImplementedException();

        public double PageHeight => 100;//throw new NotImplementedException();
        public DebugOptions DebugOptions { get; set; }

        public void DrawEllipse(double x, double y, double w, double h, bool isFilled)
        {
            //throw new NotImplementedException();
        }

        public void DrawImage(XImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage)
        {
            //throw new NotImplementedException();
        }

        public void DrawLine(double x, double y, double x1, double y1)
        {
            //throw new NotImplementedException();
        }

        public void DrawLineText(string text, double x, double y, double? w, double? h, XStringAlignment hAlign, XLineAlignment vAlign, TextOrientation textOrientation)
        {
            //throw new NotImplementedException("DrawLineText");
        }

        public void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle, bool isFilled)
        {
            //throw new NotImplementedException();
        }

        public void DrawPolygon(IEnumerable<XPoint> points, bool isFilled)
        {
            //throw new NotImplementedException();
        }

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
        {
            //throw new NotImplementedException();
        }

        public void DrawTable(double x, double y, TableDefinition tblDef)
        {
            //throw new NotImplementedException();
        }

        public void DrawText(string text, double x, double y, double? w, double? h)
        {
            //throw new NotImplementedException();
        }

        public void DrawTitle(string text, double margin, XStringAlignment hAlign, XLineAlignment vAlign)
        {
            //throw new NotImplementedException();
        }

        public void LineTo(double x, double y)
        {
            //throw new NotImplementedException();
        }

        public void MoveTo(double x, double y)
        {
            //throw new NotImplementedException();
        }

        public void NewPage(PageSize? pageSize = null, PageOrientation? pageOrientation = null)
        {
            //throw new NotImplementedException();
        }

        public void SetViewSize(double w, double h)
        {
            //throw new NotImplementedException();
        }

        public void BeginDrawRowTemplate(int index, double offsetY, double newPageTopMargin)
        {
            //throw new NotImplementedException();
        }

        public DrawingResult EndDrawRowTemplate(int index)
        {
            //throw new NotImplementedException();
            return new DrawingResult();
        }

        public void BeginIterationTemplate(int rowCount)
        {
            //throw new NotImplementedException();
        }

        public void EndIterationTemplate(double drawHeight)
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
