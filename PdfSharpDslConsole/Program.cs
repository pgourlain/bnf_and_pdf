using System.Globalization;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TerraPDF.Helpers;

ServiceProvider serviceProvider = new ServiceCollection()
    .AddLogging((loggingBuilder) => loggingBuilder
        .SetMinimumLevel(LogLevel.Trace)
        .AddSystemdConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "HH:mm:ss.fff ";
        })
    )
    .BuildServiceProvider();

var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<PdfDocumentDrawer>();
//to print decimal number with '.'
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
//Now both are working
logger?.LogDebug("Debug World");         
logger?.LogInformation("Hello World");

#region global variables
var globalComments = new[]
{
    new
    {
        Date = new DateTime(2023, 3, 6),
        Comments = new[]
        {
            new
            {
                Author = "John Smith",
                Date = new DateTime(2023, 3, 2),
                Comment = "c1"
            },
            new
            {
                Author = "John Smith",
                Date = new DateTime(2023, 3, 2),
                Comment = "c2"
            }
        }
    },
    new
    {
        Date = new DateTime(2023, 3, 6),
        Comments = new[]
        {
            new
            {
                Author = "John Doe",
                Date = new DateTime(2023, 3, 2),
                Comment = "c1"
            }
        }
    },
    new
    {
        Date = new DateTime(2023, 3, 6),
        Comments = new[]
        {
            new
            {
                Author = "John DoeDoe",
                Date = new DateTime(2023, 3, 2),
                Comment = "c1qsmdlkjqsdlkmjfqsmlkdjflmkqsdklmfqslkmd fkljmlksdflmjzmoi jlqsdkjlfqlmsdlfqlksmdaz ioeurpozerpoazueopruiaoze"
            },
            new
            {
                Author = "John DoeDoe",
                Date = new DateTime(2023, 3, 2),
                Comment = "other comment"
            }
        }
    },
};

#endregion



var parser = new Irony.Parsing.Parser(new PdfGrammar());
//var fileName = "pdfsharp-newpage.ipdf";
//var fileName = "pdfsharp.ipdf";
//var fileName = "sample1.ipdf";
var fileName = args.Length > 0 ? args[0] : "demo.ipdf";
var outputFile = Path.GetFullPath("helloworld.pdf");
if (args.Length == 0)
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
if (!Path.IsPathRooted(fileName) && !File.Exists(fileName))
    fileName = Path.Combine(AppContext.BaseDirectory, fileName);

var parsingResult = parser.Parse(File.ReadAllText(fileName));

if (parsingResult.HasErrors())
{
    //show Error
    foreach (var error in PdfDslDiagnostics.FormatParseErrors(parsingResult))
    {
        Console.Error.WriteLine($"{Path.GetFileName(fileName)}: {error}");
    }
    Environment.ExitCode = 1;
}
else
{
    RegisterSystemFonts();
    foreach (var font in LocalFontNames().Zip(LocalFontFiles()))
    {
        FontFamily.Register(font.First, font.Second);
    }

    //draw parsing result
    using var drawer = new PdfDocumentDrawer(logger);
    var visitor = new PdfDrawerVisitor(logger);

    visitor.RegisterFormulaFunction("GetFontCount", (_) => LocalFontNames().Count());
    visitor.RegisterFormulaFunction("GetFont", GetFontNameByIndex);
    visitor.RegisterFormulaFunction("getGlobalCommentDate", getGlobalCommentDate);
    visitor.RegisterFormulaFunction("getGlobalCommentsCount", (_) => getGlobalCommentsCount());
    visitor.RegisterFormulaFunction("getCommentsCount", getCommentsCount);
    visitor.RegisterFormulaFunction("getCommentDate", getCommentDate);
    visitor.RegisterFormulaFunction("getComment", getComment);
    visitor.RegisterFormulaFunction("GETCOMMENTAUTHOR", getCommentAuthor);

    visitor.Draw(drawer, parsingResult);
    drawer.PublishPdf(outputFile);
    Console.WriteLine($"PDF generated: {outputFile}");

    //var a = new PDfDsl.pdfsharp();
    //a.WritePdf(drawer);
}

IEnumerable<string> LocalFontFiles()
{
    yield return Path.Combine(AppContext.BaseDirectory, "Fonts", "AlexBrush-Regular.ttf");
    yield return Path.Combine(AppContext.BaseDirectory, "Fonts", "Just-Signature.ttf");
    yield return Path.Combine(AppContext.BaseDirectory, "Fonts", "Inspiration-Regular.ttf");
    yield return Path.Combine(AppContext.BaseDirectory, "Fonts", "Quirlycues.ttf");
    yield return Path.Combine(AppContext.BaseDirectory, "Fonts", "Rabiohead.ttf");
    yield return Path.Combine(AppContext.BaseDirectory, "Fonts", "SCRIPTIN.ttf");
}

IEnumerable<string> LocalFontNames()
{
    yield return "Alex Brush";
    yield return "Just Signature";
    yield return "Inspiration";
    yield return "Quirlycues";
    yield return "Rabiohead";
    yield return "Scriptina";
}

void RegisterSystemFonts()
{
    var fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
    RegisterFontVariant("Arial", "arial.ttf", fontsDirectory);
    RegisterFontVariant("Arial", "arialbd.ttf", fontsDirectory, bold: true);
    RegisterFontVariant("Arial", "ariali.ttf", fontsDirectory, italic: true);
    RegisterFontVariant("Arial", "arialbi.ttf", fontsDirectory, bold: true, italic: true);
    RegisterFontVariant("Consolas", "consola.ttf", fontsDirectory);
    RegisterFontVariant("Consolas", "consolab.ttf", fontsDirectory, bold: true);
    RegisterFontVariant("Consolas", "consolai.ttf", fontsDirectory, italic: true);
    RegisterFontVariant("Consolas", "consolaz.ttf", fontsDirectory, bold: true, italic: true);
}

void RegisterFontVariant(string familyName, string fileName, string directory, bool bold = false, bool italic = false)
{
    var path = Path.Combine(directory, fileName);
    if (File.Exists(path))
        FontFamily.Register(familyName, path, bold, italic);
}

object GetFontNameByIndex(object[] arguments)
{
    var index = (int)arguments[0];
    return LocalFontNames().Skip(index).First();
}

object getGlobalCommentsCount()
{
    return globalComments.Length;
}

object getGlobalCommentDate(object[] arguments)
{
    var index = (int)arguments[0];
    return globalComments[index].Date.ToShortDateString();
}

object getCommentsCount(object[] arguments)
{
    var globalIndex = (int)arguments[0];
    return globalComments[globalIndex].Comments.Length;
}

object getCommentDate(object[] arguments)
{
    var globalIndex = (int)arguments[0];
    var index = (int)arguments[1];
    return globalComments[globalIndex].Comments[index].Date.ToShortDateString();
}
object getComment(object[] arguments)
{
    var globalIndex = (int)arguments[0];
    var index = (int)arguments[1];
    return globalComments[globalIndex].Comments[index].Comment;
}

object getCommentAuthor(object[] arguments)
{
    var globalIndex = (int)arguments[0];
    var index = (int)arguments[1];
    return globalComments[globalIndex].Comments[index].Author;
}
