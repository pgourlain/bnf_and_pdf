

using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.BarCodes;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using System.Data.Common;
using System.Runtime.Intrinsics.X86;

namespace Pdf
{
    public class PdfDocumentDrawer : IDisposable, IPdfDocumentDrawer
    {
        PdfDocument _document;
        PdfPage? _currentPage;
        XPen? _currentPen;
        XBrush? _currentBrush;

        XFont? _currentFont;
        XGraphics? _gfx;
        private bool disposedValue;
        XPoint _currentPoint = new XPoint(0, 0);

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
                    _currentPage.Size = PdfSharpCore.PageSize.A4;
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
                    if (_gfx is not null)
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
            if (w is not null)
            {
                if (w < 0)
                {
                    w = page.Width + w - x;
                }
            }
            if (h is not null)
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
            Gfx.DrawLine(this.CurrentPen, x, y, x1, y1);
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
                InternalDrawString(text, x, y, w, h, fmt, textOrientation, this.CurrentFont, this.CurrentBrush);
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
            if (w == null || h == null)
            {
                Gfx.DrawString(text, CurrentFont, CurrentBrush, x, y, fmt);
            }
            else
            {
                //fmt is not used, because DrawSTring support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                Gfx.DrawString(text, CurrentFont, CurrentBrush, r, fmt);
            }
        }

        public void DrawTitle(string text, double margin, XStringAlignment hAlign, XLineAlignment vAlign)
        {
            var page = CurrentPage;

            var fmt = new XStringFormat
            {
                Alignment = hAlign,
                LineAlignment = vAlign,
            };

            var textSize = Gfx.MeasureString(text, this.CurrentFont, XStringFormats.TopLeft);
            if (margin < 0)
            {
                margin = (page.Height - textSize.Height) + margin;
            }
            var r = new XRect(0, margin, page.Width, textSize.Height);
            Gfx.DrawString(text, CurrentFont, CurrentBrush, r, fmt);
            //if debug
            //Gfx.DrawRectangle(XPens.Red, r);
        }
        //public void DrawNumber(string numberAsString, double rightAnchor, double centerY)
        //{

        //    var page = CurrentPage;
        //    if (rightAnchor < 0)
        //    {
        //        rightAnchor = page.Width + rightAnchor;
        //    }
        //    if (centerY < 0)
        //    {
        //        centerY = page.Height + centerY;
        //    }
        //    var fmt = new XStringFormat
        //    {
        //        Alignment = XStringAlignment.Far,
        //        LineAlignment = XLineAlignment.Center
        //    };
        //    Gfx.DrawString(numberAsString, CurrentFont, CurrentBrush, rightAnchor, centerY, fmt);
        //    //IF debug
        //    // var measure = Gfx.MeasureString(numberAsString, CurrentFont, fmt);
        //    // Gfx.DrawRectangle(XPens.Red, rightAnchor - measure.Width, centerY - measure.Height / 2, measure.Width, measure.Height);
        //}

        public void DrawTable(double x, double y, TableDefinition tblDef)
        {
            Gfx.Save();
            try
            {
                var maxHeightForTable = this.CurrentPage.Height - y;
                //todo check if the current page can receive 
                //calculate table dimension
                var defaultFont = this.CurrentFont;
                var defaultBrush = this.CurrentBrush;

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

                    Gfx.DrawRectangle(this.CurrentPen, tblDef.HeaderBackColor, r);
                    offsetX += column.DesiredWidth ?? 0;
                    //todo: alignment
                    var fmt = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                    Gfx.DrawString(column.ColumnHeaderName, xFonts[i], column.Brush ?? defaultBrush, r, fmt);
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
                        Gfx.DrawRectangle(this.CurrentPen, tblDef.Columns[i].BackColor, r);
                        var hMargin = margins.Left + margins.Right;
                        var vMargin = margins.Top + margins.Bottom;
                        var rText = new XRect(offsetX + x + margins.Left, offsetY + y + margins.Top, w-hMargin , h-vMargin);
                        var fmt = new XStringFormat { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
                        //to debug
                        //Gfx.DrawRectangle(XPens.Violet, rText);
                        //TODO: split to draw one string per line
                        Gfx.DrawString(row.Data[i], xFonts[i], tblDef.Columns[i].Brush ?? defaultBrush, rText, fmt);
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

        public void SetViewSize(double w, double h)
        {
            var scaleX = CurrentPage.Width / w;
            var scaleY = CurrentPage.Height / h;

            Gfx.ScaleTransform(scaleX, scaleY);
        }

        public void NewPage()
        {            
            this.CurrentPage = _document.AddPage();
        }

        public void MoveTo(double x, double y)
        {
            this._currentPoint = new XPoint(x, y);
            //var sqrt3 = Math.Sqrt(3);
            //var T = 100;
            //_MoveTo(x - T / 2, y - T * sqrt3 / 2);
            //LineTo(x + T / 2, y - T * sqrt3 / 2);
            //LineTo(x + T, y + 0);
            //LineTo(x + T / 2, y + T * sqrt3 / 2);
            //LineTo(x - T / 2, y + T * sqrt3 / 2);
            //LineTo(x + -T, y + 0);
            //LineTo(x - T / 2, y - T * sqrt3 / 2);
        }

        private void _MoveTo(double x, double y)
        {
            this._currentPoint = new XPoint(x, y);
        }
        public void LineTo(double x, double y)
        {
            var endPoint = new XPoint(x, y);
            Gfx.DrawLine(this.CurrentPen, this._currentPoint, endPoint);
            _currentPoint = endPoint;
        }
    }
}