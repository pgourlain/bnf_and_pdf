using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Drawing
{

    class RootInstructionBlock : InstructionBlock
    {
        public RootInstructionBlock(IInstructionBlock? parent, bool entirePrint, double offsetY)
            : base (parent, entirePrint, offsetY)
        {
            
        }

        public override void PushInstruction(IInstruction instruction)
        {
            //base.PushInstruction(instruction);
        }
    }
    internal class InstructionsRecorder
    {
        readonly IInstructionBlock _rootBlock = new RootInstructionBlock(null!, false,0);
        public InstructionsRecorder()
        {
            CurrentBlock = _rootBlock;
        }

        public IInstructionBlock OpenBlock(double offsetY, bool entirePrint)
        {
            CurrentBlock = CurrentBlock.OpenBlock(offsetY, entirePrint);
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
