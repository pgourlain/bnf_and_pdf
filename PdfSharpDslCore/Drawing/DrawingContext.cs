using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace PdfSharpDslCore.Drawing
{
    public class DrawingContext
    {
        private readonly InstructionsRecorder _recorder;
        private readonly Stack<XGraphics> _previousGraphics = new();

        Stack<XRect?> history = new Stack<XRect?>();
        internal XRect? DrawingRect { get; private set; }
        public int Level => _previousGraphics.Count;

        public DebugOptions DebugOptions { get; set; }
        public bool DebugText => (DebugOptions & (DebugOptions.DebugText | DebugOptions.DebugAll)) > 0;
        public bool DebugRowTemplate => (DebugOptions & (DebugOptions.DebugRowTemplate | DebugOptions.DebugAll)) > 0;

        public DrawingContext(ILogger? logger)
        {
            _recorder = new(logger);
        }
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

        public void OpenBlock(string name, double offsetY, XGraphics previousGraphics, double newPageTopMargin)
        {
            _previousGraphics.Push(previousGraphics);
            _recorder.OpenBlock(name, offsetY, true, newPageTopMargin);
        }

        public XRect BlockRect => _recorder.CurrentBlock.Rect;
        internal (IInstructionBlock, XGraphics) RestoreGraphics()
        {
            var block = _recorder.CurrentBlock;

            return (block, _previousGraphics.Pop());
        }

        internal void CloseBlock()
        {
            _recorder.CloseBlock();
        }
        public void PushInstruction(Action action, XRect rect)
        {
            _recorder.CurrentBlock.PushInstruction(new InstructionAction(action, rect));
        }

        public void PushInstruction(Action action, XPoint[] ptArray)
        {
            var r = XRect.Empty;
            foreach (var pt in ptArray)   
            {
                r.Union(pt);
            }
            PushInstruction(action, r);
        }
    }
}