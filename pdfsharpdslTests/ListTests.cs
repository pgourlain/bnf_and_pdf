using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class ListTests : BaseTests
    {
        private sealed class InspectableVisitor : PdfDrawerVisitor
        {
            public IDictionary<string, object?> Vars => Variables;
        }

        private static InspectableVisitor Draw(Irony.Parsing.ParseTree tree, Action<InspectableVisitor>? configure = null)
        {
            var visitor = new InspectableVisitor();
            configure?.Invoke(visitor);
            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree);
            return visitor;
        }

        private InspectableVisitor Run(string source, Action<InspectableVisitor>? configure = null) => Draw(ParseText(source), configure);

        [Fact]
        public void ListLiteralAndIndex()
        {
            var v = Run("SET VAR ITEMS=[\"a\",\"b\",\"c\"]; SET VAR FIRST=$ITEMS[0]; SET VAR LAST=$ITEMS[2]; SET VAR N=Count($ITEMS);");

            Assert.Equal("a", v.Vars["FIRST"]);
            Assert.Equal("c", v.Vars["LAST"]);
            Assert.Equal(3.0, Convert.ToDouble(v.Vars["N"]));
        }

        [Fact]
        public void ListItemsAreFormulas()
        {
            var v = Run("SET VAR A=2; SET VAR L=[$A*2, \"x\"+$A, Sum(1,2)]; SET VAR R=$L[0]+$L[2]; SET VAR T=$L[1];");

            Assert.Equal(4.0 + 3.0, Convert.ToDouble(v.Vars["R"]));
            Assert.Equal("x2", v.Vars["T"]);
        }

        [Fact]
        public void IndexIsAFormula()
        {
            var v = Run("SET VAR L=[10,20,30]; SET VAR I=1; SET VAR X=$L[$I+1];");

            Assert.Equal(30.0, Convert.ToDouble(v.Vars["X"]));
        }

        [Fact]
        public void EmptyListHasNoItems()
        {
            var v = Run("SET VAR L=[]; SET VAR N=Count($L); SET VAR HIT=0; FOREACH X IN $L DO SET VAR HIT=1; ENDFOREACH");

            Assert.Equal(0.0, Convert.ToDouble(v.Vars["N"]));
            Assert.Equal(0.0, Convert.ToDouble(v.Vars["HIT"]));
        }

        [Fact]
        public void NestedListsAreIndexedWithChainedBrackets()
        {
            var v = Run("SET VAR M=[[1,2],[3,4]]; SET VAR X=$M[1][0]; SET VAR ROW=$M[0]; SET VAR Y=$ROW[1];");

            Assert.Equal(3.0, Convert.ToDouble(v.Vars["X"]));
            Assert.Equal(2.0, Convert.ToDouble(v.Vars["Y"]));
        }

        [Fact]
        public void ForEachVisitsItemsInOrder()
        {
            var v = Run("SET VAR OUT=\"\"; FOREACH ITEM IN [\"a\",\"b\",\"c\"] DO SET VAR OUT=$OUT+$ITEM; ENDFOREACH");

            Assert.Equal("abc", v.Vars["OUT"]);
        }

        [Fact]
        public void ForEachOverAVariableAndNestedForEach()
        {
            var v = Run("SET VAR M=[[1,2],[3,4]]; SET VAR SUM=0; FOREACH ROW IN $M DO FOREACH C IN $ROW DO SET VAR SUM=$SUM+$C; ENDFOREACH ENDFOREACH");

            Assert.Equal(10.0, Convert.ToDouble(v.Vars["SUM"]));
        }

        [Fact]
        public void HostFunctionCanReturnAnArray()
        {
            var v = Run("SET VAR OUT=\"\"; FOREACH N IN Names() DO SET VAR OUT=$OUT+$N; ENDFOREACH SET VAR C=Count(Names()); SET VAR NAMES=Names(); SET VAR S=$NAMES[1];",
                visitor => visitor.RegisterFormulaFunction("Names", _ => new[] { "x", "y", "z" }));

            Assert.Equal("xyz", v.Vars["OUT"]);
            Assert.Equal(3.0, Convert.ToDouble(v.Vars["C"]));
            Assert.Equal("y", v.Vars["S"]);
        }

        [Fact]
        public void ListConvertsToTextInConcatenation()
        {
            var v = Run("SET VAR T=\"items: \"+[1,2,3];");

            Assert.Equal("items: [1, 2, 3]", v.Vars["T"]);
        }

        [Fact]
        public void CountDrivesARowTemplate()
        {
            var tree = ParseText("SET VAR L=[\"a\",\"b\",\"c\"]; ROWTEMPLATE Count=Count($L) Y=10 LINETEXT 0,0 Text=$L[$ROWINDEX]; ENDROWTEMPLATE");
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.Setup(d => d.EndDrawRowTemplate(It.IsAny<int>())).Returns(new DrawingResult());

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            foreach (var text in new[] { "a", "b", "c" })
            {
                drawer.Verify(d => d.DrawLineText(text, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double?>(), It.IsAny<double?>(),
                    It.IsAny<PdfHorizontalAlignment>(), It.IsAny<PdfVerticalAlignment>(), It.IsAny<TextOrientation>(), It.IsAny<TextFitOptions?>()), Times.Once);
            }
        }

        [Fact]
        public void IndexOutOfRangeThrowsWithPosition()
        {
            var tree = ParseText("SET VAR L=[1,2];\nSET VAR X=$L[2];");

            var ex = Assert.Throws<PdfParserException>(() => Draw(tree));

            Assert.Equal("List index 2 is out of range (the list has 2 item(s), indexes start at 0) at line 2, col 13.", ex.Message);
        }

        [Fact]
        public void NegativeAndFractionalIndexesThrow()
        {
            Assert.Throws<PdfParserException>(() => Run("SET VAR L=[1,2]; SET VAR X=$L[-1];"));
            Assert.Throws<PdfParserException>(() => Run("SET VAR L=[1,2]; SET VAR X=$L[0.5];"));
        }

        [Fact]
        public void IndexingANonListThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("SET VAR S=\"text\"; SET VAR X=$S[0];"));

            Assert.StartsWith("Cannot use [index] on a value that is not a list", ex.Message);
        }

        [Fact]
        public void NonNumericIndexThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("SET VAR L=[1,2]; SET VAR X=$L[\"a\"];"));

            Assert.StartsWith("List index 'a' is not a number", ex.Message);
        }

        [Fact]
        public void ForEachOnANonListThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("FOREACH X IN \"abc\" DO SET VAR Y=1; ENDFOREACH"));

            Assert.StartsWith("FOREACH expects a list, got the text \"abc\"", ex.Message);
        }

        [Fact]
        public void CountOnANonListThrows()
        {
            Assert.Throws<PdfParserException>(() => Run("SET VAR N=Count(3);"));
        }

        [Fact]
        public void MissingEndForEachIsDiagnosed()
        {
            var tree = CreateParser().Parse("FOREACH X IN [1] DO\n SET VAR A=1;\n");

            Assert.Equal("Missing 'ENDFOREACH' for 'FOREACH' opened at line 1, col 1.", PdfDslDiagnostics.FormatParseErrors(tree).First());
        }

        [Fact]
        public void PdfListDisplaysItsItems()
        {
            Assert.Equal("[a, 1, ]", new PdfList(new object?[] { "a", 1, null }).ToString());
        }
    }
}
