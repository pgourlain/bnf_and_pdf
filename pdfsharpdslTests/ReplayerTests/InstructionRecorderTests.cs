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

            public bool Draw(IPdfDocumentDrawer drawer, double offsetY)
            {
                return false;
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

            var block = recorder.OpenBlock(0, true);
            Assert.NotNull(block);

            var r = new XRect(0, 0, 50, 50);
            var r1 = new XRect(150, 150, 50, 50);

            block.PushInstruction(new DummyInstruction(r));

            Assert.Equal(r, block.Rect);
            block.PushInstruction(new DummyInstruction(r1));
            r.Union(r1);
            Assert.Equal(r, block.Rect);

            var hasNewPage = block.Draw(drawerMock.Object, 0);
            Assert.False(hasNewPage);

            block.PushInstruction(new DummyInstruction(new XRect(0, 200, 10, 100)));
            hasNewPage = block.Draw(drawerMock.Object, 0);
            Assert.True(hasNewPage);
            drawerMock.Verify(x => x.NewPage(null, null), Times.Once);

        }

        [Fact]
        public void RecorderTests_Level1_multiplePage()
        {
            var drawerMock = defaultDrawerMock();

            var recorder = new InstructionsRecorder();

            var block = recorder.OpenBlock(0, true);
            Assert.NotNull(block);

            AddInstructions(block, 10,60);
            var hasNewPage = block.Draw(drawerMock.Object, 0);
            Assert.True(hasNewPage);
            //2 newPage() == 3 pages in total
            drawerMock.Verify(x => x.NewPage(null, null), Times.Exactly(2));
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

            AddInstructions(b2, 2, 60);
            var hasNewPage = block.Draw(drawerMock.Object, 0);
            Assert.True(hasNewPage);
            drawerMock.Verify(x => x.NewPage(null, null), Times.Exactly(1));
        }
    }
}
