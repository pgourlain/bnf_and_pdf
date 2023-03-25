using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharpCore.Drawing;

namespace PdfSharpDslCore.Drawing
{
    class InstructionAction : IInstruction
    {
        public XRect Rect { get; }
        private readonly Action _action;

        public InstructionAction(Action action, XRect rect)
        {
            this.Rect = rect;
            _action = action;
        }
        public bool Draw(IPdfDocumentDrawer drawer, double offsetY)
        {
            _action();
            return false;
        }
    }
    
    class InstructionBlock : IInstructionBlock
    {
        private readonly List<IInstruction> _instructions = new List<IInstruction>();
        private readonly IInstructionBlock? _parent;
        private readonly double _offsetY;
        private int _inDraw = 0;

        public bool ShouldBeEntirePrinted { get; }

        public XRect Rect { get; private set; } = XRect.Empty;

        public IInstructionBlock? Parent => _parent;
        public double OffsetY => _offsetY;

        public InstructionBlock(IInstructionBlock? parent, bool entirePrint, double offsetY)
        {
            this._parent = parent;
            ShouldBeEntirePrinted = entirePrint;
            this._offsetY = offsetY;
        }

        public void Clear()
        {
            _instructions.Clear();
        }

        /// <summary>
        /// return true if new page occurs
        /// </summary>
        /// <param name="drawer"></param>
        /// <param name="offsetY"/>
        /// <returns></returns>
        public bool Draw(IPdfDocumentDrawer drawer, double offsetY)
        {
            var hasNewPage = false;
            _inDraw++;
            try
            {
                var selfOffsetY = _offsetY;
                
                XRect pageRect = new XRect(0, 0, drawer.PageWidth, drawer.PageHeight);
                //2 cas : l'instruction ne rentre pas dans la page actuelle, il faut une nouvelle page
                // ça ne rentre dans aucune page, il faut "imprimé" par morceau
                if (ShouldBeEntirePrinted)
                {
                    //la hauteur ne rentre pas sur une page
                    if (this.Rect.Height > pageRect.Height)
                    {
                        hasNewPage = DrawByChunck(drawer, offsetY, selfOffsetY, pageRect) | hasNewPage;
                    }
                    else
                    {
                        //le bas dépasse la page
                        if (this.Rect.Bottom> pageRect.Height - offsetY)
                        {
                            hasNewPage = true;
                            drawer.NewPage();
                            //offset of each block should set to 0
                            hasNewPage = DrawInstructions(drawer, offsetY, true) | hasNewPage;
                        }
                        else
                        {
                            hasNewPage = DrawInstructions(drawer, selfOffsetY+ offsetY) | hasNewPage;
                        }
                    }
                }
                else
                {
                    hasNewPage = DrawByChunck(drawer, offsetY, selfOffsetY, pageRect) | hasNewPage;
                }
            }
            finally
            {
                _inDraw--;
            }
            return hasNewPage;
        }

        private bool DrawByChunck(IPdfDocumentDrawer drawer, double offsetY, double selfOffsetY, XRect pageRect)
        {
            var hasNewPage = false;
            var h = this.Rect.Bottom;
            while (h > 0)
            {
                hasNewPage = DrawInstructions(drawer, selfOffsetY + offsetY) | hasNewPage;
                h = h - pageRect.Height;
                if (h > 0)
                {
                    drawer.NewPage();
                    hasNewPage = true;
                    selfOffsetY -= pageRect.Height;
                    offsetY = 0;
                }
            }

            return hasNewPage;
        }

        private bool DrawInstructions(IPdfDocumentDrawer drawer, double offsetY, bool newPageOccurs = false)
        {
            var shouldRestore = offsetY > 0;
            if (shouldRestore)
            {
                drawer.SetOffsetY(offsetY);
            }
            try
            {
                var hasNewPage = false;
                foreach (var item in _instructions)
                {
                    var negOffset = (item is IInstructionBlock block) && newPageOccurs ? block.OffsetY : 0;
                    var newPage = item.Draw(drawer, offsetY - negOffset);
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
                if (shouldRestore)
                {
                    drawer.ResetOffset();
                }
            }
        }

        public IInstruction PopInstruction()
        {
            var result = _instructions.First();
            _instructions.RemoveAt(0);
            return result;
        }

        public virtual void PushInstruction(IInstruction instruction)
        {
            //instruction at root is not permit
            if (_inDraw > 0) return;
            if (instruction == null) throw new ArgumentNullException(nameof(instruction));

            _instructions.Add(instruction);
            this.UpdateRect(instruction.Rect);
        }

        public IInstructionBlock OpenBlock(double offsetY, bool entirePrint)
        {
            var result = new InstructionBlock(this, entirePrint, offsetY);
            _instructions.Add(result);
            return result;
        }

        public void CloseBlock()
        {
            //
        }

        public void UpdateRect(XRect rect)
        {
            if (rect.IsEmpty) return;
            rect.Offset(0, _offsetY);
            var r = this.Rect;
            r.Union(rect);
            Rect = r;
            if (_parent != null)
            {
                //update parent with current offset
                _parent.UpdateRect(r);
            }
        }
    }
}