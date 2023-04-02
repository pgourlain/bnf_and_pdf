using System.Diagnostics;
using PdfSharpCore.Drawing;

namespace PdfSharpDslCore.Drawing
{
    [DebuggerDisplay("Y:{OffsetY}, OffsetRect:{OffsetRect}, Rect:{Instruction.Rect}")]
    internal class FlatInstruction
    {
        public FlatInstruction(IInstruction instr, double offsetY)
        {
            this.Instruction = instr;
            this.OffsetY = offsetY;
            var r = instr.Rect;
            r.Offset(0,offsetY);
            this.OffsetRect = r;
        }

        public IInstruction Instruction { get; set; }

        public double OffsetY { get; set; }
        public XRect OffsetRect { get; set; }
    }
}