

using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.BarCodes;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;

namespace Pdf
{
    public class PdfDocumentDrawer : IDisposable
    {
        PdfDocument _document;
        PdfPage? _currentPage;
        XPen? _currentPen;
        XBrush? _currentBrush;

        XFont? _currentFont;
        XGraphics? _gfx;
        private bool disposedValue;

        public PdfDocumentDrawer(PdfDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }
            _document = document;
        }

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

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
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
            if (isFilled)
            {
                Gfx.DrawRectangle(CurrentPen, CurrentBrush, x, y, w, h);
            }
            else
            {
                Gfx.DrawRectangle(CurrentPen, x, y, w, h);
            }
        }

        public void DrawText(string text, double x, double y, double? w, double? h/*, XStringAlignment hAlign, XLineAlignment vAlign*/)
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
                var size = sizeFormatter.CalculateTextSize(text, CurrentFont, CurrentBrush, page.Width-x);
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

        public void DrawLineText(string text, double x, double y, double? w, double? h, XStringAlignment hAlign, XLineAlignment vAlign)
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
            var fmt = new XStringFormat
            {
                Alignment = hAlign,
                LineAlignment = vAlign,
            };
            if (w == null || h == null)
            {
                Gfx.DrawString(text, CurrentFont, CurrentBrush, x,y, fmt);
            }
            else
            {
                //fmt is not used, because DrawSTring support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                Gfx.DrawString(text, CurrentFont, CurrentBrush, r, fmt);
            }
        }
        public void DrawNumber(string numberAsString, double rightAnchor, double centerY)
        {

            var page = CurrentPage;
            if (rightAnchor < 0)
            {
                rightAnchor = page.Width + rightAnchor;
            }
            if (centerY < 0)
            {
                centerY = page.Height + centerY;
            }
            var fmt = new XStringFormat
            {
                Alignment = XStringAlignment.Far,
                LineAlignment = XLineAlignment.Center
            };
            Gfx.DrawString(numberAsString, CurrentFont, CurrentBrush, rightAnchor, centerY, fmt);
            //IF debug
            // var measure = Gfx.MeasureString(numberAsString, CurrentFont, fmt);
            // Gfx.DrawRectangle(XPens.Red, rightAnchor - measure.Width, centerY - measure.Height / 2, measure.Width, measure.Height);
        }


        public void DrawSample()
        {
            var gfx = Gfx;
            //not working
            //
            //var dm = new CodeDataMatrix("0123456789", 12, 26);
            //dm.Direction = CodeDirection.TopToBottom;
            //dm.Size = new XSize(XUnit.FromMillimeter(14), XUnit.FromMillimeter(7));
            //gfx.DrawMatrixCode(dm, XBrushes.DarkBlue, new XPoint(300, 300));
            
            //yes
            //gfx.Save();
            //gfx.IntersectClip(new XRect(100, 200, 300, 500));            
            //gfx.DrawRectangle(XBrushes.LightBlue, 0, 0, gfx.PageSize.Width, gfx.PageSize.Height);
            //gfx.DrawLine(XPens.DarkRed, 0, 0, 150, 250);
            //gfx.Restore();
        }
        public void DrawTable(IEnumerable<object[]> rows, double x, double y, TableDefinition tblDef)
        {
           
            //columns widths ?
            //row height
            var gfx = Gfx;
            //gfx.DrawMatrixCode()
            foreach (var row in rows)
            {
                var startX = x;
                double maxHeight = 0;
                for (int i = 0; i < row.Length; i++)
                {

                    var cell = row[i];
                    if (cell is not null)
                    {
                        var text = cell.ToString();
                        var measure = gfx.MeasureString(text, CurrentFont);
                        maxHeight = Math.Max(maxHeight, measure.Height);
                        XRect rect = new XRect(startX, y, tblDef.ColWidth(i), maxHeight);
                        gfx.DrawString(text, CurrentFont, CurrentBrush, rect, new XStringFormat()
                        {
                            Alignment = tblDef.Alignment(i),
                            LineAlignment = XLineAlignment.Near
                        });
                    }
                    gfx.DrawLine(CurrentPen, startX, y, startX, y + maxHeight);
                    startX += tblDef.ColWidth(i);
                }
                gfx.DrawRectangle(CurrentPen, x, y, startX, maxHeight);
                y += maxHeight;
            }
        }
    }
}