using Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Utils;
using Pdf.Parser;
using pdfsharpdsl.Parser;

GlobalFontSettings.FontResolver = new FontResolver();

var document = new PdfDocument();
//var w= new PdfDocumentDrawer(document);

//w.DrawRect(-10,-10,-5,-5);
//w.DrawNumber("100.00", -0.1, 10);

//List<Person> persons = new List<Person>();

//for (int i =0; i < 20; i++)
//{
//    persons.Add(new Person{
//        Age = i,
//        FirstName = $"F-{i}",
//        LastName = $"Last-{i}"
//    });
//}
//var tblDefinition = new TableDefinition();
//tblDefinition.Columns.Add(new ColumnDefinition{ ColumnHeaderName = "First Name", Width = 100});
//tblDefinition.Columns.Add(new ColumnDefinition{ ColumnHeaderName = "Last Name", Width = 100});
//tblDefinition.Columns.Add(new ColumnDefinition{ ColumnHeaderName = "Age", Width = 50, Alignment = XStringAlignment.Far});
//w.DrawTable(persons.Select(x => new object[]{x.FirstName, x.LastName, x.Age}), 0,0, tblDefinition);
//document.Save("helloworld.pdf");

var p = new Irony.Parsing.Parser(new PdfGrammar());

var parsingResult = p.Parse(File.ReadAllText("pdfsharp.txt"));

if (parsingResult.HasErrors())
{
    //show Error
    foreach (var error in parsingResult.ParserMessages)
    {
        Console.WriteLine(error);
    }
}
else
{
    new PdfDrawerVisitor().Draw(document, parsingResult);
    document.Save("helloworld.pdf");
}

