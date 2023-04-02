using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using PdfSharpDslCore.Extensions;
using SixLabors.Fonts;

namespace PdfSharpDslCore.Drawing
{
    public sealed class PdfDocumentDrawer : IDisposable, IPdfDocumentDrawer
    {
        private readonly PdfDocument _document;
        private readonly ILogger? _logger;
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
        private readonly List<Action> _actionOnBeforeNewPage = new();
        private readonly List<Action> _actionOnAfterNewPage = new();

        private readonly DrawingContext _drawingCtx;

        private readonly XPen _debugPen = new XPen(XColors.Red, 0.5) { DashStyle = XDashStyle.DashDot };
        private readonly Lazy<XFont> _debugFont = new Lazy<XFont>(() => new XFont("monospace", 6));

        public PdfDocumentDrawer(PdfDocument document, ILogger? logger = null)
        {
            _drawingCtx = new(logger);
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _logger = logger;
        }

        #region properties

        public DebugOptions DebugOptions
        {
            get => _drawingCtx.DebugOptions;
            set => _drawingCtx.DebugOptions = value;
        }

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
                if (_logger.DebugEnabled())
                {
                    _logger.WriteDebug(this, $"Dispose GFX:{_gfx?.GetHashCode() ?? 0}");
                }
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
                if (_logger.DebugEnabled())
                {
                    _logger.WriteDebug(this, "Create new gfx from page");
                }
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
            InternalDrawLine(CurrentPen, x, y, x1, y1);
        }

        private void InternalDrawLine(XPen pen, double x, double y, double x1, double y1)
        {
            Gfx.DrawLine(pen, x, y, x1, y1);
            this._drawingCtx.PushInstruction((oy) => InternalDrawLine(pen, x, y+oy, x1, y1+oy), new XRect(new XPoint(x, y), new XPoint(x1, y1)));
        }

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
        {
            (x, y, w, h) = CurrentPage.CoordRectToPage(x, y, w, h);
            InternalDrawRect(CurrentPen, CurrentBrush, x, y, w, h, isFilled);
        }

        private void InternalDrawRect(XPen pen, XBrush brush, double x, double y, double w, double h, bool isFilled)
        {
            if (isFilled)
            {
                Gfx.DrawRectangle(pen, brush, x, y, w, h);
            }
            else
            {
                Gfx.DrawRectangle(pen, x, y, w, h);
            }
            this._drawingCtx.PushInstruction((oy) => InternalDrawRect(pen, brush, x, y+oy, w, h, isFilled), new XRect(x, y, w, h));
        }

        public void DrawEllipse(double x, double y, double w, double h, bool isFilled)
        {
            (x, y, w, h) = CurrentPage.CoordRectToPage(x, y, w, h);
            var r = new XRect(x, y, w, h);
            InternalDrawEllipse(isFilled, r, CurrentPen, CurrentBrush);
        }

        private void InternalDrawEllipse(bool isFilled, XRect r, XPen pen, XBrush brush)
        {
            if (isFilled)
            {
                Gfx.DrawEllipse(pen, brush, r);
            }
            else
            {
                Gfx.DrawEllipse(pen, r);
            }
            this._drawingCtx.PushInstruction((oy) => InternalDrawEllipse(isFilled, r.OffsetY(oy), pen, brush), r);
        }

        public void DrawText(string text, double x, double y, double? w,
            double? h)
        {
            var page = CurrentPage;
            (x, y, w, h) = page.CoordRectToPage(x, y, w, h);
            //20221009 : only top left is supported
            if (w == null || h == null)
            {
                //because of missing w/h alignment is Left
                var sizeFormatter = new XTextSegmentFormatter(Gfx)
                {
                    Alignment = XParagraphAlignment.Left
                };
                w = w ?? page.Width - x;
                var size = sizeFormatter.CalculateTextSize(text, CurrentFont, CurrentBrush, w.Value);
                var r = new XRect(x, y, size.Width, size.Height);

                sizeFormatter.DrawString(text, CurrentFont, CurrentBrush, r);
                this._drawingCtx.UpdateDrawingRect(r);
                if (_drawingCtx.DebugText)
                {
                    DebugRect(r);
                }
            }
            else
            {
                //fmt is not used, because DrawString support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                var formatter = new XTextFormatter(Gfx);
                formatter.DrawString(text, CurrentFont, CurrentBrush, r);
                this._drawingCtx.UpdateDrawingRect(r);
                if (_drawingCtx.DebugText)
                {
                    DebugRect(r);
                }
            }
        }

        public void DrawLineText(string text, double x, double y, double? w, double? h, XStringAlignment hAlign,
            XLineAlignment vAlign, TextOrientation textOrientation)
        {
            (x, y, w, h) = CurrentPage.CoordRectToPage(x, y, w, h);
            var fmt = new XStringFormat
            {
                Alignment = hAlign,
                LineAlignment = vAlign,
            };
            InternalDrawLineText(text, x, y, w, h, textOrientation, fmt, CurrentFont, CurrentBrush, HighlightBrush);
        }

        private void InternalDrawLineText(string text, double x, double y, double? w, double? h,
            TextOrientation textOrientation, XStringFormat fmt, XFont font, XBrush brush, XBrush? hb)
        {
            XRect r;
            //TODO : optimize to do this only on vertical text
            var cnt = Gfx.BeginContainer();
            try
            {
                r = InternalDrawString(text, x, y, w, h, fmt, textOrientation, font, brush, hb);
            }
            finally
            {
                Gfx.EndContainer(cnt);
            }

            this._drawingCtx.PushInstruction(
                (oy) => InternalDrawLineText(text, x, y+oy, w, h, textOrientation, fmt, font, brush, hb), r, instrName:$"DrawLineText({text})");
        }

        private XRect InternalDrawString(string text, double x, double y, double? w, double? h,
            XStringFormat fmt, TextOrientation textOrientation, XFont font, XBrush brush, XBrush? hb)
        {
            XRect result;
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

            var textSize = Gfx.MeasureString(text, font);
            if (w == null || h == null)
            {
                Gfx.DrawString(text, font, brush, x, y, fmt);
                var rText = new XRect(x, y, textSize.Width, textSize.Height);
                var r = DrawingHelper.RectFromStringFormat(x, y, textSize, fmt);
                result = r;
                if (_drawingCtx.DebugText)
                {
                    DebugRect(r);
                }

                if (hb == null) return result;


                //
                //var highlightColor = XColor.FromArgb(50, 255, 233, 178);
                //var b = new XSolidBrush(highlightColor);
                Gfx.DrawRectangle(hb, r);
            }
            else
            {
                //fmt is not used, because DrawString support only TopLeft
                var r = new XRect(x, y, w.Value, h.Value);
                Gfx.DrawString(text, font, brush, r, fmt);
                var hr = DrawingHelper.RectFromStringFormat(x, y, textSize, fmt);
                result = hr;
                if (_drawingCtx.DebugText)
                {
                    DebugRect(hr);
                }

                if (hb == null) return result;
                hr.Intersect(r);
                //var highlightColor = XColor.FromArgb(50, 255, 233, 178);
                //var b = new XSolidBrush(highlightColor);
                Gfx.DrawRectangle(hb, hr);
            }

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
            InternalDrawText(text, r, fmt, textSize, CurrentFont, CurrentBrush, HighlightBrush);
        }

        private void InternalDrawText(string text, XRect r, XStringFormat fmt, XSize textSize,
            XFont font, XBrush brush, XBrush? hb)
        {
            Gfx.DrawString(text, font, brush, r, fmt);
            this._drawingCtx.PushInstruction((oy) => InternalDrawText(text, r.OffsetY(oy), fmt, textSize, font, brush, hb), r);
            if (_drawingCtx.DebugText)
            {
                var debugRect = DrawingHelper.RectFromStringFormat(r, textSize, fmt);
                DebugRect(debugRect);
            }

            if (hb == null) return;

            var hr = DrawingHelper.RectFromStringFormat(r, textSize, fmt);
            Gfx.DrawRectangle(hb, hr);
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
                var sizeFormatter = new XTextSegmentFormatter(Gfx)
                {
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
                            row.DesiredHeight = Math.Max(row.DesiredHeight ?? 0,
                                measure.Height + margins.Top + margins.Bottom);
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
                    var fmt = new XStringFormat
                    { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
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
                        var fmt = new XStringFormat
                        { Alignment = XStringAlignment.Center, LineAlignment = XLineAlignment.Center };
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
            _gfxRenderer?.ResetClip();
        }

        private void DrawStringMultiline(string text, XFont xFont, XBrush xBrush, XRect r, XStringFormat fmt)
        {
            var formatter = new XTextFormatter(Gfx);
            formatter.DrawString(text, xFont, xBrush, r);
            this._drawingCtx.UpdateDrawingRect(r);
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

            _actionOnBeforeNewPage.ForEach(fn => fn());
            CurrentPage = AddPage();
            _actionOnAfterNewPage.ForEach(fn => fn());
        }

        private PdfPage AddPage()
        {
            if (_logger.DebugEnabled())
            {
                _logger.WriteDebug(this, $"AddPage: before={_actionOnBeforeNewPage.Count}, after={_actionOnAfterNewPage.Count}");
            }
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
            InternalLineTo(_currentPoint, endPoint, CurrentPen);
            _currentPoint = endPoint;
        }

        private void InternalLineTo(XPoint p1, XPoint p2, XPen pen)
        {
            Gfx.DrawLine(pen, p1, p2);
            this._drawingCtx.PushInstruction((oy) => InternalLineTo(p1.OffsetY(oy), p2.OffsetY(oy), pen), new XRect(p1, p2));
            //this._drawingCtx.UpdateDrawingRect(new XRect(p1, p2));
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
            InternalDrawImage(image, x, y, w, h, cropImage);
        }

        private void InternalDrawImage(XImage image, double x, double y, double? w, double? h, bool cropImage)
        {
            var ow = w;
            var oh = h;
            if (w is null && h is null)
            {
                Gfx.DrawImage(image, x, y);
                w = image.PointWidth;
                h = image.PointHeight;
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
            }
            this._drawingCtx.PushInstruction((oy) => InternalDrawImage(image, x, y+oy, ow, oh, cropImage), new XRect(x, y, w.Value, h.Value));
        }

        public void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle,
            bool isFilled)
        {
            InternalDrawPie(x, y, w, h, startAngle, sweepAngle, isFilled, CurrentPen, CurrentBrush);
        }

        private void InternalDrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle,
            bool isFilled, XPen pen, XBrush brush)
        {
            this._drawingCtx.UpdateDrawingRect(x, y, w, h);
            if (isFilled)
            {
                Gfx.DrawPie(pen, brush, x, y, w ?? 0, h ?? 0, startAngle, sweepAngle);
            }
            else
            {
                Gfx.DrawPie(pen, x, y, w ?? 0, h ?? 0, startAngle, sweepAngle);
            }
            this._drawingCtx.PushInstruction((oy) => InternalDrawPie(x, y+oy, w, h, startAngle, sweepAngle, isFilled, pen, brush),
                new XRect(x, y, w ?? 0, h ?? 0));
        }

        public void DrawPolygon(IEnumerable<XPoint> points, bool isFilled)
        {
            var ptArray = points.ToArray();
            InternalDrawPolygon(isFilled, ptArray, CurrentPen, CurrentBrush);
        }

        private void InternalDrawPolygon(bool isFilled, XPoint[] ptArray, XPen pen, XBrush brush)
        {
            if (isFilled)
            {
                Gfx.DrawPolygon(pen, brush, ptArray, XFillMode.Alternate);
            }
            else
            {
                Gfx.DrawPolygon(pen, ptArray);
            }
            this._drawingCtx.PushInstruction((oy) => InternalDrawPolygon(isFilled, ptArray.OffsetY(oy), pen, brush), ptArray);
        }


        public void BeginDrawRowTemplate(string name, int index, double offsetY, double newPageTopMargin)
        {
            this._drawingCtx.OpenBlock(name, offsetY, Gfx, newPageTopMargin);
            _gfx = XGraphics.CreateMeasureContext(new XSize(PageWidth, PageHeight),
                XGraphicsUnit.Point, XPageDirection.Downwards);
        }

        public DrawingResult EndDrawRowTemplate(int index)
        {
            double newPageOffsetY = 0;
            var result = this._drawingCtx.BlockRect;
            InternalEndRowTemplate(index, result);
            IInstructionBlock block;
            var level = _drawingCtx.Level;
            (block, _gfx) = this._drawingCtx.RestoreGraphics();
            if (result.IsEmpty)
            {
                return new()
                {
                    DrawingRect = new XRect(0, block.OffsetY, 0, 0),
                    PageOffsetY = 0
                };
            }

            if (level <= 1)
            {
                //should return pageOffsetY
                newPageOffsetY = block.Draw(this, 0, 0);
            }

            this._drawingCtx.CloseBlock();
            return  new()
            {
                DrawingRect = result,
                PageOffsetY = newPageOffsetY
            };
        }

        private void InternalEndRowTemplate(int index, XRect result)
        {
            if (_drawingCtx.DebugRowTemplate)
            {
                DebugRect(result);
                Gfx.DrawLine(_debugPen, 0, 0, 5, 2);
                Gfx.DrawLine(_debugPen, 0, 0, 2, 5);
                Gfx.DrawLine(_debugPen, 0, 0, 10, 10);
                var fmt = new XStringFormat()
                {
                    Alignment = XStringAlignment.Near,
                    LineAlignment = XLineAlignment.Near
                };

                Gfx.DrawString($"{_drawingCtx.Level}.{index}", _debugFont.Value, XBrushes.Red, 10, 10, fmt);
            }
            this._drawingCtx.PushInstruction((oy) => InternalEndRowTemplate(index, result.OffsetY(oy)), result, false, "EndRowTemplate");
        }

        public void BeginIterationTemplate(int rowCount)
        {
            //should I, Save Brushes, Pens and Fonts for replayer ?
        }

        public void EndIterationTemplate(double drawHeight)
        {
            //clean up instruction if at root level
            // var r = _drawingCtx.DrawingRect;
            // if (r is not null)
            // {
            //     //update height of last ROWTEMPLATE (including bordersize)
            //     this._drawingCtx.UpdateDrawingRect(r.Value.X, r.Value.Y, r.Value.Width, drawHeight);
            // }
        }

        public void SetOffsetY(double offsetY)
        {
            if (_logger.DebugEnabled())
            {
                _logger.WriteDebug(this, $"SetOffsetY({offsetY}, GFX:{Gfx.GetHashCode()})");
            }
            Gfx.Save();
            Gfx.TranslateTransform(0, offsetY);
            DoBeforeNewPage(RestoreOnNewPage);
            DoAfterNewPage(SaveOnNewPage);
        }

        public void ResetOffset()
        {
            if (_logger.DebugEnabled())
            {
                _logger.WriteDebug(this, $"ResetOffset(GFX:{Gfx.GetHashCode()})");
            }
            _actionOnBeforeNewPage.RemoveAt(_actionOnBeforeNewPage.Count - 1);
            _actionOnAfterNewPage.RemoveAt(_actionOnAfterNewPage.Count - 1);
            Gfx.Restore();
        }

        private void RestoreOnNewPage()
        {
            if (_logger.DebugEnabled())
            {
                _logger.WriteDebug(this, $"RestoreOnNewPage(GFX:{Gfx.GetHashCode()})");
            }
            Gfx.Restore();
        }

        private void SaveOnNewPage()
        {
            if (_logger.DebugEnabled())
            {
                _logger.WriteDebug(this, $"SaveOnNewPage(GFX:{Gfx.GetHashCode()})");
            }
            Gfx.Save();
        }

        private void DebugRect(XRect rect)
        {
            Gfx.DrawRectangle(_debugPen, rect);
        }

        private void DoBeforeNewPage(Action action)
        {
            _actionOnBeforeNewPage.Add(action);
        }

        private void DoAfterNewPage(Action action)
        {
            _actionOnAfterNewPage.Add(action);
        }
    }
}