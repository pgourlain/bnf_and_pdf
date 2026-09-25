using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class ControlFlowTests : BaseTests
    {
        private sealed class InspectableVisitor : PdfDrawerVisitor
        {
            public IDictionary<string, object?> Vars => Variables;
        }

        private InspectableVisitor Run(string source)
        {
            var tree = ParseText(source);
            var visitor = new InspectableVisitor();
            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree);
            return visitor;
        }

        [Theory]
        [InlineData(20, 1.0)]
        [InlineData(7, 2.0)]
        [InlineData(1, 3.0)]
        public void ElseIfPicksTheFirstTrueBranch(int x, double expected)
        {
            var v = Run($"SET VAR X={x}; IF $X > 10 THEN SET VAR R=1; ELSE IF $X > 5 THEN SET VAR R=2; ELSE SET VAR R=3; ENDIF");

            Assert.Equal(expected, Convert.ToDouble(v.Vars["R"]));
        }

        [Fact]
        public void ElseIfChainWithoutFinalElseDoesNothingWhenNoBranchMatches()
        {
            var v = Run("SET VAR R=0; IF 0 THEN SET VAR R=1; ELSE IF 0 THEN SET VAR R=2; ELSE IF 0 THEN SET VAR R=3; ENDIF");

            Assert.Equal(0.0, Convert.ToDouble(v.Vars["R"]));
        }

        [Fact]
        public void ElseIfConditionsAreOnlyEvaluatedUntilOneIsTrue()
        {
            var calls = 0;
            var tree = ParseText("IF 0 THEN SET VAR R=1; ELSE IF Probe(1) THEN SET VAR R=2; ELSE IF Probe(2) THEN SET VAR R=3; ENDIF");
            var visitor = new InspectableVisitor();
            visitor.RegisterFormulaFunction("Probe", _ => { calls++; return 1; });

            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree);

            Assert.Equal(1, calls);
        }

        [Fact]
        public void ElseIfWorksAcrossLinesAndInsideOtherBlocks()
        {
            var v = Run("SET VAR N=0;\nFOR I=1 TO 6 DO\n  IF $I < 3 THEN\n    SET VAR N=$N+1;\n  ELSE\n  IF $I < 5 THEN\n    SET VAR N=$N+10;\n  ELSE\n    SET VAR N=$N+100;\n  ENDIF\nENDFOR");

            Assert.Equal(2 * 1 + 2 * 10 + 2 * 100, Convert.ToInt32(v.Vars["N"]));
        }

        [Fact]
        public void ElseWithSeparatedNestedIfKeepsItsOwnEndif()
        {
            // a comment between ELSE and IF makes it a plain ELSE whose body is a nested IF
            var v = Run("SET VAR R=0;\nIF 0 THEN SET VAR R=1; ELSE # nested\n IF 1 THEN SET VAR R=2; ENDIF\nENDIF");

            Assert.Equal(2.0, Convert.ToDouble(v.Vars["R"]));
        }

        [Fact]
        public void WhileRunsUntilConditionIsFalse()
        {
            var v = Run("SET VAR I=0; SET VAR SUM=0; WHILE $I < 5 DO SET VAR SUM=$SUM+$I; SET VAR I=$I+1; ENDWHILE");

            Assert.Equal(10.0, Convert.ToDouble(v.Vars["SUM"]));
            Assert.Equal(5.0, Convert.ToDouble(v.Vars["I"]));
        }

        [Fact]
        public void WhileWithFalseConditionNeverRuns()
        {
            var v = Run("SET VAR R=0; WHILE 0 DO SET VAR R=1; ENDWHILE");

            Assert.Equal(0.0, Convert.ToDouble(v.Vars["R"]));
        }

        [Fact]
        public void WhileThatNeverEndsStopsAtTheIterationCap()
        {
            var tree = ParseText("SET VAR N=0; WHILE 1 DO SET VAR N=$N+1; ENDWHILE");
            var visitor = new InspectableVisitor();

            var ex = Assert.Throws<PdfParserException>(() => visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.Contains("10000 iterations", ex.Message);
            Assert.Equal(PdfDrawerVisitor.MaxWhileIterations, Convert.ToInt32(visitor.Vars["N"]));
        }

        [Theory]
        [InlineData("FOR I=10 TO 0 STEP -2 DO SET VAR S=$S+$I; ENDFOR", 10 + 8 + 6 + 4 + 2 + 0)]
        [InlineData("FOR I=0 TO 10 STEP 5 DO SET VAR S=$S+$I; ENDFOR", 0 + 5 + 10)]
        [InlineData("FOR I=0 TO 9 STEP 5 DO SET VAR S=$S+$I; ENDFOR", 0 + 5)]
        [InlineData("FOR I=1 TO 3 DO SET VAR S=$S+$I; ENDFOR", 6)]
        [InlineData("FOR I=5 TO 1 DO SET VAR S=$S+$I; ENDFOR", 0)]
        [InlineData("FOR I=1 TO 5 STEP -1 DO SET VAR S=$S+$I; ENDFOR", 0)]
        public void ForStepAndBounds(string loop, int expectedSum)
        {
            var v = Run("SET VAR S=0; " + loop);

            Assert.Equal(expectedSum, Convert.ToInt32(v.Vars["S"]));
        }

        [Fact]
        public void ForStepCanBeAFormula()
        {
            var v = Run("SET VAR K=2; SET VAR S=0; FOR I=0 TO 6 STEP $K*2 DO SET VAR S=$S+$I; ENDFOR");

            Assert.Equal(0 + 4, Convert.ToInt32(v.Vars["S"]));
        }

        [Fact]
        public void ForStepZeroThrows()
        {
            var tree = ParseText("FOR I=0 TO 3 STEP 0 DO SET VAR S=1; ENDFOR");

            var ex = Assert.Throws<PdfParserException>(() => new InspectableVisitor().Draw(Mock.Of<IPdfDocumentDrawer>(), tree));

            Assert.StartsWith("FOR STEP must not be 0", ex.Message);
        }

        [Fact]
        public void MissingEndWhileIsDiagnosed()
        {
            var tree = CreateParser().Parse("WHILE 1 DO\n SET VAR X=1;\n");

            Assert.Equal("Missing 'ENDWHILE' for 'WHILE' opened at line 1, col 1.", PdfDslDiagnostics.FormatParseErrors(tree).First());
        }
    }
}
