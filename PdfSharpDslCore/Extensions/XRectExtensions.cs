using System.Collections.Generic;
using PdfSharpCore.Drawing;

namespace PdfSharpDslCore.Extensions
{
    public static class XRectExtensions
    {
        public static XRect OffsetY(this XRect r, double y)
        {
            var result = r;
            result.Offset(0, y);
            return result;
        }

        public static XPoint OffsetY(this XPoint p, double y)
        {
            var result = p;
            result.Offset(0,y);
            return result;
        }
        
        public static XPoint[] OffsetY(this XPoint[] pts, double y)
        {
            if (y != 0)
            {
                List<XPoint> resultPts = new List<XPoint>();
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