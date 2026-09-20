using Microsoft.Extensions.Logging;
using PdfSharpDslCore.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerraPDF.Core;

namespace PdfSharpDslCore.Drawing
{
    public sealed class PdfDocumentDrawer : IDisposable, IPdfDocumentDrawer
    {
        private sealed class RecordedPage
        {
            public RecordedPage(PdfPageSize size, PdfPageOrientation orientation)
            {
                (Width, Height) = GetPageDimensions(size, orientation);
            }

            public double Width { get; }
            public double Height { get; }
            public double ScaleX { get; set; } = 1;
            public double ScaleY { get; set; } = 1;
            public List<Action<VectorCanvas>> Commands { get; } = new();
        }

        private readonly ILogger? _logger;
        private readonly DrawingContext _drawingCtx;
        private readonly List<RecordedPage> _pages = new();
        private readonly List<Action<int>> _onNewPageHooks = new();
        private readonly Stack<bool> _measurementStates = new();
        private PdfPageSize _defaultPageSize = PdfPageSize.A4;
        private PdfPageOrientation _defaultPageOrientation = PdfPageOrientation.Portrait;
        private PdfPen? _currentPen;
        private PdfBrush? _currentBrush;
        private PdfFont? _currentFont;
        private PdfPoint _currentPoint;
        private bool _isMeasuring;

        public PdfDocumentDrawer(ILogger? logger = null)
        {
            _logger = logger;
            _drawingCtx = new DrawingContext(logger);
        }

        public DebugOptions DebugOptions
        {
            get => _drawingCtx.DebugOptions;
            set => _drawingCtx.DebugOptions = value;
        }

        public PdfPen CurrentPen
        {
            get => _currentPen ??= new PdfPen(PdfColor.Black, 1);
            set => _currentPen = value ?? throw new ArgumentNullException(nameof(value));
        }

        public PdfBrush CurrentBrush
        {
            get => _currentBrush ??= new PdfBrush(PdfColor.Black);
            set => _currentBrush = value ?? throw new ArgumentNullException(nameof(value));
        }

        public PdfBrush? HighlightBrush { get; set; }

        public PdfFont CurrentFont
        {
            get => _currentFont ??= new PdfFont("Helvetica", 10);
            set => _currentFont = value ?? throw new ArgumentNullException(nameof(value));
        }

        public double PageWidth => CurrentPage.Width;
        public double PageHeight => CurrentPage.Height;

        private RecordedPage CurrentPage
        {
            get
            {
                if (_pages.Count == 0)
                {
                    _pages.Add(new RecordedPage(_defaultPageSize, _defaultPageOrientation));
                }

                return _pages[_pages.Count - 1];
            }
        }

        public void PublishPdf(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            CreateDocument().PublishPdf(stream);
        }

        public void PublishPdf(string path)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            CreateDocument().PublishPdf(path);
        }

        public byte[] PublishPdf() => CreateDocument().PublishPdf();

        private DocumentComposer CreateDocument()
        {
            _ = CurrentPage;
            return Document.Create(document =>
            {
                foreach (var recordedPage in _pages)
                {
                    document.Page(page =>
                    {
                        page.Size(recordedPage.Width, recordedPage.Height);
                        page.Margin(0);
                        page.Content().Canvas(recordedPage.Height, canvas =>
                        {
                            foreach (var command in recordedPage.Commands)
                            {
                                command(canvas);
                            }
                        });
                    });
                }
            });
        }

        public void DrawLine(double x, double y, double x1, double y1)
        {
            var page = CurrentPage;
            InternalDrawLine(ScalePen(CurrentPen, page), ScaleX(ResolveX(x, page), page), ScaleY(ResolveY(y, page), page),
                ScaleX(ResolveX(x1, page), page), ScaleY(ResolveY(y1, page), page));
        }

        private void InternalDrawLine(PdfPen pen, double x, double y, double x1, double y1)
        {
            AddCommand(canvas => DrawStyledLine(canvas, pen, x, y, x1, y1));
            _drawingCtx.PushInstruction(offset => InternalDrawLine(pen, x, y + offset, x1, y1 + offset),
                new PdfRect(new PdfPoint(x, y), new PdfPoint(x1, y1)));
        }

        private static void DrawStyledLine(VectorCanvas canvas, PdfPen pen, double x, double y, double x1, double y1)
        {
            canvas.Line(x, y, x1, y1, pen.Color.Hex, pen.Width, pen.Color.Opacity, GetDashPattern(pen));
        }

        private static double[]? GetDashPattern(PdfPen pen)
        {
            var width = Math.Max(pen.Width, 0.1);
            return pen.DashStyle switch
            {
                PdfDashStyle.Dash => new[] { 4 * width, 3 * width },
                PdfDashStyle.Dot => new[] { width, 2 * width },
                PdfDashStyle.DashDot => new[] { 4 * width, 3 * width, width, 3 * width },
                PdfDashStyle.DashDotDot => new[] { 4 * width, 3 * width, width, 2 * width, width, 2 * width },
                _ => null,
            };
        }

        public void DrawRect(double x, double y, double w, double h, bool isFilled)
        {
            var page = CurrentPage;
            (x, y, w, h) = DrawingHelper.CoordRectToPage(page.Width, page.Height, x, y, w, h);
            InternalDrawRect(ScalePen(CurrentPen, page), CurrentBrush, ScaleX(x, page), ScaleY(y, page), ScaleX(w, page), ScaleY(h, page), isFilled);
        }

        private void InternalDrawRect(PdfPen pen, PdfBrush brush, double x, double y, double w, double h, bool isFilled)
        {
            var dashPattern = GetDashPattern(pen);
            AddCommand(canvas =>
            {
                if (isFilled) canvas.FillRect(x, y, w, h, brush.Color.Hex, brush.Color.Opacity);
                canvas.StrokeRect(x, y, w, h, pen.Color.Hex, pen.Width, pen.Color.Opacity, dashPattern);
            });
            _drawingCtx.PushInstruction(offset => InternalDrawRect(pen, brush, x, y + offset, w, h, isFilled),
                new PdfRect(x, y, w, h));
        }

        public void DrawEllipse(double x, double y, double w, double h, bool isFilled)
        {
            var page = CurrentPage;
            (x, y, w, h) = DrawingHelper.CoordRectToPage(page.Width, page.Height, x, y, w, h);
            InternalDrawEllipse(ScalePen(CurrentPen, page), CurrentBrush, ScaleX(x, page), ScaleY(y, page), ScaleX(w, page), ScaleY(h, page), isFilled);
        }

        private void InternalDrawEllipse(PdfPen pen, PdfBrush brush, double x, double y, double w, double h, bool isFilled)
        {
            AddCommand(canvas =>
            {
                var cx = x + w / 2;
                var cy = y + h / 2;
                if (isFilled) canvas.FillEllipse(cx, cy, w / 2, h / 2, brush.Color.Hex, brush.Color.Opacity);
                canvas.StrokeEllipse(cx, cy, w / 2, h / 2, pen.Color.Hex, pen.Width, pen.Color.Opacity);
            });
            _drawingCtx.PushInstruction(offset => InternalDrawEllipse(pen, brush, x, y + offset, w, h, isFilled), new PdfRect(x, y, w, h));
        }

        public void DrawText(string text, double x, double y, double? w, double? h)
        {
            var page = CurrentPage;
            (x, y, w, h) = DrawingHelper.CoordRectToPage(page.Width, page.Height, x, y, w, h);
            InternalDrawText(text, ScaleX(x, page), ScaleY(y, page), w.HasValue ? ScaleX(w.Value, page) : page.Width - x,
                h.HasValue ? ScaleY(h.Value, page) : null, PdfHorizontalAlignment.Near, PdfVerticalAlignment.Near,
                ScaleFont(CurrentFont, page), CurrentBrush, null);
        }

        public void DrawLineText(string text, double x, double y, double? w, double? h, PdfHorizontalAlignment hAlign,
            PdfVerticalAlignment vAlign, TextOrientation textOrientation)
        {
            var page = CurrentPage;
            (x, y, w, h) = DrawingHelper.CoordRectToPage(page.Width, page.Height, x, y, w, h);
            InternalDrawText(text, ScaleX(x, page), ScaleY(y, page), w.HasValue ? ScaleX(w.Value, page) : null,
                h.HasValue ? ScaleY(h.Value, page) : null, hAlign, vAlign, ScaleFont(CurrentFont, page), CurrentBrush, HighlightBrush,
                textOrientation?.Angle ?? textOrientation?.Orientation switch
                {
                    TextOrientationEnum.Vertical => 90,
                    TextOrientationEnum.HorizontalInvert => 180,
                    TextOrientationEnum.VerticalInvert => 270,
                    _ => 0,
                });
        }

        private void InternalDrawText(string text, double x, double y, double? w, double? h, PdfHorizontalAlignment hAlign,
            PdfVerticalAlignment vAlign, PdfFont font, PdfBrush brush, PdfBrush? highlight, double angle = 0)
        {
            var lines = WrapText(text, w, font);
            var lineHeight = font.Size * 1.2;
            var measuredWidth = lines.Count == 0 ? 0 : lines.Max(line => MeasureText(line, font));
            var measuredHeight = lines.Count * lineHeight;
            var rect = new PdfRect(x, y, w ?? measuredWidth, h ?? measuredHeight);
            var textSize = new PdfSize(Math.Min(measuredWidth, rect.Width), Math.Min(measuredHeight, rect.Height));
            var textRect = w.HasValue
                ? DrawingHelper.RectFromStringFormat(rect, textSize, hAlign, vAlign)
                : DrawingHelper.RectFromStringFormat(x, y, textSize, hAlign, vAlign);

            AddCommand(canvas =>
            {
                if (highlight is not null)
                    canvas.FillRect(textRect.X, textRect.Y, textRect.Width, textRect.Height, highlight.Color.Hex, highlight.Color.Opacity);

                for (var index = 0; index < lines.Count; index++)
                {
                    var line = lines[index];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var lineWidth = MeasureText(line, font);
                    var lineX = w.HasValue
                        ? hAlign switch
                        {
                            PdfHorizontalAlignment.Center => rect.X + (rect.Width - lineWidth) / 2,
                            PdfHorizontalAlignment.Far => rect.Right - lineWidth,
                            _ => rect.X,
                        }
                        : hAlign switch
                        {
                            PdfHorizontalAlignment.Center => x - lineWidth / 2,
                            PdfHorizontalAlignment.Far => x - lineWidth,
                            _ => x,
                        };
                    canvas.Text(line, lineX, textRect.Y + font.Size + index * lineHeight, brush.Color.Hex, font.Size,
                            font.FamilyName, font.Style.HasFlag(PdfFontStyle.Bold), font.Style.HasFlag(PdfFontStyle.Italic), brush.Color.Opacity,
                            angle);
                }
            });

            _drawingCtx.PushInstruction(offset => InternalDrawText(text, x, y + offset, w, h, hAlign, vAlign, font, brush, highlight, angle),
                textRect, instrName: $"DrawText({text})");
        }

        public void DrawTitle(string text, double margin, PdfHorizontalAlignment hAlign, PdfVerticalAlignment vAlign)
        {
            var height = CurrentFont.Size * 1.2;
            if (margin < 0) margin = PageHeight - height + margin;
            InternalDrawText(text, 0, margin, PageWidth, height, hAlign, vAlign, CurrentFont, CurrentBrush, HighlightBrush);
        }

        public void DrawTable(double x, double y, TableDefinition table)
        {
            ArgumentNullException.ThrowIfNull(table);
            var availableWidth = PageWidth - x;
            var fonts = table.Columns.Select(column => column.Font ?? CurrentFont).ToArray();
            var margins = table.CellMargin;
            var cells = LayoutTableCells(table);

            for (var index = 0; index < table.Columns.Count; index++)
            {
                var column = table.Columns[index];
                var contentWidth = cells
                    .Where(cell => cell.Column == index && cell.Cell.ColumnSpan == 1)
                    .Select(cell => MeasureCellWidth(cell.Cell.Text, fonts[index]))
                    .DefaultIfEmpty(0)
                    .Max();
                var desiredWidth = Math.Max(MeasureCellWidth(column.ColumnHeaderName, fonts[index]), contentWidth)
                    + margins.Left + margins.Right;
                column.DesiredWidth ??= Math.Min(desiredWidth,
                    table.ColMaxWidth(index, availableWidth));
                table.HeaderHeight ??= fonts[index].Size * 1.2 + margins.Top + margins.Bottom;
            }

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                var contentHeight = cells.Where(cell => cell.Row == rowIndex && cell.Cell.RowSpan == 1)
                    .Select(cell =>
                    {
                        var width = table.Columns.Skip(cell.Column).Take(cell.Cell.ColumnSpan).Sum(column => column.DrawWidth);
                        var font = fonts[cell.Column];
                        return MeasureCellLineCount(cell.Cell.Text, width - margins.Left - margins.Right, font)
                               * font.Size * 1.2 + margins.Top + margins.Bottom;
                    }).DefaultIfEmpty(fonts[0].Size * 1.2 + margins.Top + margins.Bottom).Max();
                row.DesiredHeight ??= contentHeight;
            }

            foreach (var cell in cells.Where(cell => cell.Cell.RowSpan > 1))
            {
                var width = table.Columns.Skip(cell.Column).Take(cell.Cell.ColumnSpan).Sum(column => column.DrawWidth);
                var font = fonts[cell.Column];
                var requiredHeight = MeasureCellLineCount(cell.Cell.Text, width - margins.Left - margins.Right, font)
                                     * font.Size * 1.2 + margins.Top + margins.Bottom;
                var rows = table.Rows.Skip(cell.Row).Take(cell.Cell.RowSpan).ToArray();
                var missingHeight = requiredHeight - rows.Sum(row => row.DesiredHeight ?? 0);
                if (missingHeight > 0)
                    rows[^1].DesiredHeight = (rows[^1].DesiredHeight ?? 0) + missingHeight;
            }

            if (y + (table.HeaderHeight ?? 0) > PageHeight) { NewPage(); y = 1; }
            var offsetY = 0d;
            if (table.ShowHeader)
            {
                DrawTableRow(x, y, table.Columns.Select(column => column.ColumnHeaderName).ToArray(), table.HeaderHeight ?? 0, table, fonts, table.HeaderBackColor);
                offsetY = table.HeaderHeight ?? 0;
            }

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                var height = row.DesiredHeight ?? 0;
                var rowCells = cells.Where(cell => cell.Row == rowIndex).ToArray();
                var requiredHeight = rowCells.Select(cell => table.Rows.Skip(rowIndex).Take(cell.Cell.RowSpan)
                    .Sum(spannedRow => spannedRow.DesiredHeight ?? 0)).DefaultIfEmpty(height).Max();
                if (y + offsetY + requiredHeight > PageHeight) { NewPage(); y = table.TopMarginOnPageBreak; offsetY = 0; }
                foreach (var cell in rowCells)
                    DrawTableCell(x, y + offsetY, cell, table, fonts);
                offsetY += height;
            }
        }

        private static List<TableCellPlacement> LayoutTableCells(TableDefinition table)
        {
            var result = new List<TableCellPlacement>();
            var occupiedUntilRow = new int[table.Columns.Count];
            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                var cells = row.Cells.Count > 0
                    ? row.Cells
                    : row.Data.Select(text => new CellDefinition { Text = text }).ToList();
                var columnIndex = 0;
                foreach (var cell in cells)
                {
                    while (columnIndex < table.Columns.Count && occupiedUntilRow[columnIndex] > rowIndex)
                        columnIndex++;
                    if (columnIndex >= table.Columns.Count) break;

                    cell.ColumnSpan = Math.Min(Math.Max(1, cell.ColumnSpan), table.Columns.Count - columnIndex);
                    cell.RowSpan = Math.Min(Math.Max(1, cell.RowSpan), table.Rows.Count - rowIndex);
                    result.Add(new TableCellPlacement(rowIndex, columnIndex, cell));
                    for (var index = columnIndex; index < columnIndex + cell.ColumnSpan; index++)
                        occupiedUntilRow[index] = rowIndex + cell.RowSpan;
                    columnIndex += cell.ColumnSpan;
                }
            }
            return result;
        }

        private void DrawTableCell(double x, double y, TableCellPlacement placement, TableDefinition table, PdfFont[] fonts)
        {
            var column = table.Columns[placement.Column];
            var offsetX = table.Columns.Take(placement.Column).Sum(item => item.DrawWidth);
            var width = table.Columns.Skip(placement.Column).Take(placement.Cell.ColumnSpan).Sum(item => item.DrawWidth);
            var height = table.Rows.Skip(placement.Row).Take(placement.Cell.RowSpan).Sum(row => row.DesiredHeight ?? 0);
            if (column.BackColor is not null)
            {
                var previousBrush = CurrentBrush;
                CurrentBrush = column.BackColor;
                DrawRect(x + offsetX, y, width, height, true);
                CurrentBrush = previousBrush;
            }
            else DrawRect(x + offsetX, y, width, height, false);

            var margins = table.CellMargin;
            InternalDrawText(placement.Cell.Text, x + offsetX + margins.Left, y + margins.Top,
                Math.Max(0, width - margins.Left - margins.Right), Math.Max(0, height - margins.Top - margins.Bottom),
                placement.Cell.HorizontalAlignment ?? column.Alignment,
                placement.Cell.VerticalAlignment ?? PdfVerticalAlignment.Center,
                fonts[placement.Column], column.Brush ?? CurrentBrush, null);
        }

        private sealed record TableCellPlacement(int Row, int Column, CellDefinition Cell);

        private void DrawTableRow(double x, double y, string[] values, double height, TableDefinition table, PdfFont[] fonts, PdfBrush? rowBackground)
        {
            var offsetX = 0d;
            for (var index = 0; index < table.Columns.Count; index++)
            {
                var column = table.Columns[index];
                var width = column.DrawWidth;
                var background = rowBackground ?? column.BackColor;
                if (background is not null)
                {
                    var previousBrush = CurrentBrush;
                    CurrentBrush = background;
                    DrawRect(x + offsetX, y, width, height, true);
                    CurrentBrush = previousBrush;
                }
                else DrawRect(x + offsetX, y, width, height, false);

                var margins = table.CellMargin;
                InternalDrawText(index < values.Length ? values[index] : string.Empty, x + offsetX + margins.Left, y + margins.Top,
                    Math.Max(0, width - margins.Left - margins.Right), Math.Max(0, height - margins.Top - margins.Bottom),
                    column.Alignment, PdfVerticalAlignment.Center, fonts[index], column.Brush ?? CurrentBrush, null);
                offsetX += width;
            }
        }

        public void SetViewSize(double w, double h)
        {
            if (w <= 0 || h <= 0) throw new ArgumentOutOfRangeException(nameof(w), "View dimensions must be positive.");
            CurrentPage.ScaleX = CurrentPage.Width / w;
            CurrentPage.ScaleY = CurrentPage.Height / h;
        }

        public void NewPage(PdfPageSize? pageSize = null, PdfPageOrientation? pageOrientation = null)
        {
            _defaultPageSize = pageSize ?? _defaultPageSize;
            _defaultPageOrientation = pageOrientation ?? _defaultPageOrientation;
            _pages.Add(new RecordedPage(_defaultPageSize, _defaultPageOrientation));
            _logger?.WriteDebug(this, "AddPage");
            _onNewPageHooks.ForEach(callback => callback(_pages.Count));
            if ((DebugOptions & DebugOptions.DebugRule) == DebugOptions.DebugRule)
                for (var position = 25d; position < PageHeight; position += 25)
                    DrawLine(0, position, position % 50 == 0 ? 50 : 25, position);
        }

        public void MoveTo(double x, double y) => _currentPoint = new PdfPoint(x, y);

        public void LineTo(double x, double y)
        {
            var page = CurrentPage;
            var endPoint = new PdfPoint(x, y);
            InternalDrawLine(ScalePen(CurrentPen, page), ScaleX(_currentPoint.X, page), ScaleY(_currentPoint.Y, page),
                ScaleX(endPoint.X, page), ScaleY(endPoint.Y, page));
            _currentPoint = endPoint;
        }

        public void DrawImage(PdfImage image, double x, double y, double? w, double? h, bool sizeInPixel, bool cropImage)
        {
            ArgumentNullException.ThrowIfNull(image);
            var page = CurrentPage;
            (x, y, w, h) = DrawingHelper.CoordRectToPage(page.Width, page.Height, x, y, w, h);
            var data = image.Data.ToArray();
            var naturalSize = VectorCanvas.GetImageSizeInPoints(data);
            var width = w.HasValue ? (sizeInPixel ? w.Value * 72d / 96d : w.Value) : naturalSize.Width;
            var height = h.HasValue ? (sizeInPixel ? h.Value * 72d / 96d : h.Value) : naturalSize.Height;
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(w), "Image dimensions must be positive.");

            var fit = cropImage ? ImageFit.CropTopLeft : ImageFit.Stretch;
            AddCommand(canvas => canvas.Image(data, ScaleX(x, page), ScaleY(y, page),
                ScaleX(width, page), ScaleY(height, page), fit));
            _drawingCtx.PushInstruction(offset => DrawImage(image, x, y + offset, w, h, sizeInPixel, cropImage),
                new PdfRect(x, y, width, height), instrName: "DrawImage");
        }

        public void DrawPie(double x, double y, double? w, double? h, double startAngle, double sweepAngle, bool isFilled)
        {
            var page = CurrentPage;
            var width = w ?? 0;
            var height = h ?? 0;
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(w), "Pie dimensions must be positive.");
            InternalDrawPie(ScalePen(CurrentPen, page), CurrentBrush, ScaleX(x, page), ScaleY(y, page),
                ScaleX(width, page), ScaleY(height, page), startAngle, sweepAngle, isFilled);
        }

        private void InternalDrawPie(PdfPen pen, PdfBrush brush, double x, double y, double width, double height,
            double startAngle, double sweepAngle, bool isFilled)
        {
            AddCommand(canvas =>
            {
                if (isFilled)
                    canvas.DrawPie(x, y, width, height, startAngle, sweepAngle,
                        brush.Color.Hex, pen.Color.Hex, pen.Width, brush.Color.Opacity);
                else
                    canvas.StrokePie(x, y, width, height, startAngle, sweepAngle,
                        pen.Color.Hex, pen.Width, pen.Color.Opacity);
            });
            _drawingCtx.PushInstruction(offset => InternalDrawPie(pen, brush, x, y + offset, width, height, startAngle, sweepAngle, isFilled),
                new PdfRect(x, y, width, height), instrName: "DrawPie");
        }

        public void DrawPolygon(IEnumerable<PdfPoint> points, bool isFilled)
        {
            var page = CurrentPage;
            var transformed = points.Select(point => new PdfPoint(ScaleX(point.X, page), ScaleY(point.Y, page))).ToArray();
            if (transformed.Length < 3) throw new ArgumentException("A polygon requires at least three points.", nameof(points));
            InternalDrawPolygon(ScalePen(CurrentPen, page), CurrentBrush, transformed, isFilled);
        }

        private void InternalDrawPolygon(PdfPen pen, PdfBrush brush, PdfPoint[] points, bool isFilled)
        {
            var tuples = points.Select(point => (point.X, point.Y)).ToArray();
            AddCommand(canvas => canvas.Path(path =>
            {
                path.Polygon(tuples).Stroke(pen.Color.Hex, pen.Width).Opacity(pen.Color.Opacity);
                if (isFilled) path.Fill(brush.Color.Hex).Opacity(brush.Color.Opacity);
            }));
            _drawingCtx.PushInstruction(offset => InternalDrawPolygon(pen, brush, points.Select(point => point.OffsetY(offset)).ToArray(), isFilled), points);
        }

        public void BeginDrawRowTemplate(string name, int index, double offsetY, double newPageTopMargin)
        {
            _drawingCtx.OpenBlock($"{name}:{index}", offsetY, newPageTopMargin);
            _measurementStates.Push(_isMeasuring);
            _isMeasuring = true;
        }

        public DrawingResult EndDrawRowTemplate(int index)
        {
            var result = _drawingCtx.BlockRect;
            var block = _drawingCtx.EndMeasure();
            _isMeasuring = _measurementStates.Pop();
            var pageOffsetY = 0d;
            if (result.IsEmpty) result = new PdfRect(0, block.OffsetY, 0, 0);
            else if (_drawingCtx.Level == 0) pageOffsetY = block.Draw(this, 0, 0);
            _drawingCtx.CloseBlock();
            return new DrawingResult { DrawingRect = result, PageOffsetY = pageOffsetY };
        }

        public void BeginIterationTemplate(int rowCount) { }
        public void EndIterationTemplate(double drawHeight) { }

        public void RegisterOnNewPage(Action<int> callback)
        {
            if (callback is not null && !_onNewPageHooks.Contains(callback)) _onNewPageHooks.Add(callback);
        }

        public void UnRegisterOnNewPage(Action<int> callback) => _onNewPageHooks.Remove(callback);

        private void AddCommand(Action<VectorCanvas> command)
        {
            if (!_isMeasuring) CurrentPage.Commands.Add(command);
        }

        private static double MeasureText(string text, PdfFont font)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return VectorCanvas.MeasureTextWidth(text, font.Size, font.FamilyName, font.Style.HasFlag(PdfFontStyle.Bold), font.Style.HasFlag(PdfFontStyle.Italic));
        }

        private static double MeasureCellWidth(string text, PdfFont font) =>
            text.Replace("\r\n", "\n").Split('\n').Select(line => MeasureText(line, font)).DefaultIfEmpty(0).Max();

        private static int MeasureCellLineCount(string text, double width, PdfFont font) =>
            WrapText(text, width, font).Count;

        private static List<string> WrapText(string text, double? maxWidth, PdfFont font)
        {
            var lines = new List<string>();
            foreach (var sourceLine in text.Replace("\r\n", "\n").Split('\n'))
            {
                if (maxWidth is null || maxWidth <= 0 || MeasureText(sourceLine, font) <= maxWidth) { lines.Add(sourceLine); continue; }
                var current = string.Empty;
                foreach (var word in sourceLine.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidate = current.Length == 0 ? word : $"{current} {word}";
                    if (current.Length > 0 && MeasureText(candidate, font) > maxWidth) { lines.Add(current); current = word; }
                    else current = candidate;
                }
                lines.Add(current);
            }
            return lines;
        }

        private static double ResolveX(double x, RecordedPage page) => x < 0 ? page.Width + x : x;
        private static double ResolveY(double y, RecordedPage page) => y < 0 ? page.Height + y : y;
        private static double ScaleX(double value, RecordedPage page) => value * page.ScaleX;
        private static double ScaleY(double value, RecordedPage page) => value * page.ScaleY;

        private static PdfPen ScalePen(PdfPen pen, RecordedPage page)
        {
            var scaled = new PdfPen(pen.Color, pen.Width * ScaleFactor(page)) { DashStyle = pen.DashStyle };
            return scaled;
        }

        private static PdfFont ScaleFont(PdfFont font, RecordedPage page) =>
            new(font.FamilyName, font.Size * ScaleFactor(page), font.Style);

        private static double ScaleFactor(RecordedPage page) =>
            Math.Sqrt(page.ScaleX * page.ScaleY);

        private static (double Width, double Height) GetPageDimensions(PdfPageSize size, PdfPageOrientation orientation)
        {
            var dimensions = size switch
            {
                PdfPageSize.A0 => (2383.94, 3370.39),
                PdfPageSize.A1 => (1683.78, 2383.94),
                PdfPageSize.A2 => (1190.55, 1683.78),
                PdfPageSize.A3 => (841.89, 1190.55),
                PdfPageSize.A5 => (419.53, 595.28),
                PdfPageSize.A6 => (297.64, 419.53),
                PdfPageSize.Letter => (612d, 792d),
                PdfPageSize.Legal => (612d, 1008d),
                PdfPageSize.Ledger => (1224d, 792d),
                PdfPageSize.Tabloid => (792d, 1224d),
                _ => (595.28, 841.89),
            };
            return orientation == PdfPageOrientation.Landscape ? (dimensions.Item2, dimensions.Item1) : dimensions;
        }

        public void Dispose() { }
    }
}
