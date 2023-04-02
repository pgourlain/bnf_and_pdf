using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;

namespace PdfSharpDslCore.Drawing
{
    internal interface IInstruction
    {
        XRect Rect { get; }
        string Name { get; }

        /// <summary>
        /// return pageOffsetY
        /// </summary>
        /// <param name="drawer"></param>
        /// <param name="offsetY"></param>
        /// <param name="pageOffsetY"></param>
        /// <returns></returns>
        double Draw(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY);
    }
    
    internal interface IInstructionBlock : IInstruction
    {
        ILogger? Logger { get; }

        /// <summary>
        /// want to print entire if possible (height < pageheight)
        /// </summary>
        bool ShouldBeEntirePrinted { get; }
        void PushInstruction(IInstruction instruction, bool accumulate=true);

        IEnumerable<IInstruction> Instructions { get; }

        IInstructionBlock OpenBlock(string name, double offsetY, bool entirePrint, double newPageTopMargin=0);
        void CloseBlock();
        void UpdateRect(XRect rect);

        void Clear();

        IInstructionBlock? Parent { get; }
        double OffsetY { get; }
        string Name { get; }
    }

    internal record DrawResult
    {
        public XRect DrawingRect;
        public double PageOffsetY;
    }
}