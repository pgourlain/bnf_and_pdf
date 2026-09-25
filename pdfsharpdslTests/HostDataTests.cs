using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class HostDataTests : BaseTests
    {
        private sealed class InspectableVisitor : PdfDrawerVisitor
        {
            public IDictionary<string, object?> Vars => Variables;
        }

        private sealed class Order
        {
            public string Customer { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string[] Tags { get; set; } = Array.Empty<string>();
            public Order? Parent { get; set; }
            public int Field = 7;
        }

        private static readonly Order[] Orders =
        {
            new Order { Customer = "ACME", Amount = 120.5m, Date = new DateTime(2026, 3, 1), Tags = new[] { "a", "b" } },
            new Order { Customer = "Globex", Amount = 80m, Date = new DateTime(2026, 4, 2) },
        };

        private InspectableVisitor Run(string source, Action<InspectableVisitor> configure)
        {
            var tree = ParseText(source);
            var visitor = new InspectableVisitor();
            configure(visitor);
            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree);
            return visitor;
        }

        [Fact]
        public void PresetVariablesAreReadableAndStillAssignable()
        {
            var v = Run("SET VAR T=$TITLE+\"!\"; SET VAR TITLE=\"changed\";", x => x.SetData("TITLE", "Report"));

            Assert.Equal("Report!", v.Vars["T"]);
            Assert.Equal("changed", v.Vars["TITLE"]);
        }

        [Fact]
        public void DataSurvivesRedrawWithTheSameVisitor()
        {
            var visitor = new InspectableVisitor();
            visitor.SetData("N", 5);
            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), ParseText("SET VAR N=$N+1;"));
            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), ParseText("SET VAR M=$N;"));

            Assert.Equal(5, Convert.ToInt32(visitor.Vars["M"]));
        }

        [Fact]
        public void ObjectPropertiesAreReadCaseInsensitively()
        {
            var v = Run("SET VAR C=$order.customer; SET VAR A=$order.AMOUNT+1; SET VAR F=$order.field;",
                x => x.SetData("order", Orders[0]));

            Assert.Equal("ACME", v.Vars["C"]);
            Assert.Equal(121.5, Convert.ToDouble(v.Vars["A"]));
            Assert.Equal(7, Convert.ToInt32(v.Vars["F"]));
        }

        [Fact]
        public void DictionariesAreReadByKey()
        {
            var record = new Dictionary<string, object?> { ["name"] = "Ada", ["Age"] = 36, ["nested"] = new Dictionary<string, object?> { ["x"] = 1 } };
            var v = Run("SET VAR N=$p.name; SET VAR A=$p.age; SET VAR X=$p.nested.x;", x => x.SetData("p", record));

            Assert.Equal("Ada", v.Vars["N"]);
            Assert.Equal(36, Convert.ToInt32(v.Vars["A"]));
            Assert.Equal(1, Convert.ToInt32(v.Vars["X"]));
        }

        [Fact]
        public void ExactKeyWinsOverACaseInsensitiveOne()
        {
            var record = new Dictionary<string, object?> { ["Name"] = "upper", ["name"] = "lower" };
            var v = Run("SET VAR A=$p.name; SET VAR B=$p.Name;", x => x.SetData("p", record));

            Assert.Equal("lower", v.Vars["A"]);
            Assert.Equal("upper", v.Vars["B"]);
        }

        [Fact]
        public void ListOfRecordsWorksWithIndexCountAndForEach()
        {
            var v = Run(
                "SET VAR FIRST=$orders[0].customer; SET VAR N=Count($orders); SET VAR SUM=0; SET VAR NAMES=\"\";" +
                "FOREACH O IN $orders DO SET VAR SUM=$SUM+$O.amount; SET VAR NAMES=$NAMES+$O.customer+\";\"; ENDFOREACH",
                x => x.SetData("orders", Orders));

            Assert.Equal("ACME", v.Vars["FIRST"]);
            Assert.Equal(2, Convert.ToInt32(v.Vars["N"]));
            Assert.Equal(200.5, Convert.ToDouble(v.Vars["SUM"]));
            Assert.Equal("ACME;Globex;", v.Vars["NAMES"]);
        }

        [Fact]
        public void ListOfDictionariesIsAListOfRecords()
        {
            var rows = new List<Dictionary<string, object?>>
            {
                new() { ["k"] = "a" },
                new() { ["k"] = "b" },
            };
            var v = Run("SET VAR N=Count($rows); SET VAR K=$rows[1].k;", x => x.SetData("rows", rows));

            Assert.Equal(2, Convert.ToInt32(v.Vars["N"]));
            Assert.Equal("b", v.Vars["K"]);
        }

        [Fact]
        public void ADictionaryIsNotAList()
        {
            var ex = Assert.Throws<PdfParserException>(() =>
                Run("FOREACH X IN $p DO SET VAR Y=1; ENDFOREACH", x => x.SetData("p", new Dictionary<string, object?> { ["a"] = 1 })));

            Assert.StartsWith("FOREACH expects a list", ex.Message);
        }

        [Fact]
        public void MemberOfAListItemAndNestedObjectsAndArrays()
        {
            var child = new Order { Customer = "Child", Parent = Orders[0], Tags = new[] { "x", "y", "z" } };
            var v = Run("SET VAR P=$o.parent.customer; SET VAR T=$o.tags[2]; SET VAR C=Count($o.tags);", x => x.SetData("o", child));

            Assert.Equal("ACME", v.Vars["P"]);
            Assert.Equal("z", v.Vars["T"]);
            Assert.Equal(3, Convert.ToInt32(v.Vars["C"]));
        }

        [Fact]
        public void DatesCanBeFormatted()
        {
            var v = Run("SET VAR D=Format($o.date, \"yyyy-MM-dd\");", x => x.SetData("o", Orders[1]));

            Assert.Equal("2026-04-02", v.Vars["D"]);
        }

        [Fact]
        public void UnknownFieldSuggestsAndPointsAtThePosition()
        {
            var tree = ParseText("SET VAR X=1;\nSET VAR C=$order.custmer;");
            var visitor = new InspectableVisitor();
            visitor.SetData("order", Orders[0]);

            var ex = Assert.Throws<PdfParserException>(() => visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.Equal("There is no field 'custmer' at line 2, col 17. Did you mean 'Customer'?", ex.Message);
        }

        [Fact]
        public void UnknownFieldOfADictionaryListsTheKeys()
        {
            var ex = Assert.Throws<PdfParserException>(() =>
                Run("SET VAR X=$p.zzz;", x => x.SetData("p", new Dictionary<string, object?> { ["alpha"] = 1, ["beta"] = 2 })));

            Assert.Contains("Available: alpha, beta.", ex.Message);
        }

        [Fact]
        public void FieldOfANullValueThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("SET VAR X=$o.parent.customer;", x => x.SetData("o", Orders[0])));

            Assert.StartsWith("Cannot read '.customer' of an empty value", ex.Message);
        }

        [Fact]
        public void DecimalNumbersUseTheInvariantFormatInFormulas()
        {
            var v = Run("SET VAR T=$o.amount+\" EUR\";", x => x.SetData("o", Orders[0]));

            Assert.Contains("120", (string)v.Vars["T"]!);
        }

        [Fact]
        public void DataDrivesARowTemplate()
        {
            var tree = ParseText("ROWTEMPLATE Count=Count($orders) Y=10 LINETEXT 0,0 Text=$orders[$ROWINDEX].customer; ENDROWTEMPLATE");
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.Setup(d => d.EndDrawRowTemplate(It.IsAny<int>())).Returns(new DrawingResult());
            var visitor = new PdfDrawerVisitor();
            visitor.SetData("orders", Orders);

            visitor.Draw(drawer.Object, tree);

            foreach (var name in new[] { "ACME", "Globex" })
            {
                drawer.Verify(d => d.DrawLineText(name, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double?>(),
                    It.IsAny<PdfHorizontalAlignment>(), It.IsAny<PdfVerticalAlignment>(), It.IsAny<TextOrientation>(), It.IsAny<TextFitOptions?>()), Times.Once);
            }
        }

        [Fact]
        public void SetDataNeedsAName()
        {
            Assert.Throws<ArgumentException>(() => new PdfDrawerVisitor().SetData(" ", 1));
        }

        [Fact]
        public void MemberAccessDoesNotBreakDecimalNumbers()
        {
            var v = Run("SET VAR X=1.5+2; SET VAR Y=$N.x;", x => x.SetData("N", new Dictionary<string, object?> { ["x"] = 2 }));

            Assert.Equal(2, Convert.ToInt32(v.Vars["Y"]));
        }
    }
}
