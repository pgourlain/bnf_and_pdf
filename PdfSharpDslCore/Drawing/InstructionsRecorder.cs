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
            : base (null!, "_root_", false, 0,0)
        {
            this.Logger = logger;
        }

        public override void PushInstruction(IInstruction instruction, bool accumulate)
        {
            throw new NotSupportedException("PushInstruction on root is not allowed");
        }
    }
    
    /// <summary>
    /// block recorder based on "tree"
    /// </summary>
    internal class BlocksRecorder
    {
        private readonly IInstructionBlock _rootBlock;
        public BlocksRecorder(ILogger? logger=null)
        {
            _rootBlock = new RootInstructionBlock(logger);
            CurrentBlock = _rootBlock;
        }

        public IInstructionBlock OpenBlock(string name, double offsetY, bool entirePrint, double newPageTopMargin=0)
        {
            CurrentBlock = CurrentBlock.OpenBlock(name, offsetY, entirePrint, newPageTopMargin);
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

        /// <summary>
        /// for optimisation to avoid calling PushInstruction
        /// </summary>
        public bool CanPushInstruction => CurrentBlock != _rootBlock;
       
    }
}
