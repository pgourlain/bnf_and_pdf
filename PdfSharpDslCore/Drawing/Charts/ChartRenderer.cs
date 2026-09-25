using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PdfSharpDslCore.Parser;

namespace PdfSharpDslCore.Drawing.Charts
{
    public enum PdfChartType
    {
        Bar,
        Line,
        Pie,
    }

    /// <summary>
    /// Draws a small chart with the drawer's own primitives (rectangles, lines, ellipses, pies and text), so
    /// everything a primitive supports (debug overlays, ROWTEMPLATE replay, VIEWSIZE) works for a chart as well.
    /// The drawer's pen, brush and font are restored afterwards.
    /// </summary>
    public static class ChartRenderer
    {
        private const double LabelSize = 8;
        private const double LabelHeight = 12;

        /// <summary>Colors used, in this order, for the series that have no color of their own.</summary>
        public static readonly PdfColor[] DefaultPalette =
        {
            PdfColor.FromRgb(68, 114, 196),
            PdfColor.FromRgb(237, 125, 49),
            PdfColor.FromRgb(112, 173, 71),
            PdfColor.FromRgb(255, 192, 0),
            PdfColor.FromRgb(91, 155, 213),
            PdfColor.FromRgb(165, 165, 165),
            PdfColor.FromRgb(38, 68, 120),
            PdfColor.FromRgb(158, 72, 14),
        };

        private static readonly PdfColor TextColor = PdfColor.FromRgb(64, 64, 64);
        private static readonly PdfColor GridColor = PdfColor.FromRgb(217, 217, 217);
        private static readonly PdfColor AxisColor = PdfColor.FromRgb(128, 128, 128);

        public static void Draw(IPdfDocumentDrawer drawer, PdfChartType type, double x, double y, double w, double h,
            IReadOnlyList<double> data, IReadOnlyList<string>? labels, IReadOnlyList<PdfColor>? colors)
        {
            if (data.Count == 0) throw new PdfParserException("CHART needs at least one value in Data.");
            if (w <= 0 || h <= 0) throw new PdfParserException("CHART needs a positive width and height.");

            var pen = drawer.CurrentPen;
            var brush = drawer.CurrentBrush;
            var font = drawer.CurrentFont;
            var highlight = drawer.HighlightBrush;
            try
            {
                drawer.HighlightBrush = null;
                drawer.CurrentFont = new PdfFont(font.FamilyName, LabelSize, PdfFontStyle.Regular);
                var colorOf = new Func<int, PdfColor>(i => colors != null && colors.Count > 0
                    ? colors[i % colors.Count]
                    : DefaultPalette[i % DefaultPalette.Length]);

                switch (type)
                {
                    case PdfChartType.Pie:
                        DrawPie(drawer, x, y, w, h, data, labels, colorOf);
                        break;
                    default:
                        DrawAxesChart(drawer, type, x, y, w, h, data, labels, colorOf);
                        break;
                }
            }
            finally
            {
                drawer.CurrentPen = pen;
                drawer.CurrentBrush = brush;
                drawer.CurrentFont = font;
                drawer.HighlightBrush = highlight;
            }
        }

        private static void DrawAxesChart(IPdfDocumentDrawer drawer, PdfChartType type, double x, double y, double w, double h,
            IReadOnlyList<double> data, IReadOnlyList<string>? labels, Func<int, PdfColor> colorOf)
        {
            var (niceMin, niceMax, step) = NiceRange(Math.Min(0, data.Min()), Math.Max(0, data.Max()));
            var ticks = new List<double>();
            for (var tick = niceMin; tick <= niceMax + step / 2; tick += step) ticks.Add(Math.Round(tick, 10));

            var axisWidth = ticks.Max(t => drawer.MeasureText(Format(t), null).Width);
            var padLeft = axisWidth + 8;
            var padBottom = labels != null ? 16 : 6;
            // room for the value label above the tallest bar; every coordinate must stay >= y, since a negative one
            // would mean "from the bottom of the page" for the drawer
            const double padTop = 14;
            const double padRight = 6;
            var px = x + padLeft;
            var py = y + padTop;
            var pw = w - padLeft - padRight;
            var ph = h - padTop - padBottom;
            if (pw <= 4 || ph <= 4) throw new PdfParserException("CHART is too small to draw its axes: give it a larger rectangle.");

            double YOf(double value) => py + ph - (value - niceMin) / (niceMax - niceMin) * ph;

            //grid and value axis
            foreach (var tick in ticks)
            {
                var ty = YOf(tick);
                drawer.CurrentPen = new PdfPen(Math.Abs(tick) < 1e-9 ? AxisColor : GridColor, Math.Abs(tick) < 1e-9 ? 0.8 : 0.5);
                drawer.DrawLine(px, ty, px + pw, ty);
                drawer.CurrentBrush = new PdfBrush(TextColor);
                Text(drawer, Format(tick), x, ty - LabelHeight / 2, padLeft - 4, PdfHorizontalAlignment.Far, PdfVerticalAlignment.Center);
            }

            var slot = pw / data.Count;
            if (type == PdfChartType.Bar)
            {
                var barWidth = slot * 0.6;
                for (var i = 0; i < data.Count; i++)
                {
                    var color = colorOf(i);
                    var top = YOf(Math.Max(data[i], 0));
                    var bottom = YOf(Math.Min(data[i], 0));
                    drawer.CurrentBrush = new PdfBrush(color);
                    drawer.CurrentPen = new PdfPen(color, 0.5);
                    if (bottom - top > 0) drawer.DrawRect(px + i * slot + (slot - barWidth) / 2, top, barWidth, bottom - top, true);

                    drawer.CurrentBrush = new PdfBrush(TextColor);
                    if (data[i] >= 0)
                        Text(drawer, Format(data[i]), px + i * slot, top - LabelHeight - 1, slot, PdfHorizontalAlignment.Center, PdfVerticalAlignment.Far);
                    else
                        Text(drawer, Format(data[i]), px + i * slot, bottom + 1, slot, PdfHorizontalAlignment.Center, PdfVerticalAlignment.Near);
                }
            }
            else
            {
                var color = colorOf(0);
                var points = data.Select((v, i) => (X: px + (i + 0.5) * slot, Y: YOf(v))).ToArray();
                drawer.CurrentPen = new PdfPen(color, 1.5);
                for (var i = 1; i < points.Length; i++) drawer.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y);
                drawer.CurrentBrush = new PdfBrush(color);
                foreach (var point in points) drawer.DrawEllipse(point.X - 2.5, point.Y - 2.5, 5, 5, true);
            }

            if (labels != null)
            {
                drawer.CurrentBrush = new PdfBrush(TextColor);
                for (var i = 0; i < data.Count && i < labels.Count; i++)
                    Text(drawer, labels[i], px + i * slot, py + ph + 3, slot, PdfHorizontalAlignment.Center, PdfVerticalAlignment.Near, true);
            }
        }

        private static void DrawPie(IPdfDocumentDrawer drawer, double x, double y, double w, double h,
            IReadOnlyList<double> data, IReadOnlyList<string>? labels, Func<int, PdfColor> colorOf)
        {
            if (data.Any(v => v < 0)) throw new PdfParserException("CHART pie needs values that are zero or positive.");
            var total = data.Sum();
            if (total <= 0) throw new PdfParserException("CHART pie needs values that do not sum to zero.");

            var hasLegend = labels != null;
            var diameter = Math.Min(h, hasLegend ? w * 0.6 : w);
            var pieX = hasLegend ? x : x + (w - diameter) / 2;
            var pieY = y + (h - diameter) / 2;

            drawer.CurrentPen = new PdfPen(PdfColor.FromRgb(255, 255, 255), 1);
            var start = -90d;
            for (var i = 0; i < data.Count; i++)
            {
                if (data[i] <= 0) continue;
                var sweep = data[i] / total * 360;
                drawer.CurrentBrush = new PdfBrush(colorOf(i));
                if (data[i] >= total)
                    drawer.DrawEllipse(pieX, pieY, diameter, diameter, true);
                else
                    drawer.DrawPie(pieX, pieY, diameter, diameter, start, sweep, true);
                start += sweep;
            }

            if (!hasLegend) return;
            var legendX = pieX + diameter + 14;
            var rows = Math.Min(labels!.Count, data.Count);
            var rowHeight = 14d;
            var legendY = y + (h - rows * rowHeight) / 2;
            for (var i = 0; i < rows; i++)
            {
                var color = colorOf(i);
                drawer.CurrentPen = new PdfPen(color, 0.5);
                drawer.CurrentBrush = new PdfBrush(color);
                drawer.DrawRect(legendX, legendY + i * rowHeight + 2, 8, 8, true);
                drawer.CurrentBrush = new PdfBrush(TextColor);
                var percent = Math.Round(data[i] / total * 100, 0, MidpointRounding.AwayFromZero);
                Text(drawer, $"{labels[i]} ({percent.ToString("0", CultureInfo.InvariantCulture)}%)", legendX + 12, legendY + i * rowHeight,
                    Math.Max(x + w - legendX - 12, 10), PdfHorizontalAlignment.Near, PdfVerticalAlignment.Near, true);
            }
        }

        private static void Text(IPdfDocumentDrawer drawer, string text, double x, double y, double width,
            PdfHorizontalAlignment hAlign, PdfVerticalAlignment vAlign, bool fit = false) =>
            drawer.DrawLineText(text, x, y, width, LabelHeight, hAlign, vAlign,
                new TextOrientation { Orientation = TextOrientationEnum.Horizontal }, fit ? new TextFitOptions(true, true) : null);

        private static string Format(double value) => Math.Round(value, 6).ToString("0.######", CultureInfo.InvariantCulture);

        /// <summary>Extends [min, max] to round tick values (1, 2 or 5 times a power of ten), about 4 intervals.</summary>
        internal static (double Min, double Max, double Step) NiceRange(double min, double max)
        {
            if (max - min < 1e-12) max = min + 1;
            var raw = (max - min) / 4;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            var normalized = raw / magnitude;
            var step = (normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10) * magnitude;
            return (Math.Floor(min / step + 1e-9) * step, Math.Ceiling(max / step - 1e-9) * step, step);
        }
    }
}
