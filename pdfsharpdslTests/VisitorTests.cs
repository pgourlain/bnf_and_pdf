using Castle.Components.DictionaryAdapter.Xml;
using Irony.Parsing;
using Moq;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]

    public class VisitorTests : BaseTests
    {
        [Fact]
        public void TestValidFiles()
        {
            var res = ParseText("SET BRUSH black");
            var mock = new Mock<IPdfDocumentDrawer>();
            mock.SetupProperty(x => x.CurrentBrush);
            new PdfDrawerVisitor().Draw(mock.Object, res);

            Assert.Equal(XColors.Black, ((XSolidBrush)mock.Object.CurrentBrush).Color);
        }

        [Theory]
        [InlineData("SET VAR X=-2+3*2+5/2-3;", -2 + 3 * 2 + 5 / 2.0 - 3)]
        [InlineData("SET VAR X=-(2);", -2.0)]
        [InlineData("SET VAR X=\"coucou3\"+(2);", "coucou32")]
        [InlineData("SET VAR X=\"3\"+(2);", 5.0)]
        [InlineData("SET VAR X=\"3\"+(Random()+Random(1,2,3));", 9.0)]
        [InlineData("SET VAR A=2;SET VAR B=3;SET VAR X=Sum($A*$A, $B*$B,Sum(1,2,3));", 19.0)]
        public void TestFormulaEvaluator(string input, object expected)
        {
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.RegisterFormulaFunction("RANDOM", (args) => args.Sum(x => Convert.ToDouble(x)));
            visitor.RegisterFormulaFunction("Sum", (args) => args.Sum(x => Convert.ToDouble(x)));
            visitor.Draw(mock.Object, res);
            Assert.Equal(expected, visitor.Vars["X"]);
        }


        [Theory]

        [InlineData("SET FONT Name=getFontName() Size=12;","Arial", 12)]
        public void TestFormulaEvaluatorWithFontName(string input, object expected, int size)
        {
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            mock.SetupProperty(x => x.CurrentFont, new XFont("Consolas", 8));

            var visitor = new PdfDrawerForTestsVisitor();
            visitor.RegisterFormulaFunction("getFontName", (_) => expected);
            var drawer = mock.Object;
            visitor.Draw(drawer, res);
            var f = drawer.CurrentFont;
            //because font names change on different OS
            var pi = typeof(XFont).GetProperty("FamilyName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.StartsWith((string)expected, (string)pi?.GetValue(drawer.CurrentFont)!);
            Assert.Equal(size, drawer.CurrentFont.Size);
        }


        [Theory]
        [InlineData("pdf1-custom-udfs.txt")]
        public void TestCustomUdfEvaluator(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();
            visitor.Draw(mock.Object, res);
            Assert.Equal(10, visitor.UDFs.Count);
            foreach (var udFs in visitor.UDFs)
            {
                var n = int.Parse(udFs.Key[6..]);
                //check that number arguments is equal
                Assert.Equal(n, udFs.Value.Length);

            }
        }

        [Theory]
        [InlineData("pdf1-conditions.txt")]
        public void TestConditionEvaluator(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            var res = ParseText(input);
            var mock = new Mock<IPdfDocumentDrawer>();
            var visitor = new PdfDrawerForTestsVisitor();

            visitor.Draw(mock.Object, res);
            foreach (var udF in visitor.UDFs)
            {
                Assert.Equal("OK", udF.Value[0]);
            }
        }

        [Fact]
        public void DrawExecutesViewSizeTextWidthAndDebugOptions()
        {
            var tree = ParseText("VIEWSIZE 100,140;TEXT 10,20 MaxWidth=30 Text=\"hello\";DEBUGOPTIONS DEBUG_TEXT, DEBUG_RECT, DEBUG_ROWTEMPLATE, DEBUG_RULE, DEBUG_ALL, UNKNOWN;");
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.SetupProperty(x => x.DebugOptions);

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            drawer.Verify(x => x.SetViewSize(100, 140), Times.Once);
            drawer.Verify(x => x.DrawText("hello", 10, 20, 30, null), Times.Once);
            Assert.Equal(
                DebugOptions.DebugText | DebugOptions.DebugRect | DebugOptions.DebugRowTemplate | DebugOptions.DebugRule | DebugOptions.DebugAll,
                drawer.Object.DebugOptions);
        }

        [Fact]
        public void DrawResolvesPageSystemVariables()
        {
            var tree = ParseText("SET VAR WIDTH=$PAGEWIDTH;SET VAR HEIGHT=$PAGEHEIGHT;");
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.SetupGet(x => x.PageWidth).Returns(612);
            drawer.SetupGet(x => x.PageHeight).Returns(792);
            var visitor = new InspectablePdfDrawerVisitor();

            visitor.Draw(drawer.Object, tree);

            Assert.Equal(612.0, visitor.Vars["WIDTH"]);
            Assert.Equal(792.0, visitor.Vars["HEIGHT"]);
        }

        [Fact]
        public void CustomUdfCanFallBackToDslBodyOrOverrideIt()
        {
            var fallbackTree = ParseText("UDF SAMPLE(X) LINE $X,0,$X,1; ENDUDF CALL SAMPLE(3);");
            var overrideTree = ParseText("CALL CUSTOM(5);");
            var drawer = new Mock<IPdfDocumentDrawer>();
            var visitor = new InspectablePdfDrawerVisitor();
            string[]? parameterNames = null;
            object?[]? parameterValues = null;
            visitor.RegisterCustomUdf("sample", (_, names, values) =>
            {
                parameterNames = names;
                parameterValues = values;
                return false;
            });
            visitor.RegisterCustomUdf("custom", (_, _, _) => false);
            visitor.RegisterCustomUdf("CUSTOM", (_, _, values) =>
            {
                parameterValues = values;
                return true;
            });

            visitor.Draw(drawer.Object, fallbackTree);
            visitor.Draw(drawer.Object, overrideTree);

            Assert.Equal(new[] { "X" }, parameterNames);
            Assert.Equal(5.0, Assert.Single(parameterValues!));
            drawer.Verify(x => x.DrawLine(3, 0, 3, 1), Times.Once);
        }

        [Fact]
        public void CustomUdfErrorsAreReportedAsParserErrors()
        {
            var customFailureTree = ParseText("CALL CUSTOM();");
            var missingTree = ParseText("CALL MISSING();");
            var drawer = new Mock<IPdfDocumentDrawer>();
            var visitor = new InspectablePdfDrawerVisitor();
            visitor.RegisterCustomUdf("CUSTOM", (_, _, _) => throw new InvalidOperationException("failure"));

            var customError = Assert.Throws<PdfParserException>(() => visitor.Draw(drawer.Object, customFailureTree));
            var missingError = Assert.Throws<PdfParserException>(() => visitor.Draw(drawer.Object, missingTree));

            Assert.IsType<InvalidOperationException>(customError.InnerException);
            Assert.Contains("MISSING", missingError.Message);
        }

        [Fact]
        public void RowTemplateTracksOffsetsAndFinalHeight()
        {
            var tree = ParseText("ROWTEMPLATE Count=2 Y=10 Name=\"row\" BorderSize=2 NewPageTopMargin=5 LINE 0,0,10,10; ENDROWTEMPLATE");
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.SetupSequence(x => x.EndDrawRowTemplate(It.IsAny<int>()))
                .Returns(new DrawingResult { DrawingRect = new XRect(0, 0, 10, 20), PageOffsetY = 0 })
                .Returns(new DrawingResult { DrawingRect = new XRect(0, 20, 10, 30), PageOffsetY = 10 });
            var visitor = new InspectablePdfDrawerVisitor();

            visitor.Draw(drawer.Object, tree);

            drawer.Verify(x => x.BeginIterationTemplate(2), Times.Once);
            drawer.Verify(x => x.BeginDrawRowTemplate("row", 0, 12, 5), Times.Once);
            drawer.Verify(x => x.BeginDrawRowTemplate("row", 1, 34, 5), Times.Once);
            drawer.Verify(x => x.EndIterationTemplate(42), Times.Once);
            drawer.Verify(x => x.DrawLine(0, 0, 10, 10), Times.Exactly(2));
            Assert.Equal(42.0, visitor.Vars["LASTTEMPLATEHEIGHT"]);
            Assert.False(visitor.Vars.ContainsKey("ROWINDEX"));
        }

        [Fact]
        public void OnNewPageUdfPersistsGlobalVariables()
        {
            var tree = ParseText("UDF __ONNEWPAGE() SET VAR SAVEDPAGE=$PAGEINDEX; ENDUDF");
            var drawer = new Mock<IPdfDocumentDrawer>();
            Action<int>? onNewPage = null;
            drawer.Setup(x => x.RegisterOnNewPage(It.IsAny<Action<int>>()))
                .Callback<Action<int>>(callback => onNewPage = callback);
            var visitor = new InspectablePdfDrawerVisitor();

            visitor.Draw(drawer.Object, tree);
            Assert.NotNull(onNewPage);
            onNewPage!(4);

            Assert.Equal(4, visitor.Vars["PAGEINDEX"]);
            Assert.Equal(4, visitor.Vars["SAVEDPAGE"]);
        }

        private sealed class InspectablePdfDrawerVisitor : PdfDrawerVisitor
        {
            public IDictionary<string, object?> Vars => Variables;
        }
    }
}
