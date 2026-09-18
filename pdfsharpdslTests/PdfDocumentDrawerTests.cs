using PdfSharpDslCore.Drawing;

namespace pdfsharpdslTests
{
    public class PdfDocumentDrawerTests
    {
        [Fact]
        public void NewPageUsesDefaultsAndPersistsExplicitSettings()
        {
            using var drawer = new PdfDocumentDrawer();
            var pageNumbers = new List<int>();
            Action<int> callback = pageNumbers.Add;
            drawer.RegisterOnNewPage(callback);
            drawer.RegisterOnNewPage(callback);

            Assert.Equal(595.28, drawer.PageWidth, 2);
            Assert.Equal(841.89, drawer.PageHeight, 2);

            drawer.NewPage(PdfPageSize.Letter, PdfPageOrientation.Landscape);
            Assert.Equal(792, drawer.PageWidth);
            Assert.Equal(612, drawer.PageHeight);

            drawer.NewPage();
            Assert.Equal(792, drawer.PageWidth);
            Assert.Equal(612, drawer.PageHeight);
            Assert.Equal(new[] { 2, 3 }, pageNumbers);

            drawer.UnRegisterOnNewPage(callback);
            drawer.NewPage();
            Assert.Equal(new[] { 2, 3 }, pageNumbers);

            using var stream = new MemoryStream();
            drawer.PublishPdf(stream);
            var pdf = new PdfBinaryInspector(stream);
            Assert.Equal(4, pdf.PageCount);
            Assert.Equal(4, pdf.MediaBoxes.Count);
        }
    }
}