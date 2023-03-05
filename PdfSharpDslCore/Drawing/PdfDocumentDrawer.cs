using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PdfSharpDslCore.Drawing
{
    public sealed class PdfDocumentDrawer : IDisposable, IPdfDocumentDrawer
    {
        private readonly PdfDocument _document;
        private PdfPage? _currentPage;
        private XPen? _currentPen;
        private XBrush? _currentBrush;

        private XFont? _currentFont;
        private XGraphics? _gfx;
        private IXGraphicsRenderer? _gfxRenderer;
        private bool _disposedValue;
        private XPoint _currentPoint = new XPoint(0, 0);
        private PageSize _defaultPageSize = PageSize.A4;
        private PageOrientation _defaultPageOrientation = PageOrientation.Portrait;

        private readonly DrawingContext _drawingContext = new();

        public PdfDocumentDrawer(PdfDocument document)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
        }

        #region properties

        public PdfPage CurrentPage
        {
            get
            {
                if (_currentPage is not null) return _currentPage;
                _currentPage = _document.AddPage();
                _currentPage.Size = _defaultPageSize;
                _currentPage.Orientation = _defaultPageOrientation;

                return _currentPage;
            }
            set
            {
                if (_currentPage == value) return;
                _currentPage = value;
                _gfx?.Dispose();
                _gfx = null;
                _gfxRenderer = null;
            }
        }

        public XPen CurrentPen
        {
            get => _currentPen ??= XPens.Black;
            set => _currentPen = value;
        }

        public XBrush CurrentBrush
        {
            get => _currentBrush ??= XBrushes.Black;
            set => _currentBrush = value;
        }

        public XBrush? HighlightBrush { get; set; }

        public XFont CurrentFont
        {
            get => _currentFont ??= new XFont("Arial", 10);
            set => _currentFont = value;
        }

        public double PageWidth => CurrentPage.Width;
        public double PageHeight => CurrentPage.Height;


        private XGraphics Gfx
        {
            get
            {
                if (_gfx is not null) return _gfx;
                _gfx = XGraphics.FromPdfPage(CurrentPage);
                // HACK, read from https://github.com/ststeiger/PdfSharpCore/blob/master/docs/MigraDocCore/samples/MixMigraDocCoreAndPDFsharpCore.md
                _gfx.MUH = PdfFontEncoding.Unicode;

                return _gfx;
            }
        }

        #endregion

        private void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
                _gfx?.Dispose();
            }

            _gfx = null;
            _gfxRenderer = null;
            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposedValue = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
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
            this._drawingContext.UpdateDrawingRect(new XPoint(x, y), new XPoint(x1, y1));
        }

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
        {
            (x, y, w, h) = CurrentPage.CoordRectToPage(x, y, w, h);
            this._drawingContext.UpdateDrawingRect(x, y, w, h);
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
            (x, y, w, h) = CurrentPage.CoordRectToPage(x, y, w, h);
            var r = new XRect(x, y, w, h);
            this._drawingContext.UpdateDrawingRect(r);
            if (isFilled)
            {
                Gfx.DrawEllipse(CurrentPen, CurrentBrush, r);
            }
            else
            {
                Gfx.DrawEllipse(CurrentPen, r);
            }
        }

        public void DrawText(string text, double x, double y, double? w,
            double? h /*, XStringAlignment hAlign, XLineAlignment vAlign*/)
        {
            var page = CurrentPage;
            (x, y, w, h) = page.CoordRectToPage(x, y, w, h);
            //20221009 : only top left is supported
            //var fmt = new XStringFormat
            //{
            //    Alignment = hAlign,
            //    LineAlignment = vAlign,
            //};
            if (w == null || h == null)
            {
                //because of missing w/h alignment is Left
                var sizeFormatter = new XTextSegmentFormatter(Gfx) {
                    Alignment = XParagraphAlignment.Left
                };
                w = w ?? page.Width - x;
                var size = sizeFormatter.CalculateTextSize(text, CurrentFont, CurrentBrush, w.Value);
                var r = new XRect(x, y, size.Width, size.Height);

                sizeFormatter.DrawString(text, CurrentFont, CurrentBrush, r);
                //var formatter = new XTextFormatter(Gfx);
                //formatter.DrawString(text, CurrentFont, CurrentBrush, r);
                this._drawingContext.UpdateDrawingRect(r);
#if DEBUG1
                Gfx.DrawRectangle(XPens.Red, r);
#endif
            }
            else
            {
                //fmt is not used, because DrawString support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                var formatter = new XTextFormatter(Gfx);
                formatter.DrawString(text, CurrentFont, CurrentBrush, r);
                this._drawingContext.UpdateDrawingRect(r);
#if DEBUG1
                Gfx.DrawRectangle(XPens.Red, r);
#endif
            }
        }

        public void DrawLineText(string text, double x, double y, double? w, double? h, XStringAlignment hAlign,
            XLineAlignment vAlign, TextOrientation textOrientation)
        {
            (x, y, w, h) = CurrentPage.CoordRectToPage(x, y, w, h);
            var fmt = new XStringFormat {
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

            if (textOrientation.Angle is not null)
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
                this._drawingContext.UpdateDrawingRect(x, y, textSize.Width, textSize.Height);
                if (HighlightBrush == null) return;

                var r = DrawingHelper.RectFromStringFormat(x, y, textSize, fmt);
                //
                //var highlightColor = XColor.FromArgb(50, 255, 233, 178);
                //var b = new XSolidBrush(highlightColor);
                Gfx.DrawRectangle(HighlightBrush, r);
            }
            else
            {
                //fmt is not used, because DrawString support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                Gfx.DrawString(text, CurrentFont, CurrentBrush, r, fmt);

                this._drawingContext.UpdateDrawingRect(r);
                if (HighlightBrush == null) return;
                var hr = DrawingHelper.RectFromStringFormat(x, y, textSize, fmt);
                hr.Intersect(r);
                //var highlightColor = XColor.FromArgb(50, 255, 233, 178);
                //var b = new XSolidBrush(highlightColor);
                Gfx.DrawRectangle(HighlightBrush, hr);
            }
        }

        public void DrawTitle(string text, double margin, XStringAlignment hAlign, XLineAlignment vAlign)
        {
            var page = CurrentPage;

            var fmt = new XStringFormat {
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
            this._drawingContext.UpdateDrawingRect(r);
            if (HighlightBrush == null) return;

            var hr = DrawingHelper.RectFromStringFormat(r, textSize, fmt);
            Gfx.DrawRectangle(HighlightBrush, hr);
        }

        public void DrawTable(double x, double y, TableDefinition tblDef)
        {
            var availableWidth = PageWidth - x;
            Gfx.Save();
            try
            {
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
                            tblDef.HeaderHeight = Math.Max(tblDef.HeaderHeight ?? 0,
                                measure.Height + margins.Top + margins.Bottom);
                        }
                    }

                    i++;
                }

                //measure all rows
                var sizeFormatter = new XTextSegmentFormatter(Gfx) {
                    Alignment = XParagraphAlignment.Left
                };
                foreach (var row in tblDef.Rows)
                {
                    var rowMeasure = row.DesiredHeight is null;
                    for (i = 0; i < row.Data.Length; i++)
                    {
                        var testSize = !colMeasure[i];
                        var pageSpaceLeft = tblDef.ColMaxWidth(i, availableWidth);
                        var column = tblDef.Columns[i];
                        if (colMeasure[i])
                        {
                            var cSize = Gfx.MeasureString(row.Data[i], xFonts[i]);
                            cSize.Width += (margins.Left + margins.Right);
                            if (cSize.Width > pageSpaceLeft)
                            {
                                //measure height with fixed width
                                testSize = true;
                                column.DesiredWidth = Math.Max(column.DesiredWidth ?? 0, pageSpaceLeft);
                            }
                            else
                            {
                                column.DesiredWidth = Math.Max(column.DesiredWidth ?? 0, cSize.Width);
                            }

                            if (rowMeasure)
                            {
                                row.DesiredHeight = Math.Max(row.DesiredHeight ?? 0,
                                    cSize.Height + margins.Top + margins.Bottom);
                            }
                        }

                        if (!testSize) continue;
                        var w = Math.Min(column.DesiredWidth ?? 0, pageSpaceLeft);
                        var measure = sizeFormatter.CalculateTextSize(row.Data[i], xFonts[i], defaultBrush, w);
                        if (rowMeasure)
                        {
                            row.DesiredHeight = Math.Max(row.DesiredHeight ?? 0, measure.Height + margins.Top + margins.Bottom);
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
                    var w = column.DrawWidth;
                    var h = tblDef.HeaderHeight ?? 0;

                    var r = new XRect(offsetX + x, y, w, h);
                    var hMargin = margins.Left + margins.Right;
                    var vMargin = margins.Top + margins.Bottom;
                    var rText = new XRect(offsetX + x + margins.Left, offsetY + y + margins.Top, w - hMargin,
                        h - vMargin);

                    Gfx.DrawRectangle(CurrentPen, tblDef.HeaderBackColor, r);
                    offsetX += column.DrawWidth;
                    //todo: alignment
                    var fmt = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                    DrawStringMultiline(column.ColumnHeaderName, xFonts[i], column.Brush ?? defaultBrush, rText, fmt);
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
                        var w = tblDef.Columns[i].DrawWidth;
                        var h = row.DesiredHeight ?? 0;
                        var r = new XRect(offsetX + x, offsetY + y, w, h);
                        ResetClip();
                        Gfx.DrawRectangle(CurrentPen, tblDef.Columns[i].BackColor, r);
                        var hMargin = margins.Left + margins.Right;
                        var vMargin = margins.Top + margins.Bottom;
                        var rText = new XRect(offsetX + x + margins.Left, offsetY + y + margins.Top, w - hMargin,
                            h - vMargin);
                        Gfx.IntersectClip(rText);
                        var fmt = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                        //to debug
                        //Gfx.DrawRectangle(XPens.Violet, rText);
                        //TODO: split to draw one string per line
                        DrawStringMultiline(row.Data[i], xFonts[i], tblDef.Columns[i].Brush ?? defaultBrush, rText,
                            fmt);
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

        private void ResetClip()
        {
            if (_gfx is null) return;
            _gfxRenderer ??= (IXGraphicsRenderer)_gfx.GetType().GetField("_renderer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                                                             | System.Reflection.BindingFlags.Instance)
                .GetValue(_gfx);
            _gfxRenderer.ResetClip();
        }

        private void DrawStringMultiline(string text, XFont xFont, XBrush xBrush, XRect r, XStringFormat fmt)
        {
            var formatter = new XTextFormatter(Gfx);
            formatter.DrawString(text, xFont, xBrush, r);
            this._drawingContext.UpdateDrawingRect(r);
            //var splitted = text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            //splitted = splitted.SelectMany(x => x.Split('\r', '\n')).ToArray();
            //var h = r.Height / splitted.Length;
            //var offsetY = 0.0;
            //foreach (var s in splitted)
            //{
            //    Gfx.DrawString(s, xFont, xBrush, new XRect(r.Left, r.Top + offsetY, r.Width, h), fmt);
            //    offsetY += h;
            //}
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
            this._drawingContext.UpdateDrawingRect(new XRect(_currentPoint, endPoint));
            _currentPoint = endPoint;
        }

        public void DrawImage(XImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage)
        {
            if (sizeInPixel)
            {
                //convert Pixel to Point
                w = w * 72 / 96.0;
                h = h * 72 / 96.0;
                /*
                if (h is not null)
                {
                    h = (double)(h * 72) / 96.0;
                }
                 */
            }

            //fix coord if < 0 
            (x, y, w, h) = CurrentPage.CoordRectToPage(x, y, w, h);
            if (w is null && h is null)
            {
                Gfx.DrawImage(image, x, y);
                this._drawingContext.UpdateDrawingRect(x, y, image.PointWidth, image.PointHeight);
            }
            else
            {
                w ??= image.PointWidth;
                h ??= image.PointHeight;
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
                this._drawingContext.UpdateDrawingRect(x, y, w, h);
            }
        }

        public void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle,
            bool isFilled)
        {
            this._drawingContext.UpdateDrawingRect(x, y, w, h);
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
            var ptArray = points.ToArray();
            this._drawingContext.UpdateDrawingRect(ptArray);
            if (isFilled)
            {
                Gfx.DrawPolygon(CurrentPen, CurrentBrush, ptArray, XFillMode.Alternate);
            }
            else
            {
                Gfx.DrawPolygon(CurrentPen, ptArray);
            }
        }

        public void BeginDrawRowTemplate(double offsetY)
        {
            this._drawingContext.PushDrawingRect(new XRect(0, 0, 0, 0));
            //Gfx.DrawLine(XPens.DarkViolet, new XPoint(0, offsetY), new XPoint(PageWidth, offsetY));
            Gfx.Save();
            Gfx.TranslateTransform(0, offsetY);
            //Gfx.DrawLine(XPens.DarkViolet, new XPoint(0, 0), new XPoint(PageWidth, 0));

        }

        public XRect EndDrawRowTemplate()
        {
            var result = this._drawingContext.PopDrawingRect(false);
            //var level = this.drawingContext.Level;
            //XPen pen = level switch {
            //    2 => XPens.PaleVioletRed,
            //    1 => XPens.BlueViolet,
            //    _ => XPens.Violet
            //};

            //Gfx.DrawRectangle(pen, result);
            Gfx.Restore();
            return result;
        }

        public void BeginIterationTemplate()
        {

        }

        public void EndIterationTemplate(double drawHeight)
        {
            this._drawingContext.UpdateDrawingRect(0, 0, 0, drawHeight);
        }

    }
}