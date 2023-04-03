using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Extensions;

namespace PdfSharpDslCore.Drawing
{
    [DebuggerDisplay("Rect:{Rect}")]
    class InstructionAction : IInstruction
    {
        public XRect Rect { get; }
        private readonly Action<double> _action;
        public string Name { get; }

        public InstructionAction(Action<double> action, XRect rect, string name)
        {
            this.Rect = rect;
            _action = action;
            Name = name;
        }
        public double Draw(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY)
        {
            _action(offsetY);
            return 0;
        }
    }

    [DebuggerDisplay("Name:{Name}, Y:{OffsetY}, childCount:{_instructions.Count}, rect:{Rect}")]
    class InstructionBlock : IInstructionBlock
    {
        private readonly List<IInstruction> _instructions = new List<IInstruction>();
        private readonly IInstructionBlock? _parent;
        private readonly string _name;
        private readonly double _offsetY;
        private int _inDraw = 0;
        private readonly double _newPageTopMargin;

        public bool ShouldBeEntirePrinted { get; }

        public XRect Rect { get; private set; } = XRect.Empty;

        public IInstructionBlock? Parent => _parent;
        public double OffsetY => _offsetY;

        public ILogger? Logger { get; protected set; }
        public string Name => _name;

        public IEnumerable<IInstruction> Instructions => _instructions.AsReadOnly();

        public InstructionBlock(IInstructionBlock? parent, string name, bool entirePrint, double offsetY, double newPageTopMargin)
        {
            this._parent = parent;
            _name = name;
            ShouldBeEntirePrinted = entirePrint;
            this._offsetY = offsetY;
            this._newPageTopMargin = newPageTopMargin;
            Logger = parent?.Logger;
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
        public double Draw(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY)
        {
            double newPageOffsetY = 0;
            if (Logger.DebugEnabled())
            {
                Logger.WriteDebug(this, $"BeginDraw({offsetY})");
            }
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
                        if (LogEnabled(LogLevel.Debug))
                        {
                            Logger.WriteDebug(this, "drawing height > page height");
                        }
                        newPageOffsetY = DrawByChunck(drawer, offsetY, selfOffsetY, pageOffsetY, pageRect);
                    }
                    else
                    {
                        //le bas dépasse la page
                        if ((this.Rect.Bottom - pageOffsetY) > pageRect.Height - offsetY)
                        {
                            if (LogEnabled(LogLevel.Debug))
                            {
                                Logger.WriteDebug(this, "drawing bottom is under page bottom");
                            }
                            newPageOffsetY = drawer.PageHeight;
                            drawer.NewPage();
                            //offset of each block should set to 0
                            newPageOffsetY += DrawInstructions(drawer, offsetY, pageOffsetY, true);
                        }
                        else
                        {
                            newPageOffsetY = DrawInstructions(drawer, selfOffsetY + offsetY, pageOffsetY);
                        }
                    }
                }
                else
                {
                    newPageOffsetY = DrawByChunck(drawer, offsetY, selfOffsetY, pageOffsetY, pageRect);
                }
            }
            finally
            {
                _inDraw--;
            }
            return newPageOffsetY;
        }

        private double DrawByChunck(IPdfDocumentDrawer drawer, double offsetY, double selfOffsetY, double pageOffsetY, XRect pageRect)
        {
            double newPageOffsetY = 0;
            if (LogEnabled(LogLevel.Debug))
            {
                Logger.WriteDebug(this, $"DrawByChunck({offsetY},{selfOffsetY},{pageOffsetY})");
            }

            newPageOffsetY = DrawInstructionsByChunk(drawer, _instructions, selfOffsetY, pageOffsetY);

            newPageOffsetY = this.Rect.Bottom - newPageOffsetY;
            return newPageOffsetY;
        }

        private double DrawInstructionsByChunk(IPdfDocumentDrawer drawer, IEnumerable<IInstruction> instructions,
            double originalOffsetY, double pageOffsetY)
        {
            var currentOffsetY = originalOffsetY;
            var hasNewPage = false;
            var offsetyResult = 0.0;
            var deltaOnPreviousPage = 0.0;
            foreach (var instr in instructions)
            {
                if (instr is IInstructionBlock block)
                {
                    if (block.Rect.Height + block.OffsetY - pageOffsetY + currentOffsetY > drawer.PageHeight)
                    {
                        //the block does not fit in the page
                        if (block.Rect.Height <= drawer.PageHeight)
                        {
                            drawer.NewPage();
                            hasNewPage = true;
                            currentOffsetY = 0;
                            pageOffsetY += drawer.PageHeight;
                            var newY = block.OffsetY - pageOffsetY + originalOffsetY;
                            if (newY < 0)
                            {
                                currentOffsetY = -newY;
                                deltaOnPreviousPage += currentOffsetY;
                            }
                            DrawInstructionsByChunk(drawer, block.Instructions, 0, pageOffsetY);
                            //drawer.ResetOffset();
                            offsetyResult = block.Rect.Height + currentOffsetY;
                        }
                        else
                        {
                            //si ne rentre pas du tout, on imprime , puis on créé une page et on réimprime avec un décalage
                            throw new NotImplementedException("Block higher than one page");
                        }

                    }
                    else
                    {
                        var newY = block.OffsetY - pageOffsetY + originalOffsetY + (hasNewPage ? currentOffsetY : 0);
                        DrawInstructionsByChunk(drawer, block.Instructions, newY, pageOffsetY /*0?*/);
                        offsetyResult = Math.Max(offsetyResult, block.Rect.Bottom - pageOffsetY + originalOffsetY);
                    }
                }
                else
                {
                    instr.Draw(drawer, currentOffsetY, 0);
                }
            }

            return offsetyResult + deltaOnPreviousPage;
        }

        private IEnumerable<FlatInstruction> GenerateInstruction(IEnumerable<IInstruction> instructions, double offsetY)
        {
            foreach (var instr in instructions.ToArray())
            {
                if (instr is IInstructionBlock block)
                {
                    foreach (var flatInstr in GenerateInstruction(block.Instructions, block.OffsetY))
                    {
                        flatInstr.OffsetY += offsetY;
                        yield return flatInstr;
                    }
                }
                else
                {
                    yield return new FlatInstruction(instr, offsetY);
                }
            }
        }

        private double DrawInstructions(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY, bool newPageOccurs = false, bool drawchunk = false)
        {
            double negOffset = 0;
            var shouldRestore = offsetY != 0;
            if (shouldRestore)
            {
                drawer.SetOffsetY(offsetY);
            }
            try
            {
                if (newPageOccurs)
                {
                    var block = _instructions.OfType<IInstructionBlock>().FirstOrDefault();
                    if (block != null)
                    {
                        negOffset = -block.OffsetY;
                    }
                }
                double newPageOffsetY = 0;
                foreach (var item in _instructions)
                {
                    //offsetY should not be add here because of SetOffsetY above
                    newPageOffsetY += item.Draw(drawer, negOffset, pageOffsetY);
                }
                return newPageOffsetY;
            }
            finally
            {
                if (shouldRestore)
                {
                    drawer.ResetOffset();
                }
            }
        }

        private double GlobalOffset(IInstructionBlock? block)
        {
            double result = 0;
            while (block != null)
            {
                result += block.OffsetY;
                block = block.Parent;
            }

            return result;
        }

        public virtual void PushInstruction(IInstruction instruction, bool accumulate)
        {
            //instruction at root is not permit
            if (_inDraw > 0) return;
            if (instruction == null) throw new ArgumentNullException(nameof(instruction));

            _instructions.Add(instruction);
            if (accumulate)
            {
                this.UpdateRect(instruction.Rect);
            }
        }

        public IInstructionBlock OpenBlock(string name, double offsetY, bool entirePrint, double newPageTopMargin = 0)
        {
            // if (LogEnabled(LogLevel.Debug))
            // {
            //     Logger?.LogDebug(LogFmt($"OpenBlock:{offsetY}"));
            // }
            var result = new InstructionBlock(this, name, entirePrint, offsetY, newPageTopMargin);
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

        private bool LogEnabled(LogLevel level)
        {
            return Logger?.IsEnabled(level) ?? false;
        }
    }
}