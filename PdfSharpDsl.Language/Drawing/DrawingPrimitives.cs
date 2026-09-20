using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfSharpDslCore.Drawing
{
    public readonly struct PdfPoint
    {
        public PdfPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public PdfPoint OffsetY(double offsetY) => new(X, Y + offsetY);
    }

    public readonly struct PdfSize
    {
        public PdfSize(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width { get; }
        public double Height { get; }
    }

    public struct PdfRect
    {
        public PdfRect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double X { readonly get; private set; }
        public double Y { readonly get; private set; }
        public double Width { readonly get; private set; }
        public double Height { readonly get; private set; }

        public static PdfRect Empty => new(0, 0, -1, -1);

        public PdfRect(PdfPoint first, PdfPoint second)
            : this(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y),
                Math.Abs(second.X - first.X), Math.Abs(second.Y - first.Y))
        {
        }

        public PdfRect(PdfPoint location, PdfSize size)
            : this(location.X, location.Y, size.Width, size.Height)
        {
        }

        public readonly bool IsEmpty => Width < 0 || Height < 0;
        public readonly double Left => X;
        public readonly double Top => Y;
        public readonly double Right => X + Width;
        public readonly double Bottom => Y + Height;
        public readonly PdfPoint TopLeft => new(X, Y);

        public void Offset(double offsetX, double offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

        public readonly PdfRect OffsetY(double offsetY) => new(X, Y + offsetY, Width, Height);

        public void Union(PdfPoint point)
        {
            Union(new PdfRect(point.X, point.Y, 0, 0));
        }

        public void Union(PdfRect rect)
        {
            if (rect.IsEmpty)
            {
                return;
            }

            if (IsEmpty)
            {
                this = rect;
                return;
            }

            var left = Math.Min(Left, rect.Left);
            var top = Math.Min(Top, rect.Top);
            var right = Math.Max(Right, rect.Right);
            var bottom = Math.Max(Bottom, rect.Bottom);
            this = new PdfRect(left, top, right - left, bottom - top);
        }

        public void Intersect(PdfRect rect)
        {
            var left = Math.Max(Left, rect.Left);
            var top = Math.Max(Top, rect.Top);
            var right = Math.Min(Right, rect.Right);
            var bottom = Math.Min(Bottom, rect.Bottom);
            this = right < left || bottom < top
                ? Empty
                : new PdfRect(left, top, right - left, bottom - top);
        }
    }

    public readonly struct PdfColor
    {
        public PdfColor(byte alpha, byte red, byte green, byte blue)
        {
            Alpha = alpha;
            Red = red;
            Green = green;
            Blue = blue;
        }

        public byte Alpha { get; }
        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }

        public byte A => Alpha;

        public static PdfColor FromArgb(byte alpha, byte red, byte green, byte blue) =>
            new(alpha, red, green, blue);

        public static PdfColor FromRgb(byte red, byte green, byte blue) => new(255, red, green, blue);

        public static PdfColor FromArgb(uint argb) => new(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb);

        public static PdfColor FromGrayScale(double value)
        {
            var component = (byte)Math.Min(255, Math.Max(0, (int)Math.Round(value * 255)));
            return FromRgb(component, component, component);
        }

        public static PdfColor Black => FromRgb(0, 0, 0);
        public static PdfColor White => FromRgb(255, 255, 255);
        public static PdfColor RedColor => FromRgb(255, 0, 0);
        public static PdfColor Transparent => new(0, 0, 0, 0);

        internal string Hex => $"#{Red:X2}{Green:X2}{Blue:X2}";
        internal double Opacity => Alpha / 255d;
    }

    public static class PdfColors
    {
        private static readonly IReadOnlyDictionary<string, PdfColor> Colors =
            new Dictionary<string, PdfColor>(StringComparer.OrdinalIgnoreCase)
            {
                ["black"] = PdfColor.FromRgb(0, 0, 0),
                ["blue"] = PdfColor.FromRgb(0, 0, 255),
                ["crimson"] = PdfColor.FromRgb(220, 20, 60),
                ["darkblue"] = PdfColor.FromRgb(0, 0, 139),
                ["darkgreen"] = PdfColor.FromRgb(0, 100, 0),
                ["darkslategray"] = PdfColor.FromRgb(47, 79, 79),
                ["gold"] = PdfColor.FromRgb(255, 215, 0),
                ["gray"] = PdfColor.FromRgb(128, 128, 128),
                ["green"] = PdfColor.FromRgb(0, 128, 0),
                ["lightblue"] = PdfColor.FromRgb(173, 216, 230),
                ["lightgray"] = PdfColor.FromRgb(211, 211, 211),
                ["lightgreen"] = PdfColor.FromRgb(144, 238, 144),
                ["lightsalmon"] = PdfColor.FromRgb(255, 160, 122),
                ["lightseagreen"] = PdfColor.FromRgb(32, 178, 170),
                ["maroon"] = PdfColor.FromRgb(128, 0, 0),
                ["orange"] = PdfColor.FromRgb(255, 165, 0),
                ["purple"] = PdfColor.FromRgb(128, 0, 128),
                ["red"] = PdfColor.FromRgb(255, 0, 0),
                ["slategray"] = PdfColor.FromRgb(112, 128, 144),
                ["steelblue"] = PdfColor.FromRgb(70, 130, 180),
                ["tomato"] = PdfColor.FromRgb(255, 99, 71),
                ["transparent"] = PdfColor.Transparent,
                ["violet"] = PdfColor.FromRgb(238, 130, 238),
                ["white"] = PdfColor.FromRgb(255, 255, 255),
                ["yellow"] = PdfColor.FromRgb(255, 255, 0),
            };

        public static IEnumerable<string> Names => Colors.Keys;

        public static PdfColor FromName(string name)
        {
            return Colors.TryGetValue(name, out var color) ? color : PdfColor.Black;
        }
    }

    public enum PdfDashStyle
    {
        Solid,
        Dash,
        Dot,
        DashDot,
        DashDotDot,
    }

    public sealed class PdfPen
    {
        public PdfPen(PdfColor color, double width)
        {
            Color = color;
            Width = width;
        }

        public PdfColor Color { get; }
        public double Width { get; }
        public PdfDashStyle DashStyle { get; set; } = PdfDashStyle.Solid;
    }

    public sealed class PdfBrush
    {
        public PdfBrush(PdfColor color)
        {
            Color = color;
        }

        public PdfColor Color { get; }
    }

    [Flags]
    public enum PdfFontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        BoldItalic = Bold | Italic,
        Underline = 4,
        Strikeout = 8,
    }

    public sealed class PdfFont
    {
        public PdfFont(string familyName, double size, PdfFontStyle style = PdfFontStyle.Regular)
        {
            FamilyName = familyName;
            Size = size;
            Style = style;
        }

        public string FamilyName { get; }
        public double Size { get; }
        public PdfFontStyle Style { get; }
    }

    public sealed class PdfImage
    {
        private readonly byte[] _data;

        public PdfImage(ReadOnlySpan<byte> data)
        {
            _data = data.ToArray();
        }

        public ReadOnlyMemory<byte> Data => _data;
    }

    public sealed class PdfMargins
    {
        private double _all;

        public double All
        {
            set => Left = Top = Right = Bottom = _all = value;
            get => _all;
        }

        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
    }

    public enum PdfHorizontalAlignment
    {
        Near,
        Center,
        Far,
    }

    public enum PdfVerticalAlignment
    {
        Near,
        Center,
        Far,
    }

    public enum PdfPageOrientation
    {
        Portrait,
        Landscape,
    }

    public enum PdfPageSize
    {
        Undefined,
        A0,
        A1,
        A2,
        A3,
        A4,
        A5,
        A6,
        Letter,
        Legal,
        Ledger,
        Tabloid,
    }
}