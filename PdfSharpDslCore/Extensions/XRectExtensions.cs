using System.Collections.Generic;
using PdfSharpDslCore.Drawing;

namespace PdfSharpDslCore.Extensions
{
    public static class PdfRectExtensions
    {
        public static PdfRect OffsetY(this PdfRect r, double y)
        {
            var result = r;
            result.Offset(0, y);
            return result;
        }

        public static PdfPoint OffsetY(this PdfPoint p, double y)
        {
            return p.OffsetY(y);
        }
        
        public static PdfPoint[] OffsetY(this PdfPoint[] pts, double y)
        {
            if (y != 0)
            {
                List<PdfPoint> resultPts = new List<PdfPoint>();
                foreach (var pt in pts)
                {
                    resultPts.Add(pt.OffsetY(y));
                }
                return resultPts.ToArray();
            }
            return pts;
        }
    }
}