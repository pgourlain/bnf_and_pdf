using PdfSharpCore;
using PdfSharpCore.Pdf;
using PdfSharpDslCore.Drawing;

namespace pdfsharpdslTests
{
    public class PdfDocumentDrawerTests
    {
        [Fact]
        public void NewPageUsesDefaultsAndPersistsExplicitSettings()
        {
            using var document = new PdfDocument();
            using var drawer = new PdfDocumentDrawer(document);
            var pageNumbers = new List<int>();
            Action<int> callback = pageNumbers.Add;
            drawer.RegisterOnNewPage(callback);
            drawer.RegisterOnNewPage(callback);

            Assert.Equal(PageSize.A4, drawer.CurrentPage.Size);
            Assert.Equal(PageOrientation.Portrait, drawer.CurrentPage.Orientation);

            drawer.NewPage(PageSize.Letter, PageOrientation.Landscape);
            Assert.Equal(PageSize.Letter, drawer.CurrentPage.Size);
            Assert.Equal(PageOrientation.Landscape, drawer.CurrentPage.Orientation);

            drawer.NewPage();
            Assert.Equal(PageSize.Letter, drawer.CurrentPage.Size);
            Assert.Equal(PageOrientation.Landscape, drawer.CurrentPage.Orientation);
            Assert.Equal(new[] { 2, 3 }, pageNumbers);

            drawer.UnRegisterOnNewPage(callback);
            drawer.NewPage();
            Assert.Equal(new[] { 2, 3 }, pageNumbers);
        }
    }
}