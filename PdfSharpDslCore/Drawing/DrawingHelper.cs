using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace PdfSharpDslCore.Drawing
{
    internal static class DrawingHelper
    {
        public static XRect RectFromStringFormat(XRect r, XSize textSize, XStringFormat fmt)
        {
            var result = new XRect(r.TopLeft, textSize);

            switch (fmt.Alignment)
            {
                case XStringAlignment.Center:
                    result.Offset((r.Width - textSize.Width) / 2, 0);
                    break;
                case XStringAlignment.Near:
                    break;
                case XStringAlignment.Far:
                    result.Offset(r.Right - textSize.Width, 0);
                    break;
            }

            switch (fmt.LineAlignment)
            {
                case XLineAlignment.Center:
                    result.Offset(0, (r.Height - textSize.Height) / 2);
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

        public static XRect RectFromStringFormat(double x, double y, XSize textSize, XStringFormat fmt)
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
        public static (double, double, double, double) CoordRectToPage(this PdfPage page, double x, double y, double w, double h)
        {
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

        public static (double, double, double?, double?) CoordRectToPage(this PdfPage page, double x, double y, double? w, double? h)
        {
            if (x < 0)
            {
                x = page.Width + x;
            }

            if (y < 0)
            {
                y = page.Height + y;
            }

            if (w is < 0)
            {
                w = page.Width + w - x;
            }

            if (h is < 0)
            {
                h = page.Height + h - y;
            }

            return (x, y, w, h);
        }
    }
}