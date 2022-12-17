

using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.BarCodes;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Xml.Linq;

namespace PdfSharpDslCore.Drawing
{
    public class PdfDocumentDrawer : IDisposable, IPdfDocumentDrawer
    {
        PdfDocument _document;
        PdfPage? _currentPage;
        XPen? _currentPen;
        XBrush? _currentBrush;
        XBrush? _highlightBrush;

        XFont? _currentFont;
        XGraphics? _gfx;
        private bool disposedValue;
        XPoint _currentPoint = new XPoint(0, 0);
        PageSize _defaultPageSize = PageSize.A4;
        PageOrientation _defaultPageOrientation = PageOrientation.Portrait;

        public PdfDocumentDrawer(PdfDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            _document = document;
        }
        #region properties
        public PdfPage CurrentPage
        {
            get
            {
                if (_currentPage is null)
                {
                    _currentPage = _document.AddPage();
                    _currentPage.Size = _defaultPageSize;
                    _currentPage.Orientation = _defaultPageOrientation;
                }
                return _currentPage;
            }
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    _gfx?.Dispose();
                    _gfx = null;
                }
            }
        }

        public XPen CurrentPen
        {
            get
            {
                if (_currentPen is null)
                {
                    _currentPen = XPens.Black;
                }
                return _currentPen;
            }
            set
            {
                _currentPen = value;
            }
        }

        public XBrush CurrentBrush
        {
            get
            {
                if (_currentBrush is null)
                {
                    _currentBrush = XBrushes.Black;
                }
                return _currentBrush;
            }
            set
            {
                _currentBrush = value;
            }
        }

        public XBrush? HighlightBrush
        {
            get
            {
                return _highlightBrush;
            }
            set
            {
                _highlightBrush = value;
            }
        }

        public XFont CurrentFont
        {
            get
            {
                if (_currentFont is null)
                {
                    _currentFont = new XFont("Arial", 10);
                }
                return _currentFont;
            }
            set
            {
                _currentFont = value;
            }
        }

        public double PageWidth => CurrentPage.Width;
        public double PageHeight => CurrentPage.Height;


        private XGraphics Gfx
        {
            get
            {
                if (_gfx is null)
                {
                    _gfx = XGraphics.FromPdfPage(CurrentPage);
                }
                return _gfx;
            }
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (!(_gfx is null))
                        _gfx.Dispose();
                }
                _gfx = null;
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private (double, double, double, double) CoordRectToPage(double x, double y, double w, double h)
        {
            var page = CurrentPage;
            if (x < 0)
            {
                x = page.Width + x;
            }
            if (y < 0)
            {
                y = page.Height + y;
            }
            if (w < 0)
            {
                w = page.Width + w - x;
            }
            if (h < 0)
            {
                h = page.Height + h - y;
            }
            return (x, y, w, h);
        }

        private (double, double, double?, double?) CoordRectToPage(double x, double y, double? w, double? h)
        {
            var page = CurrentPage;
            if (x < 0)
            {
                x = page.Width + x;
            }
            if (y < 0)
            {
                y = page.Height + y;
            }
            if (!(w is null))
            {
                if (w < 0)
                {
                    w = page.Width + w - x;
                }
            }
            if (!(h is null))
            {
                if (h < 0)
                {
                    h = page.Height + h - y;
                }
            }
            return (x, y, w, h);
        }

        public void DrawLine(double x, double y, double x1, double y1)
        {
            var page = CurrentPage;
            if (x < 0)
            {
                x = page.Width + x;
            }
            if (y < 0)
            {
                y = page.Height + y;
            }
            if (x1 < 0)
            {
                x1 = page.Width + x1;
            }
            if (y1 < 0)
            {
                y1 = page.Height + y1;
            }
            Gfx.DrawLine(CurrentPen, x, y, x1, y1);
        }

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
        {
            (x, y, w, h) = CoordRectToPage(x, y, w, h);
            if (isFilled)
            {
                Gfx.DrawRectangle(CurrentPen, CurrentBrush, x, y, w, h);
            }
            else
            {
                Gfx.DrawRectangle(CurrentPen, x, y, w, h);
            }
        }

        public void DrawEllipse(double x, double y, double w, double h, bool isFilled)
        {
            (x, y, w, h) = CoordRectToPage(x, y, w, h);
            var r = new XRect(x, y, w, h);
            if (isFilled)
            {
                Gfx.DrawEllipse(CurrentPen, CurrentBrush, r);
            }
            else
            {
                Gfx.DrawEllipse(CurrentPen, r);
            }

        }

        public void DrawText(string text, double x, double y, double? w, double? h/*, XStringAlignment hAlign, XLineAlignment vAlign*/)
        {
            var page = CurrentPage;
            (x, y, w, h) = CoordRectToPage(x, y, w, h);
            //20221009 : only top left is supported
            //var fmt = new XStringFormat
            //{
            //    Alignment = hAlign,
            //    LineAlignment = vAlign,
            //};
            if (w == null || h == null)
            {
                //because of missing w/h alignment is Left
                var sizeFormatter = new XTextSegmentFormatter(Gfx);
                sizeFormatter.Alignment = XParagraphAlignment.Left;
                var size = sizeFormatter.CalculateTextSize(text, CurrentFont, CurrentBrush, page.Width - x);
                var r = new XRect(x, y, size.Width, size.Height);
                var formatter = new XTextFormatter(Gfx);
                formatter.DrawString(text, CurrentFont, CurrentBrush, r);
#if DEBUG1
                Gfx.DrawRectangle(XPens.Red, r);
#endif
            }
            else
            {
                //fmt is not used, beacause DrawSTring support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                var formatter = new XTextFormatter(Gfx);
                formatter.DrawString(text, CurrentFont, CurrentBrush, r);
#if DEBUG1
                Gfx.DrawRectangle(XPens.Red, r);
#endif
            }
        }

        public void DrawLineText(string text, double x, double y, double? w, double? h, XStringAlignment hAlign, XLineAlignment vAlign, TextOrientation textOrientation)
        {
            (x, y, w, h) = CoordRectToPage(x, y, w, h);
            var fmt = new XStringFormat
            {
                Alignment = hAlign,
                LineAlignment = vAlign,
            };
            //TODO : optimize to do this only on vertical text
            var cnt = Gfx.BeginContainer();
            try
            {
                InternalDrawString(text, x, y, w, h, fmt, textOrientation, CurrentFont, CurrentBrush);
            }
            finally
            {
                Gfx.EndContainer(cnt);
            }
        }

        private void InternalDrawString(string text, double x, double y, double? w, double? h,
            XStringFormat fmt, TextOrientation textOrientation, XFont font, XBrush brush)
        {
            double angle = 0;

            if (!(textOrientation.Angle is null))
            {
                angle = textOrientation.Angle.Value;
            }
            else if (textOrientation.Orientation == TextOrientationEnum.Vertical)
            {
                angle = 90;
            }

            if (angle != 0)
            {
                Gfx.RotateAtTransform(angle, new XPoint(x, y));
            }
            var textSize = Gfx.MeasureString(text, CurrentFont);
            if (w == null || h == null)
            {
                Gfx.DrawString(text, CurrentFont, CurrentBrush, x, y, fmt);

                if (_highlightBrush != null)
                {
                    var r = RectFromStringFormat(x, y, textSize, fmt);
                    //
                    //var highlightColor = XColor.FromArgb(50, 255, 233, 178);
                    //var b = new XSolidBrush(highlightColor);
                    Gfx.DrawRectangle(_highlightBrush, r);
                }
            }
            else
            {
                //fmt is not used, because DrawString support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                Gfx.DrawString(text, CurrentFont, CurrentBrush, r, fmt);
                if (_highlightBrush != null)
                {
                    var hr = RectFromStringFormat(x, y, textSize, fmt);
                    hr.Intersect(r);
                    //var highlightColor = XColor.FromArgb(50, 255, 233, 178);
                    //var b = new XSolidBrush(highlightColor);
                    Gfx.DrawRectangle(_highlightBrush, hr);
                }
            }
        }

        private XRect RectFromStringFormat(XRect r, XSize textSize, XStringFormat fmt)
        {
            var result = new XRect(r.TopLeft, textSize);
            
            switch (fmt.Alignment)
            {
                case XStringAlignment.Center:
                    result.Offset((r.Width - textSize.Width)/2, 0);
                    break;
                case XStringAlignment.Near:
                    break;
                case XStringAlignment.Far:
                    result.Offset(r.Right-textSize.Width, 0);
                    break;
            }
            switch (fmt.LineAlignment)
            {
                case XLineAlignment.Center:
                    result.Offset(0,(r.Height - textSize.Height) / 2);
                    break;
                case XLineAlignment.Near:
                    break;
                case XLineAlignment.Far:
                    result.Offset(0, r.Bottom - textSize.Height);
                    break;
            }
            //to crop if text is larger/higher than provided rectangle
            result.Intersect(r);
            return result;
        }

        private XRect RectFromStringFormat(double x, double y, XSize textSize, XStringFormat fmt)
        {
            var result = new XRect(x, y, textSize.Width, textSize.Height);

            var xOffset = 0.0;
            var yOffset = 0.0;
            switch (fmt.Alignment)
            {
                case XStringAlignment.Center:
                    xOffset -= textSize.Width / 2;
                    break;
                case XStringAlignment.Near:
                    break;
                case XStringAlignment.Far:
                    xOffset -= textSize.Width;
                    break;
            }
            switch (fmt.LineAlignment)
            {
                case XLineAlignment.Center:
                    yOffset -= textSize.Height / 2;
                    break;
                case XLineAlignment.Near:
                    break;
                case XLineAlignment.Far:
                    yOffset -= textSize.Height;
                    break;
            }
            result.Offset(xOffset, yOffset);

            return result;
        }

        public void DrawTitle(string text, double margin, XStringAlignment hAlign, XLineAlignment vAlign)
        {
            var page = CurrentPage;

            var fmt = new XStringFormat
            {
                Alignment = hAlign,
                LineAlignment = vAlign,
            };

            var textSize = Gfx.MeasureString(text, CurrentFont, XStringFormats.TopLeft);
            if (margin < 0)
            {
                margin = page.Height - textSize.Height + margin;
            }
            var r = new XRect(0, margin, page.Width, textSize.Height);
            Gfx.DrawString(text, CurrentFont, CurrentBrush, r, fmt);
            if (_highlightBrush != null)
            {
                var hr = RectFromStringFormat(r, textSize, fmt);
                Gfx.DrawRectangle(_highlightBrush, hr);
            }
        }

        public void DrawTable(double x, double y, TableDefinition tblDef)
        {
            Gfx.Save();
            try
            {
                var maxHeightForTable = CurrentPage.Height - y;
                //todo check if the current page can receive 
                //calculate table dimension
                var defaultFont = CurrentFont;
                var defaultBrush = CurrentBrush;

                XFont[] xFonts = new XFont[tblDef.Columns.Count];
                bool[] colMeasure = new bool[tblDef.Columns.Count];
                bool calcHeaderHeight = tblDef.HeaderHeight is null;
                var margins = tblDef.CellMargin;
                int i = 0;
                foreach (var column in tblDef.Columns)
                {
                    xFonts[i] = column.Font ?? defaultFont;
                    colMeasure[i] = column.DesiredWidth is null;
                    if (colMeasure[i] || calcHeaderHeight)
                    {
                        var measure = Gfx.MeasureString(column.ColumnHeaderName, xFonts[i]);
                        if (colMeasure[i])
                        {
                            column.DesiredWidth = measure.Width + margins.Left + margins.Right;
                        }
                        if (calcHeaderHeight)
                        {
                            tblDef.HeaderHeight = Math.Max(tblDef.HeaderHeight ?? 0, measure.Height + margins.Top + margins.Bottom);
                        }
                    }
                    i++;
                }
                //measure all rows
                foreach (var row in tblDef.Rows)
                {
                    if (row.DesiredHeight is null)
                    {
                        for (i = 0; i < row.Data.Length; i++)
                        {
                            var measure = Gfx.MeasureString(row.Data[i], xFonts[i]);
                            row.DesiredHeight = Math.Max(row.DesiredHeight ?? 0, measure.Height + margins.Top + margins.Bottom);
                            if (colMeasure[i])
                            {
                                var column = tblDef.Columns[i];
                                column.DesiredWidth = Math.Max(column.DesiredWidth ?? 0, measure.Width + margins.Left + margins.Right);
                            }
                        }
                    }
                }

                //draw header
                double offsetX = 0;
                double offsetY = 0;
                i = 0;
                if (y + tblDef.HeaderHeight > CurrentPage.Height)
                {
                    Gfx.Restore();
                    NewPage();
                    Gfx.Save();
                    //TODO: set top margin
                    y = 1;
                }
                foreach (var column in tblDef.Columns)
                {
                    var r = new XRect(offsetX + x, y, column.DesiredWidth ?? 0, tblDef.HeaderHeight ?? 0);

                    Gfx.DrawRectangle(CurrentPen, tblDef.HeaderBackColor, r);
                    offsetX += column.DesiredWidth ?? 0;
                    //todo: alignment
                    var fmt = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                    DrawStringMultiline(column.ColumnHeaderName, xFonts[i], column.Brush ?? defaultBrush, r, fmt);
                    i++;
                }
                offsetY = tblDef.HeaderHeight ?? 0;
                //draw body
                foreach (var row in tblDef.Rows)
                {
                    if (y + offsetY + row.DesiredHeight > CurrentPage.Height)
                    {
                        Gfx.Restore();
                        NewPage();
                        Gfx.Save();
                        //TODO: set top margin
                        y = 1;
                        offsetY = 0;
                    }
                    offsetX = 0;
                    for (i = 0; i < row.Data.Length; i++)
                    {
                        var w = tblDef.Columns[i].DesiredWidth ?? 0;
                        var h = row.DesiredHeight ?? 0;
                        var r = new XRect(offsetX + x, offsetY + y, w, h);
                        Gfx.DrawRectangle(CurrentPen, tblDef.Columns[i].BackColor, r);
                        var hMargin = margins.Left + margins.Right;
                        var vMargin = margins.Top + margins.Bottom;
                        var rText = new XRect(offsetX + x + margins.Left, offsetY + y + margins.Top, w - hMargin, h - vMargin);
                        var fmt = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                        //to debug
                        //Gfx.DrawRectangle(XPens.Violet, rText);
                        //TODO: split to draw one string per line
                        DrawStringMultiline(row.Data[i], xFonts[i], tblDef.Columns[i].Brush ?? defaultBrush, rText, fmt);
                        offsetX += w;
                    }
                    offsetY += row.DesiredHeight ?? 0;
                }
            }
            finally
            {
                Gfx.Restore();
            }

        }

        private void DrawStringMultiline(string text, XFont xFont, XBrush xBrush, XRect r, XStringFormat fmt)
        {
            var splitted = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            splitted = splitted.SelectMany(x => x.Split('\r', '\n')).ToArray();
            var h = r.Height / splitted.Length;
            var offsetY = 0.0;
            foreach (var s in splitted)
            {
                Gfx.DrawString(s, xFont, xBrush, new XRect(r.Left, r.Top + offsetY, r.Width, h), fmt);
                offsetY += h;
            }

        }

        public void SetViewSize(double w, double h)
        {
            var scaleX = CurrentPage.Width / w;
            var scaleY = CurrentPage.Height / h;

            Gfx.ScaleTransform(scaleX, scaleY);
        }

        public void NewPage(PageSize? pageSize = null, PageOrientation? pageOrientation = null)
        {
            _defaultPageSize = pageSize ?? _defaultPageSize;
            _defaultPageOrientation = pageOrientation ?? _defaultPageOrientation;
            CurrentPage = AddPage();
        }

        private PdfPage AddPage()
        {
            var page = _document.AddPage();
            page.Size = _defaultPageSize;
            page.Orientation = _defaultPageOrientation;
            return page;
        }

        public void MoveTo(double x, double y)
        {
            _currentPoint = new XPoint(x, y);
        }

        public void LineTo(double x, double y)
        {
            var endPoint = new XPoint(x, y);
            Gfx.DrawLine(CurrentPen, _currentPoint, endPoint);
            _currentPoint = endPoint;
        }

        public void DrawImage(XImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage)
        {
            if (sizeInPixel)
            {
                //convert Pixel to Point
                if (w is not null)
                {
                    w = (double)(w * 72) / 96.0;
                }
                if (h is not null)
                {
                    h = (double)(h * 72) / 96.0;
                }
            }
            //fix coord if < 0 
            (x, y, w, h) = CoordRectToPage(x, y, w, h);
            if (w is null && h is null)
            {
                Gfx.DrawImage(image, x, y);
            }
            else
            {
                if (w is null)
                {
                    w = image.PointWidth;
                }
                if (h is null)
                {
                    h = image.PointHeight;
                }
                if (cropImage)
                {
                    //draw in form, then draw form in page
                    using XForm form = new XForm(this._document, XUnit.FromPoint(w.Value), XUnit.FromPoint(h.Value));
                    using var gr = XGraphics.FromForm(form);
                    gr.DrawImage(image, 0, 0);
                    Gfx.DrawImage(form, x, y);
                }
                else
                {
                    Gfx.DrawImage(image, x, y, w.Value, h.Value);
                }
            }
        }

        public void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle, bool isFilled)
        {
            if (isFilled)
            {
                Gfx.DrawPie(CurrentPen, CurrentBrush, x, y, w ?? 0, h ?? 0, startAngle, sweepAngle);
            }
            else
            {
                Gfx.DrawPie(CurrentPen, x, y, w ?? 0, h ?? 0, startAngle, sweepAngle);
            }
        }

        public void DrawPolygon(IEnumerable<XPoint> points, bool isFilled)
        {
            if (isFilled)
            {
                Gfx.DrawPolygon(CurrentPen, CurrentBrush, points.ToArray(), XFillMode.Alternate);
            }
            else
            {
                Gfx.DrawPolygon(CurrentPen, points.ToArray());
            }
        }

    }
}