using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

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


var parser = new Irony.Parsing.Parser(new PdfGrammar());
var fileName = "pdfsharp.txt";
if (args.Length > 0)
{
    fileName = args[0];
}
var parsingResult = parser.Parse(File.ReadAllText(fileName));

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
    //PdfSharpCore cclasses
    var document = new PdfDocument();
    //draw parsing result
    using var drawer = new PdfDocumentDrawer(document);
    new PdfDrawerVisitor().Draw(drawer, parsingResult);
    document.Save("helloworld.pdf");
}

