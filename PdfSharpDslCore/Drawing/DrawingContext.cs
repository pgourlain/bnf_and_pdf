using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using Irony;
using Microsoft.Extensions.Logging;

namespace PdfSharpDslCore.Drawing
{
    internal class DrawingContext
    {
        private readonly BlocksRecorder _recorder;
        private readonly Stack<XGraphics> _previousGraphics = new();
        public int Level => _previousGraphics.Count;

        public DebugOptions DebugOptions { get; set; }
        public bool DebugText => (DebugOptions & (DebugOptions.DebugText | DebugOptions.DebugAll)) > 0;
        public bool DebugRowTemplate => (DebugOptions & (DebugOptions.DebugRowTemplate | DebugOptions.DebugAll)) > 0;

        public DrawingContext(ILogger? logger)
        {
            _recorder = new(logger);
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
        
        public void PushInstruction(Action<double> action, XRect rect, bool accumulate=true, string instrName="")
        {
            if (_recorder.CanPushInstruction)
            {
                //only call pushinstruction if an openblock was called
                _recorder.CurrentBlock.PushInstruction(new InstructionAction(action, rect, instrName), accumulate);
            }
        }

        public void PushInstruction(Action<double> action, XPoint[] ptArray)
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