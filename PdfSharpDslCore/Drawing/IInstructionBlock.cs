using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;

namespace PdfSharpDslCore.Drawing
{
    internal interface IInstruction
    {
        XRect Rect { get; }

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
        void PushInstruction(IInstruction instruction);

        IInstruction PopInstruction();

        IInstructionBlock OpenBlock(double offsetY, bool entirePrint, double newPageTopMargin=0);
        void CloseBlock();
        void UpdateRect(XRect rect);

        void Clear();

        IInstructionBlock? Parent { get; }
        double OffsetY { get; }
    }

    internal record DrawResult
    {
        public XRect DrawingRect;
        public double PageOffsetY;
    }
}