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
        private readonly Action _action;

        public InstructionAction(Action action, XRect rect)
        {
            this.Rect = rect;
            _action = action;
        }
        public double Draw(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY)
        {
            _action();
            return 0;
        }
    }

    [DebuggerDisplay("Y:{OffsetY}, childCount:{_instructions.Count}, rect:{Rect}")]
    class InstructionBlock : IInstructionBlock
    {
        private readonly List<IInstruction> _instructions = new List<IInstruction>();
        private readonly IInstructionBlock? _parent;
        private readonly double _offsetY;
        private int _inDraw = 0;
        private readonly double _newPageTopMargin;

        public bool ShouldBeEntirePrinted { get; }

        public XRect Rect { get; private set; } = XRect.Empty;

        public IInstructionBlock? Parent => _parent;
        public double OffsetY => _offsetY;

        public ILogger? Logger { get; protected set; }

        public InstructionBlock(IInstructionBlock? parent, bool entirePrint, double offsetY, double newPageTopMargin)
        {
            this._parent = parent;
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
                        if ((this.Rect.Bottom-pageOffsetY) > pageRect.Height - offsetY)
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
            if (LogEnabled(LogLevel.Debug))
            {
                Logger.WriteDebug(this, $"DrawByChunck({offsetY},{selfOffsetY},{pageOffsetY})");
            }
            double newPageOffsetY = 0;
            var h = this.Rect.Bottom;
            var drawingOffset = selfOffsetY + offsetY;
            while (h > 0)
            {
                newPageOffsetY += DrawInstructions(drawer, drawingOffset, pageOffsetY);
                h = h - (pageRect.Height);
                if (h > 0)
                {
                    drawer.NewPage();
                    newPageOffsetY = pageRect.Height;
                    pageOffsetY += pageRect.Height;
                    drawingOffset -= pageRect.Height;
                }
            }

            return newPageOffsetY;
        }

        private double DrawInstructions(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY, bool newPageOccurs = false)
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
                    if (item is IInstructionBlock block)
                    {
                        if (block.OffsetY + offsetY > drawer.PageHeight)
                        {
                            //avoid drawing under bottom page
                            break;
                        }
                        if (block.OffsetY + block.Rect.Bottom + offsetY < 0)
                        {
                            //do not draw block < top page
                            continue;
                        }
                    }
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

        public IInstructionBlock OpenBlock(double offsetY, bool entirePrint, double newPageTopMargin = 0)
        {
            // if (LogEnabled(LogLevel.Debug))
            // {
            //     Logger?.LogDebug(LogFmt($"OpenBlock:{offsetY}"));
            // }
            var result = new InstructionBlock(this, entirePrint, offsetY, newPageTopMargin);
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