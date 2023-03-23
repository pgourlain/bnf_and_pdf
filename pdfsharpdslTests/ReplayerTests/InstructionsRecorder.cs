using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests.ReplayerTests
{

    internal interface IInstruction
    {
        XRect Rect { get; }

        bool Draw(IPdfDocumentDrawer drawer, double offsetY);
    }

    internal interface IInstructionBlock : IInstruction
    {
        /// <summary>
        /// want to print entire if possible (height < pageheight)
        /// </summary>
        bool ShouldBeEntirePrinted { get; }
        void PushInstruction(IInstruction instruction);

        IInstruction PopInstruction();

        IInstructionBlock OpenBlock(bool entirePrint);
        void UpdateRect(XRect rect);

        IInstructionBlock Parent { get; }
    }

    internal class InstructionsRecorder
    {
        IInstructionBlock rootBlock = new InstructionBlock(null!, false,0);
        public InstructionsRecorder()
        {
            CurrentBlock = rootBlock;
        }

        public IInstructionBlock OpenBlock(double offsetY, bool entirePrint)
        {
            CurrentBlock = rootBlock.OpenBlock(entirePrint);
            return CurrentBlock;
        }

        public void CloseBlock()
        {
            if (CurrentBlock != null && CurrentBlock != rootBlock)
            {
                CurrentBlock = CurrentBlock.Parent;
            }
        }

        public IInstructionBlock CurrentBlock { get; private set; }
    }

    class InstructionBlock : IInstructionBlock
    {
        List<IInstruction> instructions = new List<IInstruction>();
        private readonly IInstructionBlock _parent;
        private readonly double _offsetY;
        private bool inDraw = false;

        public bool ShouldBeEntirePrinted { get; }

        public XRect Rect { get; private set; }

        public IInstructionBlock Parent => _parent;

        public InstructionBlock(IInstructionBlock parent, bool entirePrint, double offsetY)
        {
            this._parent = parent;
            ShouldBeEntirePrinted = entirePrint;
            this._offsetY = offsetY;
        }

        /// <summary>
        /// return true if new page occurs
        /// </summary>
        /// <param name="drawer"></param>
        /// <returns></returns>
        public bool Draw(IPdfDocumentDrawer drawer, double offsetY)
        {
            var hasNewPage = false;
            inDraw = true;
            try
            {
                var selfOffsetY = _offsetY;
                //2 cas : l'instruction ne rentre pas dans la page actuelle, il faut une nouvelle page
                // ça ne rentre dans aucune page, il faut "imprimé" par morceau
                if (ShouldBeEntirePrinted)
                {
                    XRect rect = new XRect(0, 0, drawer.PageWidth, drawer.PageHeight);
                    if (this.Rect.Height + selfOffsetY > rect.Height)
                    {
                        var h = this.Rect.Height;
                        while (h > 0)
                        {
                            hasNewPage = DrawInstructions(drawer, selfOffsetY+offsetY) | hasNewPage;
                            h = h - rect.Height;
                            if (h > 0)
                            {
                                drawer.NewPage();
                                hasNewPage = true;
                                selfOffsetY = 0;
                            }
                            offsetY = 0;
                        }
                    }
                    else
                    {
                        if (this.Rect.Height+ selfOffsetY > rect.Height - offsetY)
                        {
                            hasNewPage = true;
                            drawer.NewPage();
                            offsetY = 0;
                            selfOffsetY = 0;
                            hasNewPage = DrawInstructions(drawer, offsetY) | hasNewPage;
                        }
                        else
                        {
                            hasNewPage = DrawInstructions(drawer, offsetY) | hasNewPage;
                        }
                    }
                }
                else
                {
                    hasNewPage = DrawInstructions(drawer, offsetY) | hasNewPage;
                }
            }
            finally
            {
                inDraw = false;
            }
            return hasNewPage;
        }

        private bool DrawInstructions(IPdfDocumentDrawer drawer, double offsetY)
        {
            if (offsetY > 0)
            {
                drawer.SetOffsetY(offsetY);
            }
            try
            {
                var hasNewPage = false;
                foreach (var item in instructions)
                {
                    var newPage = item.Draw(drawer, offsetY);
                    hasNewPage |= newPage;
                    if (newPage)
                    {
                        offsetY = 0;//should be set to margin top
                    }
                    else
                    {
                        offsetY += item.Rect.Height;
                    }
                }
                return hasNewPage;
            }
            finally
            {
                if (offsetY > 0)
                {
                    drawer.ResetOffset();
                }
            }
        }

        public IInstruction PopInstruction()
        {
            var result = instructions.First();
            instructions.RemoveAt(0);
            return result;
        }

        public void PushInstruction(IInstruction instruction)
        {
            if (inDraw) return;
            if (instruction == null) throw new ArgumentNullException(nameof(instruction));

            instructions.Add(instruction);
            this.UpdateRect(instruction.Rect);
        }

        public IInstructionBlock OpenBlock(bool entirePrint)
        {
            var result = new InstructionBlock(this, entirePrint, 0);
            instructions.Add(result);
            return result;
        }

        public void UpdateRect(XRect rect)
        {
            var r = this.Rect;
            r.Union(rect);
            Rect = r;
            if (_parent != null)
            {
                _parent.UpdateRect(Rect);
            }
        }
    }
}
