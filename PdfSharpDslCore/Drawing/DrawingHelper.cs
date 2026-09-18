namespace PdfSharpDslCore.Drawing
{
    internal static class DrawingHelper
    {
        /// <summary>
        /// compute rect from text and his format
        /// </summary>
        /// <param name="r"></param>
        /// <param name="textSize"></param>
        /// <param name="fmt"></param>
        /// <returns></returns>
        public static PdfRect RectFromStringFormat(PdfRect r, PdfSize textSize,
            PdfHorizontalAlignment horizontalAlignment, PdfVerticalAlignment verticalAlignment)
        {
            var result = new PdfRect(r.TopLeft, textSize);

            switch (horizontalAlignment)
            {
                case PdfHorizontalAlignment.Center:
                    result.Offset((r.Width - textSize.Width) / 2, 0);
                    break;
                case PdfHorizontalAlignment.Near:
                    break;
                case PdfHorizontalAlignment.Far:
                    result.Offset(r.Width - textSize.Width, 0);
                    break;
            }

            switch (verticalAlignment)
            {
                case PdfVerticalAlignment.Center:
                    result.Offset(0, (r.Height - textSize.Height) / 2);
                    break;
                case PdfVerticalAlignment.Near:
                    break;
                case PdfVerticalAlignment.Far:
                    result.Offset(0, r.Height - textSize.Height);
                    break;
            }

            //to crop if text is larger/higher than provided rectangle
            result.Intersect(r);
            return result;
        }

        public static PdfRect RectFromStringFormat(double x, double y, PdfSize textSize,
            PdfHorizontalAlignment horizontalAlignment, PdfVerticalAlignment verticalAlignment)
        {
            var result = new PdfRect(x, y, textSize.Width, textSize.Height);

            var xOffset = 0.0;
            var yOffset = 0.0;
            switch (horizontalAlignment)
            {
                case PdfHorizontalAlignment.Center:
                    xOffset -= textSize.Width / 2;
                    break;
                case PdfHorizontalAlignment.Near:
                    break;
                case PdfHorizontalAlignment.Far:
                    xOffset -= textSize.Width;
                    break;
            }

            switch (verticalAlignment)
            {
                case PdfVerticalAlignment.Center:
                    yOffset -= textSize.Height / 2;
                    break;
                case PdfVerticalAlignment.Near:
                    break;
                case PdfVerticalAlignment.Far:
                    yOffset -= textSize.Height;
                    break;
            }

            result.Offset(xOffset, yOffset);

            return result;
        }
        public static (double, double, double, double) CoordRectToPage(double pageWidth, double pageHeight,
            double x, double y, double w, double h)
        {
            if (x < 0)
            {
                x = pageWidth + x;
            }

            if (y < 0)
            {
                y = pageHeight + y;
            }

            if (w < 0)
            {
                w = pageWidth + w - x;
            }

            if (h < 0)
            {
                h = pageHeight + h - y;
            }

            return (x, y, w, h);
        }

        public static (double, double, double?, double?) CoordRectToPage(double pageWidth, double pageHeight,
            double x, double y, double? w, double? h)
        {
            if (x < 0)
            {
                x = pageWidth + x;
            }

            if (y < 0)
            {
                y = pageHeight + y;
            }

            if (w is < 0)
            {
                w = pageWidth + w - x;
            }

            if (h is < 0)
            {
                h = pageHeight + h - y;
            }

            return (x, y, w, h);
        }
    }
}