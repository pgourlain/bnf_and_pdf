using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;

namespace PdfSharpDslCore.Drawing
{
    public class DrawingContext
    {
        Stack<XRect?> history = new Stack<XRect?>();
        internal XRect? DrawingRect { get; private set; }
        public int Level => history.Count;

        public DebugOptions DebugOptions { get; set; }
        public bool DebugText => (DebugOptions & (DebugOptions.DebugText | DebugOptions.DebugAll)) > 0;
        public bool DebugRowTemplate => (DebugOptions & (DebugOptions.DebugRowTemplate | DebugOptions.DebugAll)) > 0;

        internal XRect PopDrawingRect(bool updateRestored)
        {
            var result = DrawingRect;
            DrawingRect = history.Pop();
            if (updateRestored && result is not null)
            {
                UpdateDrawingRect(result.Value);
            }
            return result ?? XRect.Empty;
        }

        internal void PushDrawingRect(XRect xRect)
        {
            history.Push(DrawingRect);
            DrawingRect = xRect;
        }

        internal void UpdateDrawingRect(XPoint[] ptArray)
        {
            if (DrawingRect is null) return;
            foreach (var pt in ptArray)
            {
                DrawingRect.Value.Union(pt);
            }
        }

        internal void UpdateDrawingRect(double x, double y, double? w, double? h)
        {
            if (DrawingRect is null) return;
            var src = new XRect(x, y, w ?? 0, h ?? 0);
            var xREct = DrawingRect.Value;
            xREct.Union(src);
            DrawingRect= xREct;
        }

        internal void UpdateDrawingRect(XRect src)
        {
            if (DrawingRect is null) return;
            var xREct = DrawingRect.Value;
            xREct.Union(src);
            DrawingRect = xREct;
        }

        internal void UpdateDrawingRect(XPoint xPoint1, XPoint xPoint2)
        {
            if (DrawingRect is null) return;
            var src = new XRect(xPoint1, xPoint2);
            var xREct = DrawingRect.Value;
            xREct.Union(src);
            DrawingRect = xREct;
        }
    }
}