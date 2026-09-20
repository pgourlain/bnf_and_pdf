using Moq;
using PdfSharpDslCore.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony;
using Microsoft.Extensions.Logging;
using Xunit;

namespace pdfsharpdslTests.ReplayerTests
{
    [ExcludeFromCodeCoverage]
    public class InstructionRecorderTests
    {
        #region private classes
        class DummyInstruction : IInstruction
        {
            public DummyInstruction(PdfRect r, string name="")
            {
                this.Rect = r;
                Name = name;
            }

            public PdfRect Rect { get; }
            public string Name { get; }
            public PdfRect DrawingRect { get; private set; }

            public double Draw(IPdfDocumentDrawer drawer, double offsetY, double pageOffsetY)
            {
                var r = Rect;
                r.Offset(0,offsetY);
                DrawingRect = r;
                return 0;
            }
        }
        #endregion

        private Mock<IPdfDocumentDrawer> defaultDrawerMock()
        {
            var drawerMock = new Mock<IPdfDocumentDrawer>();
            drawerMock.Setup(x => x.PageHeight).Returns(297);
            drawerMock.Setup(x => x.PageWidth).Returns(210);
            return drawerMock;
        }

        [Fact]
        public void RecorderTests_Level1()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new BlocksRecorder();

            var block = recorder.OpenBlock(string.Empty,0, true,0);
            Assert.NotNull(block);

            var r = new PdfRect(0, 0, 50, 50);
            var r1 = new PdfRect(150, 150, 50, 50);

            block.PushInstruction(new DummyInstruction(r));

            Assert.Equal(r, block.Rect);
            block.PushInstruction(new DummyInstruction(r1));
            r.Union(r1);
            Assert.Equal(r, block.Rect);

            var hasNewPage = block.Draw(drawerMock.Object, 0,0);
            Assert.Equal(0,hasNewPage);

            block.PushInstruction(new DummyInstruction(new PdfRect(0, 200, 10, 100)));
            hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            Assert.True(hasNewPage > 0);
            drawerMock.Verify(x => x.NewPage(null, null), Times.Once);
            recorder.CloseBlock();
            
            //draw at bottom page
            block = recorder.OpenBlock(string.Empty,200, true,0);
            var instr = new DummyInstruction(new PdfRect(0, 0, 50, 100));
            block.PushInstruction(instr);
            hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            Assert.True(hasNewPage > 0);
            Assert.Equal(new PdfRect(0,0,50,100), instr.DrawingRect);
            recorder.CloseBlock();
            

        }

        [Fact]
        public void DrawAtBottomPage()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new BlocksRecorder();
            //draw at bottom page
            var block = recorder.OpenBlock(string.Empty,200, true, 0);
            var instr = new DummyInstruction(new PdfRect(0, 0, 50, 100));
            block.PushInstruction(instr);
            var hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            
            drawerMock.Verify(x => x.NewPage(null, null), Times.Once);
            //drawerMock.Verify(x => x.SetOffsetY(200), Times.Once);
            //drawerMock.Verify(x => x.SetOffsetY(-97), Times.Once);

            Assert.True(hasNewPage > 0);
            recorder.CloseBlock();


        }

        [Fact]
        public void RecorderTests_Level1_multiplePage()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new BlocksRecorder();

            var block = recorder.OpenBlock(string.Empty,0, true);
            Assert.NotNull(block);

            AddInstructions(block, 10,60);
            var hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            Assert.True(hasNewPage > 0);
            //2 newPage() == 3 pages in total
            drawerMock.Verify(x => x.NewPage(null, null), Times.Exactly(2));
        }

        [Fact]
        public void RecorderTests_block_with_offsetY()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new BlocksRecorder();

            var block = recorder.OpenBlock(string.Empty,200, true,0);
            AddInstructions(block, 1, 100);
            
            Assert.Equal(new PdfRect(0,200, 50, 100), block.Rect);
            
            recorder.CloseBlock();
            block = recorder.OpenBlock(string.Empty,100, true);
            var block1 = recorder.OpenBlock(string.Empty,100, true);
            AddInstructions(block1, 1, 100);
            Assert.Equal(new PdfRect(0,200, 50, 100), block.Rect);
            Assert.Equal(new PdfRect(0,100, 50, 100), block1.Rect);
        }

        [Fact]
        public void InstructionActionExecutesWithOffsetAndExposesMetadata()
        {
            var rectangle = new PdfRect(1, 2, 3, 4);
            double? appliedOffset = null;
            var instruction = new InstructionAction(offset => appliedOffset = offset, rectangle, "action");

            var result = instruction.Draw(defaultDrawerMock().Object, 12, 99);

            Assert.Equal(rectangle, instruction.Rect);
            Assert.Equal("action", instruction.Name);
            Assert.Equal(12, appliedOffset);
            Assert.Equal(0, result);
        }

        [Fact]
        public void RecorderRootRejectsInstructionsAndBlockMetadataCanBeCleared()
        {
            var recorder = new BlocksRecorder();
            var instruction = new DummyInstruction(new PdfRect(0, 0, 10, 10));

            Assert.False(recorder.CanPushInstruction);
            Assert.Throws<NotSupportedException>(() => recorder.CurrentBlock.PushInstruction(instruction));

            var block = recorder.OpenBlock("named", 15, false, 4);
            block.PushInstruction(instruction, false);

            Assert.True(recorder.CanPushInstruction);
            Assert.Equal("named", block.Name);
            Assert.Equal(15, block.OffsetY);
            Assert.NotNull(block.Parent);
            Assert.True(block.Rect.IsEmpty);
            Assert.Single(block.Instructions);

            block.Clear();
            Assert.Empty(block.Instructions);
        }

        [Fact]
        public void NestedBlockMovesToNextPage()
        {
            var logger = new Mock<ILogger>();
            logger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
            var drawer = defaultDrawerMock();
            var recorder = new BlocksRecorder(logger.Object);
            var outer = recorder.OpenBlock("outer", 0, false);
            var child = outer.OpenBlock("child", 250, true);
            var instruction = new DummyInstruction(new PdfRect(0, 0, 50, 50));
            child.PushInstruction(instruction);

            var pageOffset = outer.Draw(drawer.Object, 0, 0);

            drawer.Verify(x => x.NewPage(null, null), Times.Once);
            Assert.Equal(new PdfRect(0, 0, 50, 50), instruction.DrawingRect);
            Assert.True(pageOffset > 0);
        }

        [Fact]
        public void OversizedNestedBlockCannotBePrintedEntirely()
        {
            var drawer = defaultDrawerMock();
            var recorder = new BlocksRecorder();
            var outer = recorder.OpenBlock("outer", 0, false);
            var child = outer.OpenBlock("child", 0, true);
            child.PushInstruction(new DummyInstruction(new PdfRect(0, 0, 50, 400)));

            Assert.Throws<NotImplementedException>(() => outer.Draw(drawer.Object, 0, 0));
        }

        [Fact]
        public void InstructionsCannotBeAddedWhileBlockIsDrawing()
        {
            var drawer = defaultDrawerMock();
            var recorder = new BlocksRecorder();
            var block = recorder.OpenBlock("block", 0, true);
            var extraInstruction = new DummyInstruction(new PdfRect(0, 20, 10, 10));
            block.PushInstruction(new InstructionAction(
                _ => block.PushInstruction(extraInstruction),
                new PdfRect(0, 0, 10, 10),
                "mutating"));

            block.Draw(drawer.Object, 0, 0);

            Assert.Single(block.Instructions);
        }

        private static void AddInstructions(IInstructionBlock block, int count, int height)
        {
            var r = new PdfRect(0, 0, 50, height);
            for (var i = 0; i < count; i++)
            {

                block.PushInstruction(new DummyInstruction(r));
                r.Offset(0, r.Height + 1);
            }
        }

        // [Fact]
        // public void RecorderTests_Level2()
        // {
        //     var drawerMock = defaultDrawerMock();
        //
        //     var recorder = new BlocksRecorder();
        //
        //     var block = recorder.OpenBlock(string.Empty,0, true);
        //
        //     var b2 = recorder.OpenBlock(string.Empty,200, true);
        //     //180 height should be print on second page
        //     AddInstructions(b2, 3, 60);
        //     var hasNewPage = block.Draw(drawerMock.Object, 0, 0);
        //     Assert.True(hasNewPage>0);
        //     drawerMock.Verify(x => x.NewPage(null, null), Times.Exactly(1));
        // }
    }
}
