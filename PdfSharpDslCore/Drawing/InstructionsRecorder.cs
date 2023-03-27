using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PdfSharpDslCore.Drawing
{

    class RootInstructionBlock : InstructionBlock
    {
        public RootInstructionBlock(ILogger? logger)
            : base (null!, false, 0,0)
        {
            this.Logger = logger;
        }

        public override void PushInstruction(IInstruction instruction)
        {
            //base.PushInstruction(instruction);
        }
    }
    internal class InstructionsRecorder
    {
        private readonly IInstructionBlock _rootBlock;
        public InstructionsRecorder(ILogger? logger=null)
        {
            _rootBlock = new RootInstructionBlock(logger);
            CurrentBlock = _rootBlock;
        }

        public IInstructionBlock OpenBlock(double offsetY, bool entirePrint, double newPageTopMargin=0)
        {
            CurrentBlock = CurrentBlock.OpenBlock(offsetY, entirePrint, newPageTopMargin);
            return CurrentBlock;
        }

        public void CloseBlock()
        {
            if (CurrentBlock != _rootBlock)
            {
                CurrentBlock = CurrentBlock.Parent ?? _rootBlock;
            }

            if (_rootBlock == CurrentBlock)
            {
                _rootBlock.Clear();
            }
        }

        public IInstructionBlock CurrentBlock { get; private set; }
    }
}
