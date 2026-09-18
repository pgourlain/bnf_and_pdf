using Castle.Components.DictionaryAdapter.Xml;
using Irony.Parsing;
using Moq;
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

            Assert.Equal(PdfColor.Black, mock.Object.CurrentBrush.Color);
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
            mock.SetupProperty(x => x.CurrentFont, new PdfFont("Consolas", 8));

            var visitor = new PdfDrawerForTestsVisitor();
            visitor.RegisterFormulaFunction("getFontName", (_) => expected);
            var drawer = mock.Object;
            visitor.Draw(drawer, res);
            var f = drawer.CurrentFont;
            Assert.StartsWith((string)expected, drawer.CurrentFont.FamilyName);
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
            var tree = ParseText("VIEWSIZE 100,140;TEXT 10,20 MaxWidth=30 Text=\"hello\";DEBUGOPTIONS DEBUG_TEXT, DEBUG_RECT, DEBUG_ROWTEMPLATE, DEBUG_IMAGE, DEBUG_RULE, DEBUG_ALL, UNKNOWN;");
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.SetupProperty(x => x.DebugOptions);

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            drawer.Verify(x => x.SetViewSize(100, 140), Times.Once);
            drawer.Verify(x => x.DrawText("hello", 10, 20, 30, null), Times.Once);
            Assert.Equal(
                DebugOptions.DebugText | DebugOptions.DebugRect | DebugOptions.DebugRowTemplate | DebugOptions.DebugImage | DebugOptions.DebugRule | DebugOptions.DebugAll,
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
        public void ImageStatementsPreserveDimensionsUnitsAndCropMode()
        {
            const string imageData = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
            var tree = ParseText(
                $"IMAGE 1,2 Data=\"data:image/png;base64,{imageData}\";" +
                $"IMAGE 3,4,30,40 pixel crop Data=\"{imageData}\";" +
                $"IMAGE 5,6,70,80 point fit Data=\"{imageData}\";");
            var calls = new List<(double X, double Y, double? Width, double? Height, bool Pixel, bool Crop)>();
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.Setup(x => x.DrawImage(
                    It.IsAny<PdfImage>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double?>(),
                    It.IsAny<double?>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .Callback<PdfImage, double, double, double?, double?, bool, bool>((_, x, y, width, height, pixel, crop) =>
                    calls.Add((x, y, width, height, pixel, crop)));

            new PdfDrawerVisitor().Draw(drawer.Object, tree);

            Assert.Equal((1, 2, null, null, false, false), calls[0]);
            Assert.Equal((3, 4, 30, 40, true, true), calls[1]);
            Assert.Equal((5, 6, 70, 80, false, false), calls[2]);
        }

        [Fact]
        public void TableRowTemplateBuildsRowsWidthsAndPadding()
        {
            var tree = ParseText(
                "TABLE 20,30 " +
                "HEAD " +
                "COL Width=40 MaxWidth=30 \"A\"; " +
                "COL Width=auto MaxWidth=100 \"B\"; " +
                "ENDHEAD " +
                "ROWTEMPLATE 2 " +
                "COL $ROWINDEX; " +
                "ENDROW " +
                "ENDTABLE");
            TableDefinition? capturedTable = null;
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.Setup(x => x.DrawTable(20, 30, It.IsAny<TableDefinition>()))
                .Callback<double, double, TableDefinition>((_, _, table) => capturedTable = table);
            var visitor = new InspectablePdfDrawerVisitor();

            visitor.Draw(drawer.Object, tree);

            Assert.NotNull(capturedTable);
            Assert.Equal(2, capturedTable.Columns.Count);
            Assert.Equal(40, capturedTable.Columns[0].DesiredWidth);
            Assert.Equal(30, capturedTable.Columns[0].MaxWidth);
            Assert.Null(capturedTable.Columns[1].DesiredWidth);
            Assert.Equal(100, capturedTable.Columns[1].MaxWidth);
            Assert.Equal(new[] { "0", string.Empty }, capturedTable.Rows[0].Data);
            Assert.Equal(new[] { "1", string.Empty }, capturedTable.Rows[1].Data);
            Assert.False(visitor.Vars.ContainsKey("ROWINDEX"));
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
        public void NestedDslUdfsRestoreOuterScopes()
        {
            var tree = ParseText(
                "SET VAR VALUE=10; " +
                "UDF INNER(VALUE) SET VAR INNERONLY=99; LINE $VALUE,$INNERONLY,$VALUE,$INNERONLY; ENDUDF " +
                "UDF OUTER(VALUE) SET VAR OUTERONLY=20; CALL INNER(30); LINE $VALUE,$OUTERONLY,$VALUE,$OUTERONLY; ENDUDF " +
                "CALL OUTER(40); LINE $VALUE,0,$VALUE,0;");
            var drawer = new Mock<IPdfDocumentDrawer>();
            var visitor = new InspectablePdfDrawerVisitor();

            visitor.Draw(drawer.Object, tree);

            drawer.Verify(x => x.DrawLine(30, 99, 30, 99), Times.Once);
            drawer.Verify(x => x.DrawLine(40, 20, 40, 20), Times.Once);
            drawer.Verify(x => x.DrawLine(10, 0, 10, 0), Times.Once);
            Assert.Equal(10.0, visitor.Vars["VALUE"]);
            Assert.False(visitor.Vars.ContainsKey("INNERONLY"));
            Assert.False(visitor.Vars.ContainsKey("OUTERONLY"));
        }

        [Fact]
        public void DslUdfFailureRestoresOuterScope()
        {
            var tree = ParseText(
                "SET VAR VALUE=1; " +
                "UDF FAIL(VALUE) SET VAR TEMP=2; LINE $MISSING,0,0,0; ENDUDF " +
                "CALL FAIL(9);");
            var visitor = new InspectablePdfDrawerVisitor();

            Assert.Throws<ArgumentOutOfRangeException>(() => visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.Equal(1.0, visitor.Vars["VALUE"]);
            Assert.False(visitor.Vars.ContainsKey("TEMP"));
        }

        [Theory]
        [InlineData("UDF NEEDS(X) LINE $X,0,0,0; ENDUDF CALL NEEDS();")]
        [InlineData("UDF NONE() LINE 0,0,0,0; ENDUDF CALL NONE(1);")]
        public void DslUdfRejectsZeroArgumentCountMismatches(string input)
        {
            var tree = ParseText(input);
            var visitor = new InspectablePdfDrawerVisitor();

            var error = Assert.Throws<PdfParserException>(() => visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.Contains("arguments count", error.Message);
        }

        [Fact]
        public void RowTemplateTracksOffsetsAndFinalHeight()
        {
            var tree = ParseText("ROWTEMPLATE Count=2 Y=10 Name=\"row\" BorderSize=2 NewPageTopMargin=5 LINE 0,0,10,10; ENDROWTEMPLATE");
            var drawer = new Mock<IPdfDocumentDrawer>();
            drawer.SetupSequence(x => x.EndDrawRowTemplate(It.IsAny<int>()))
                .Returns(new DrawingResult { DrawingRect = new PdfRect(0, 0, 10, 20), PageOffsetY = 0 })
                .Returns(new DrawingResult { DrawingRect = new PdfRect(0, 20, 10, 30), PageOffsetY = 10 });
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
