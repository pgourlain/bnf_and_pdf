using Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Utils;
using Pdf.Parser;
using pdfsharpdsl.Parser;
using SixLabors.Fonts;

//GlobalFontSettings.FontResolver = new FontResolver();
GlobalFontSettings.DefaultFontEncoding = PdfFontEncoding.Unicode;


//var pairings = new List<Tuple<string,string?>>();
//IOrderedEnumerable<FontFamily> ordered = SystemFonts.Families.OrderBy(x => x.Name);
//foreach (FontFamily family in ordered)
//{
//    IOrderedEnumerable<FontStyle> styles = family.GetAvailableStyles().OrderBy(x => x);
//    foreach (FontStyle style in styles)
//    {
//        Font font = family.CreateFont(0F, style);
//        font.TryGetPath(out var path);
//        pairings.Add(new Tuple<string,string?>(font.Name, path));
//    }
//}

//int max = pairings.Max(x => x.Item1.Length);
//foreach (var pk in pairings)
//{
//    Console.WriteLine($"{pk.Item1.PadRight(max)} {pk.Item2}");
//}


var p = new Irony.Parsing.Parser(new PdfGrammar());

var parsingResult = p.Parse(File.ReadAllText("pdfsharp.txt"));

if (parsingResult.HasErrors())
{
    //show Error
    foreach (var error in parsingResult.ParserMessages)
    {
        Console.Write(error.Location.ToString());
        Console.Write("=>");
        Console.WriteLine(error);
    }
}
else
{
    //PdfSharpCore
    var document = new PdfDocument();
    using var drawer = new PdfDocumentDrawer(document);
    new PdfDrawerVisitor().Draw(drawer, parsingResult);
    document.Save("helloworld.pdf");
}

