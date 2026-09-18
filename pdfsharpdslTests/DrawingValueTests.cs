using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Extensions;

namespace pdfsharpdslTests
{
    public class DrawingValueTests
    {
        [Fact]
        public void OffsetYMovesRectanglesAndPointsWithoutChangingInputs()
        {
            var rectangle = new PdfRect(10, 20, 30, 40);
            var point = new PdfPoint(5, 6);

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
            var points = new[] { new PdfPoint(1, 2), new PdfPoint(3, 4) };

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
                Alignment = PdfHorizontalAlignment.Center
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
            Assert.Equal(PdfHorizontalAlignment.Center, table.Alignment(0));
            Assert.Equal(50, table.Columns[0].DrawWidth);
            Assert.Equal(0, table.Columns[1].DrawWidth);
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
            var rectangle = new PdfRect(1, 2, 3, 4);
            var result = new DrawingResult
            {
                DrawingRect = rectangle,
                PageOffsetY = 25
            };

            Assert.Equal(rectangle, result.DrawingRect);
            Assert.Equal(25, result.PageOffsetY);
        }

        [Fact]
        public void TextRectangleAlignmentUsesRelativeBounds()
        {
            var bounds = new PdfRect(10, 20, 100, 50);
            var textSize = new PdfSize(30, 10);
            var centered = DrawingHelper.RectFromStringFormat(bounds, textSize,
                PdfHorizontalAlignment.Center, PdfVerticalAlignment.Center);
            var far = DrawingHelper.RectFromStringFormat(bounds, textSize,
                PdfHorizontalAlignment.Far, PdfVerticalAlignment.Far);

            Assert.Equal(new PdfRect(45, 40, 30, 10), centered);
            Assert.Equal(new PdfRect(80, 60, 30, 10), far);
        }

        [Fact]
        public void TextRectangleIsClippedToProvidedBounds()
        {
            var bounds = new PdfRect(10, 20, 100, 50);
            var result = DrawingHelper.RectFromStringFormat(bounds, new PdfSize(200, 100),
                PdfHorizontalAlignment.Near, PdfVerticalAlignment.Near);

            Assert.Equal(bounds, result);
        }
    }
}