using Moq;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace pdfsharpdslTests.ReplayerTests
{
    [ExcludeFromCodeCoverage]
    public class InstructionRecorderTests
    {
        #region private classes
        class DummyInstruction : IInstruction
        {
            public DummyInstruction(XRect r) { this.Rect = r; }

            public XRect Rect { get; }
            public XRect DrawingRect { get; private set; }

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

            var recorder = new InstructionsRecorder();

            var block = recorder.OpenBlock(0, true,0);
            Assert.NotNull(block);

            var r = new XRect(0, 0, 50, 50);
            var r1 = new XRect(150, 150, 50, 50);

            block.PushInstruction(new DummyInstruction(r));

            Assert.Equal(r, block.Rect);
            block.PushInstruction(new DummyInstruction(r1));
            r.Union(r1);
            Assert.Equal(r, block.Rect);

            var hasNewPage = block.Draw(drawerMock.Object, 0,0);
            Assert.Equal(0,hasNewPage);

            block.PushInstruction(new DummyInstruction(new XRect(0, 200, 10, 100)));
            hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            Assert.True(hasNewPage > 0);
            drawerMock.Verify(x => x.NewPage(null, null), Times.Once);
            recorder.CloseBlock();
            
            //draw at bottom page
            block = recorder.OpenBlock(200, true,0);
            var instr = new DummyInstruction(new XRect(0, 0, 50, 100));
            block.PushInstruction(instr);
            hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            Assert.True(hasNewPage > 0);
            Assert.Equal(new XRect(0,0,50,100), instr.DrawingRect);
            recorder.CloseBlock();
            

        }

        [Fact]
        public void DrawAtBottomPage()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new InstructionsRecorder();
            //draw at bottom page
            var block = recorder.OpenBlock(200, false, 0);
            var instr = new DummyInstruction(new XRect(0, 0, 50, 100));
            block.PushInstruction(instr);
            var hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            
            drawerMock.Verify(x => x.NewPage(null, null), Times.Once);
            drawerMock.Verify(x => x.SetOffsetY(200), Times.Once);
            drawerMock.Verify(x => x.SetOffsetY(-97), Times.Once);

            Assert.True(hasNewPage > 0);
            recorder.CloseBlock();


        }

        [Fact]
        public void RecorderTests_Level1_multiplePage()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new InstructionsRecorder();

            var block = recorder.OpenBlock(0, true);
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

            var recorder = new InstructionsRecorder();

            var block = recorder.OpenBlock(200, true,0);
            AddInstructions(block, 1, 100);
            
            Assert.Equal(new XRect(0,200, 50, 100), block.Rect);
            
            recorder.CloseBlock();
            block = recorder.OpenBlock(100, true);
            var block1 = recorder.OpenBlock(100, true);
            AddInstructions(block1, 1, 100);
            Assert.Equal(new XRect(0,200, 50, 100), block.Rect);
            Assert.Equal(new XRect(0,100, 50, 100), block1.Rect);
        }

        private static void AddInstructions(IInstructionBlock block, int count, int height)
        {
            var r = new XRect(0, 0, 50, height);
            for (var i = 0; i < count; i++)
            {

                block.PushInstruction(new DummyInstruction(r));
                r.Offset(0, r.Height + 1);
            }
        }

        [Fact]
        public void RecorderTests_Level2()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new InstructionsRecorder();

            var block = recorder.OpenBlock(0, true);

            var b2 = recorder.OpenBlock(200, true);
            //180 height should be print on second page
            AddInstructions(b2, 3, 60);
            var hasNewPage = block.Draw(drawerMock.Object, 0, 0);
            Assert.True(hasNewPage>0);
            drawerMock.Verify(x => x.NewPage(null, null), Times.Exactly(1));
        }
    }
}
