using System;
using System.Collections.Generic;
using Irony;
using Microsoft.Extensions.Logging;

namespace PdfSharpDslCore.Drawing
{
    internal class DrawingContext
    {
        private readonly BlocksRecorder _recorder;
        private int _level;
        public int Level => _level;

        public DebugOptions DebugOptions { get; set; }
        public bool DebugText => (DebugOptions & (DebugOptions.DebugText | DebugOptions.DebugAll)) > 0;
        public bool DebugRowTemplate => (DebugOptions & (DebugOptions.DebugRowTemplate | DebugOptions.DebugAll)) > 0;

        public DrawingContext(ILogger? logger)
        {
            _recorder = new(logger);
        }
        
        public void OpenBlock(string name, double offsetY, double newPageTopMargin)
        {
            _level++;
            _recorder.OpenBlock(name, offsetY, true, newPageTopMargin);
        }

        public PdfRect BlockRect => _recorder.CurrentBlock.Rect;
        internal IInstructionBlock EndMeasure()
        {
            var block = _recorder.CurrentBlock;
            _level--;
            return block;
        }

        internal void CloseBlock()
        {
            _recorder.CloseBlock();
        }
        
        public void PushInstruction(Action<double> action, PdfRect rect, bool accumulate=true, string instrName="")
        {
            if (_recorder.CanPushInstruction)
            {
                //only call pushinstruction if an openblock was called
                _recorder.CurrentBlock.PushInstruction(new InstructionAction(action, rect, instrName), accumulate);
            }
        }

        public void PushInstruction(Action<double> action, PdfPoint[] ptArray)
        {
            var r = PdfRect.Empty;
            foreach (var pt in ptArray)   
            {
                r.Union(pt);
            }
            PushInstruction(action, r);
        }
    }
}