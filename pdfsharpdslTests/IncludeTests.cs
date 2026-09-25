using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class IncludeTests : BaseTests, IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "ipdf-include-" + Guid.NewGuid().ToString("N"));

        public IncludeTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private sealed class InspectableVisitor : PdfDrawerVisitor
        {
            public InspectableVisitor(string dir) : base(dir, null) { }
            public IDictionary<string, object?> Vars => Variables;
        }

        private void Write(string relativePath, string content)
        {
            var path = Path.Combine(_dir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        private InspectableVisitor Run(string source)
        {
            var tree = ParseText(source);
            var visitor = new InspectableVisitor(_dir);
            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree);
            return visitor;
        }

        [Fact]
        public void IncludeParses()
        {
            ParseText("INCLUDE \"styles.ipdf\";\nSET VAR X=1;");
            ParseText("INCLUDE \"styles.ipdf\"\nSET VAR X=1;");
        }

        [Fact]
        public void IncludedStatementsRunInPlace()
        {
            Write("b.ipdf", "SET VAR B=$A+1;");

            var v = Run("SET VAR A=1; INCLUDE \"b.ipdf\"; SET VAR C=$B+1;");

            Assert.Equal(3.0, Convert.ToDouble(v.Vars["C"]));
        }

        [Fact]
        public void UdfsAndStylesAreSharedAndCanBeUsedBeforeTheInclude()
        {
            Write("lib.ipdf", "UDF THICK() SET PEN blue 3; ENDUDF\nSTYLE red SET BRUSH red; ENDSTYLE");
            var mock = new Mock<IPdfDocumentDrawer>();
            mock.SetupProperty(x => x.CurrentBrush);
            mock.SetupProperty(x => x.CurrentPen, new PdfPen(PdfColor.Black, 1));
            var tree = ParseText("CALL THICK(); USE red; INCLUDE \"lib.ipdf\";");
            var visitor = new InspectableVisitor(_dir);

            visitor.Draw(mock.Object, tree);

            Assert.Equal(3, mock.Object.CurrentPen.Width);
            Assert.Equal(PdfColors.FromName("red"), mock.Object.CurrentBrush.Color);
        }

        [Fact]
        public void NestedIncludesAreRelativeToTheIncludingFile()
        {
            Write("sub/a.ipdf", "INCLUDE \"deeper/b.ipdf\";");
            Write("sub/deeper/b.ipdf", "SET VAR FOUND=1;");

            var v = Run("INCLUDE \"sub/a.ipdf\";");

            Assert.Equal(1.0, Convert.ToDouble(v.Vars["FOUND"]));
        }

        [Fact]
        public void AFileIsOnlyIncludedOnce()
        {
            Write("common.ipdf", "SET VAR N=$N+1;");
            Write("a.ipdf", "INCLUDE \"common.ipdf\";");

            var v = Run("SET VAR N=0; INCLUDE \"a.ipdf\"; INCLUDE \"common.ipdf\"; INCLUDE \"a.ipdf\";");

            Assert.Equal(1.0, Convert.ToDouble(v.Vars["N"]));
        }

        [Fact]
        public void FilesCanEachIncludeTheSharedFileTheyNeed()
        {
            Write("common.ipdf", "UDF SHARED() SET VAR HIT=1; ENDUDF");
            Write("one.ipdf", "INCLUDE \"common.ipdf\"; SET VAR ONE=1;");
            Write("two.ipdf", "INCLUDE \"common.ipdf\"; SET VAR TWO=1;");

            var v = Run("INCLUDE \"common.ipdf\"; INCLUDE \"one.ipdf\"; INCLUDE \"two.ipdf\"; SET VAR SUM=$ONE+$TWO;");

            Assert.Equal(2.0, Convert.ToDouble(v.Vars["SUM"]));
        }

        [Fact]
        public void RunTimeErrorInAnIncludedFileNamesIt()
        {
            Write("bad.ipdf", "SET VAR A=1;\nSET VAR B=$MISSING;");

            var ex = Assert.Throws<PdfParserException>(() => Run("INCLUDE \"bad.ipdf\";"));

            Assert.Equal("Variable '$MISSING' is not defined at line 2, col 11 of bad.ipdf.", ex.Message);
        }

        [Fact]
        public void RunTimeErrorInAnIncludedUdfNamesItsFileNotTheCallers()
        {
            Write("lib.ipdf", "UDF BROKEN()\n SET VAR B=$MISSING;\nENDUDF");

            var ex = Assert.Throws<PdfParserException>(() => Run("INCLUDE \"lib.ipdf\";\nCALL BROKEN();"));

            Assert.Equal("Variable '$MISSING' is not defined at line 2, col 12 of lib.ipdf.", ex.Message);
        }

        [Fact]
        public void RunTimeErrorInTheMainFileHasNoFileEvenAfterAnInclude()
        {
            Write("ok.ipdf", "SET VAR A=1;");

            var ex = Assert.Throws<PdfParserException>(() => Run("INCLUDE \"ok.ipdf\";\nSET VAR B=$MISSING;"));

            Assert.Equal("Variable '$MISSING' is not defined at line 2, col 11.", ex.Message);
        }

        [Fact]
        public void MainFileUdfCalledFromAnIncludedFileReportsTheMainFile()
        {
            Write("caller.ipdf", "CALL FROMMAIN();");

            var ex = Assert.Throws<PdfParserException>(() => Run("UDF FROMMAIN() SET VAR B=$MISSING; ENDUDF INCLUDE \"caller.ipdf\";"));

            Assert.DoesNotContain(" of ", ex.Message);
        }

        [Fact]
        public void ImageSourceIsRelativeToTheFileThatDrawsIt()
        {
            Directory.CreateDirectory(Path.Combine(_dir, "sub"));
            File.WriteAllBytes(Path.Combine(_dir, "sub", "pic.png"), Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
            Write("sub/a.ipdf", "IMAGE 10,10,20,20 point fit Source=\"pic.png\";");
            var drawer = new Mock<IPdfDocumentDrawer>();

            new InspectableVisitor(_dir).Draw(drawer.Object, ParseText("INCLUDE \"sub/a.ipdf\";"));

            drawer.Verify(d => d.DrawImage(It.IsAny<PdfImage>(), 10, 10, 20, 20, false, false), Times.Once);
        }

        [Fact]
        public void CircularIncludeThrowsWithTheChain()
        {
            Write("a.ipdf", "INCLUDE \"b.ipdf\";");
            Write("b.ipdf", "INCLUDE \"a.ipdf\";");

            var ex = Assert.Throws<PdfParserException>(() => Run("INCLUDE \"a.ipdf\";"));

            // detected in b.ipdf, on its INCLUDE of a.ipdf
            Assert.Equal("Circular INCLUDE at line 1, col 9: a.ipdf -> b.ipdf -> a.ipdf.", ex.Message);
        }

        [Fact]
        public void MissingFileThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("SET VAR A=1;\nINCLUDE \"nope.ipdf\";"));

            Assert.StartsWith("INCLUDE file 'nope.ipdf' not found at line 2", ex.Message);
        }

        [Fact]
        public void ParseErrorInIncludedFileNamesTheFileAndItsOwnLine()
        {
            Write("bad.ipdf", "SET VAR A=1;\n\nLINETXT 1,2 Text=\"x\";\n");

            var ex = Assert.Throws<PdfParserException>(() => Run("INCLUDE \"bad.ipdf\";"));

            Assert.Contains("bad.ipdf: Unknown instruction 'LINETXT' at line 3, col 1. Did you mean 'LINETEXT'?", ex.Message);
        }

        [Fact]
        public void DuplicateUdfAcrossFilesThrows()
        {
            Write("lib.ipdf", "UDF F() SET VAR A=1; ENDUDF");

            Assert.Throws<PdfParserException>(() => Run("UDF F() SET VAR A=2; ENDUDF INCLUDE \"lib.ipdf\";"));
        }

        [Fact]
        public void IncludeNestedTooDeepThrows()
        {
            for (var i = 0; i < 20; i++) Write($"f{i}.ipdf", $"INCLUDE \"f{i + 1}.ipdf\";");

            var ex = Assert.Throws<PdfParserException>(() => Run("INCLUDE \"f0.ipdf\";"));

            Assert.Contains("levels deep", ex.Message);
        }
    }
}
