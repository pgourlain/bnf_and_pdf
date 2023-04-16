using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Extensions;

namespace PdfSharpDslCore.Drawing
{
    internal interface IInstruction
    {
        XRect Rect { get; }
        string Name { get; }

        /// <summary>
        /// draw the instruction
        /// </summary>
        /// <param name="drawer"></param>
        /// <param name="offsetY"></param>
        /// <param name="pageOffsetY"></param>
        /// <returns>return new pageOffsetY</returns>
        double Draw(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY);
    }
    
    internal interface IInstructionBlock : IInstruction
    {
        ILogger? Logger { get; }

        /// <summary>
        /// want to print entire if possible (height &lt; pageHeight)
        /// </summary>
        bool ShouldBeEntirePrinted { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="instruction"></param>
        /// <param name="accumulate">shoud update Rect of current block ?</param>
        void PushInstruction(IInstruction instruction, bool accumulate=true);

        /// <summary>
        /// instructions in this block
        /// </summary>
        IEnumerable<IInstruction> Instructions { get; }

        /// <summary>
        /// open a child block
        /// </summary>
        /// <param name="name"></param>
        /// <param name="offsetY"></param>
        /// <param name="entirePrint"></param>
        /// <param name="newPageTopMargin"></param>
        /// <returns></returns>
        IInstructionBlock OpenBlock(string name, double offsetY, bool entirePrint, double newPageTopMargin=0);
        void CloseBlock();
        void UpdateRect(XRect rect);

        void Clear();

        IInstructionBlock? Parent { get; }
        double OffsetY { get; }
    }
}