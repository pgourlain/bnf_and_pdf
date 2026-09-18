using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Extensions;

namespace pdfsharpdslTests
{
    public class DrawingValueTests
    {
        [Fact]
        public void OffsetYMovesRectanglesAndPointsWithoutChangingInputs()
        {
            var rectangle = new XRect(10, 20, 30, 40);
            var point = new XPoint(5, 6);

            var movedRectangle = rectangle.OffsetY(7);
            var movedPoint = point.OffsetY(8);

            Assert.Equal(20, rectangle.Y);
            Assert.Equal(27, movedRectangle.Y);
            Assert.Equal(10, movedRectangle.X);
            Assert.Equal(6, point.Y);
            Assert.Equal(14, movedPoint.Y);
            Assert.Equal(5, movedPoint.X);
        }

        [Fact]
        public void OffsetYMovesPointArraysOnlyWhenNeeded()
        {
            var points = new[] { new XPoint(1, 2), new XPoint(3, 4) };

            var unchanged = points.OffsetY(0);
            var moved = points.OffsetY(10);

            Assert.Same(points, unchanged);
            Assert.NotSame(points, moved);
            Assert.Equal(new[] { 12.0, 14.0 }, moved.Select(point => point.Y));
            Assert.Equal(new[] { 1.0, 3.0 }, moved.Select(point => point.X));
        }

        [Fact]
        public void TableDefinitionCalculatesColumnDimensionsAndAlignment()
        {
            var table = new TableDefinition
            {
                TopMarginOnPageBreak = 12,
                ShowHeader = false,
                HeaderHeight = 20
            };
            table.Columns.Add(new ColumnDefinition
            {
                DesiredWidth = 80,
                MaxWidth = 50,
                Alignment = XStringAlignment.Center
            });
            table.Columns.Add(new ColumnDefinition());
            table.Rows.Add(new RowDefinition
            {
                DesiredHeight = 15,
                MaxHeight = 20,
                Data = new[] { "value" }
            });

            Assert.Equal(80, table.ColWidth(0));
            Assert.Equal(0, table.ColWidth(1));
            Assert.Equal(50, table.ColMaxWidth(0, 100));
            Assert.Equal(40, table.ColMaxWidth(1, 40));
            Assert.Equal(XStringAlignment.Center, table.Alignment(0));
            Assert.Equal(12, table.TopMarginOnPageBreak);
            Assert.False(table.ShowHeader);
            Assert.Equal(20, table.HeaderHeight);
            Assert.Equal(15, table.Rows[0].DesiredHeight);
            Assert.Equal(20, table.Rows[0].MaxHeight);
            Assert.Equal("value", table.Rows[0].Data[0]);
        }

        [Fact]
        public void DrawingResultStoresRectangleAndPageOffset()
        {
            var rectangle = new XRect(1, 2, 3, 4);
            var result = new DrawingResult
            {
                DrawingRect = rectangle,
                PageOffsetY = 25
            };

            Assert.Equal(rectangle, result.DrawingRect);
            Assert.Equal(25, result.PageOffsetY);
        }
    }
}