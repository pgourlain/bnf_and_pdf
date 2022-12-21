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
        public XPen CurrentPen { get => throw new NotImplementedException();
            set
            {
                var penvName = $"pen{vNameIndex++}";
                _code.AppendLine($"var {penvName} = new XPen({value.Color}, {value.Width});");
                _code.AppendLine($"pen.DashStyle = XDashStyle.{value.DashStyle};");
                _code.AppendLine($"{_drawerPrefix}.CurrentPen = {penvName};");
            }
        }
        public XBrush CurrentBrush { get => throw new NotImplementedException(); set { } }
        public XBrush HighlightBrush { get => throw new NotImplementedException(); set { } }
        public XFont CurrentFont { get => throw new NotImplementedException(); set { } }

        public double PageWidth => 100;//throw new NotImplementedException();

        public double PageHeight => 100;//throw new NotImplementedException();

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
            throw new NotImplementedException();
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
    }
}
