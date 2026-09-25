using System.Diagnostics.CodeAnalysis;
using Moq;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    public class UdfReturnTests : BaseTests
    {
        private sealed class InspectableVisitor : PdfDrawerVisitor
        {
            public IDictionary<string, object?> Vars => Variables;
        }

        private InspectableVisitor Run(string source, Mock<IPdfDocumentDrawer>? drawer = null)
        {
            var tree = ParseText(source);
            var visitor = new InspectableVisitor();
            visitor.Draw((drawer ?? new Mock<IPdfDocumentDrawer>()).Object, tree);
            return visitor;
        }

        [Fact]
        public void UdfReturnValueIsUsableInAFormula()
        {
            var v = Run("UDF DOUBLE(X) RETURN $X*2; ENDUDF SET VAR Y=DOUBLE(21);");

            Assert.Equal(42.0, Convert.ToDouble(v.Vars["Y"]));
        }

        [Fact]
        public void UdfCanBeUsedBeforeItsDefinitionAndInsideLargerFormulas()
        {
            var v = Run("SET VAR Y=1+Double(4)*Double(Double(1)); UDF DOUBLE(X) RETURN $X*2; ENDUDF");

            Assert.Equal(1.0 + 8 * 4, Convert.ToDouble(v.Vars["Y"]));
        }

        [Fact]
        public void ReturnEndsTheBodyEvenWhenNested()
        {
            var v = Run(
                "UDF SIGN(X) IF $X < 0 THEN RETURN \"neg\"; ENDIF SET VAR NEVER=0; IF $X == 0 THEN RETURN \"zero\"; ENDIF RETURN \"pos\"; ENDUDF " +
                "SET VAR A=SIGN(-5); SET VAR B=SIGN(0); SET VAR C=SIGN(9);");

            Assert.Equal("neg", v.Vars["A"]);
            Assert.Equal("zero", v.Vars["B"]);
            Assert.Equal("pos", v.Vars["C"]);
            Assert.False(v.Vars.ContainsKey("NEVER"));
        }

        [Fact]
        public void ReturnInsideLoopsStopsTheLoop()
        {
            var v = Run(
                "UDF FIRSTOVER(LIMIT) FOR I=1 TO 100 DO IF $I*$I > $LIMIT THEN RETURN $I; ENDIF ENDFOR RETURN -1; ENDUDF " +
                "UDF WHILEUDF() SET VAR K=0; WHILE 1 DO SET VAR K=$K+1; IF $K == 3 THEN RETURN $K; ENDIF ENDWHILE ENDUDF " +
                "UDF EACHUDF() FOREACH V IN [5,6,7] DO IF $V == 6 THEN RETURN $V; ENDIF ENDFOREACH RETURN 0; ENDUDF " +
                "SET VAR A=FIRSTOVER(50); SET VAR B=WHILEUDF(); SET VAR C=EACHUDF();");

            Assert.Equal(8.0, Convert.ToDouble(v.Vars["A"]));
            Assert.Equal(3.0, Convert.ToDouble(v.Vars["B"]));
            Assert.Equal(6.0, Convert.ToDouble(v.Vars["C"]));
        }

        [Fact]
        public void RecursionWorks()
        {
            var v = Run("UDF FACT(N) IF $N <= 1 THEN RETURN 1; ENDIF RETURN $N*FACT($N-1); ENDUDF SET VAR F=FACT(6);");

            Assert.Equal(720.0, Convert.ToDouble(v.Vars["F"]));
        }

        [Fact]
        public void EndlessRecursionThrowsInsteadOfOverflowingTheStack()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("UDF LOOP(N) RETURN LOOP($N+1); ENDUDF SET VAR X=LOOP(0);"));

            Assert.Contains("256 levels deep", ex.Message);
        }

        [Fact]
        public void UdfVariablesStayLocalToTheCall()
        {
            var v = Run("SET VAR X=1; UDF F(X) SET VAR TMP=9; RETURN $X+1; ENDUDF SET VAR R=F(10);");

            Assert.Equal(11.0, Convert.ToDouble(v.Vars["R"]));
            Assert.Equal(1.0, Convert.ToDouble(v.Vars["X"]));
            Assert.False(v.Vars.ContainsKey("TMP"));
        }

        [Fact]
        public void ReturnedValuesCanBeTextOrLists()
        {
            var v = Run("UDF GREET(N) RETURN \"Hello \"+$N; ENDUDF UDF PAIR() RETURN [1,2]; ENDUDF " +
                        "SET VAR G=GREET(\"Ada\"); SET VAR P=PAIR(); SET VAR S=$P[1];");

            Assert.Equal("Hello Ada", v.Vars["G"]);
            Assert.Equal(2.0, Convert.ToDouble(v.Vars["S"]));
        }

        [Fact]
        public void AUdfCalledAsAFunctionMayAlsoDraw()
        {
            var drawer = new Mock<IPdfDocumentDrawer>();
            var v = Run("UDF STAMP(X) RECT $X,0,10,10; RETURN $X+10; ENDUDF SET VAR NEXT=STAMP(5);", drawer);

            drawer.Verify(d => d.DrawRect(5, 0, 10, 10, false), Times.Once);
            Assert.Equal(15.0, Convert.ToDouble(v.Vars["NEXT"]));
        }

        [Fact]
        public void ReturnEndsAUdfCalledWithCallToo()
        {
            var v = Run("UDF F() SET VAR A=1; RETURN 0; SET VAR B=1; ENDUDF CALL F(); SET VAR C=1;");

            Assert.True(v.Vars.ContainsKey("C"));
            Assert.False(v.Vars.ContainsKey("B"));
        }

        [Fact]
        public void UdfWithoutReturnCannotBeUsedInAFormula()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("UDF F() SET VAR A=1; ENDUDF SET VAR X=F();"));

            Assert.Equal("UDF 'F' is used in a formula but did not RETURN a value.", ex.Message);
        }

        [Fact]
        public void ReturnOutsideAUdfThrowsWithPosition()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("SET VAR A=1;\nRETURN 3;"));

            Assert.Equal("RETURN can only be used inside a UDF at line 2, col 8.", ex.Message);
        }

        [Fact]
        public void WrongArgumentCountThrows()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("UDF F(A,B) RETURN $A+$B; ENDUDF SET VAR X=F(1);"));

            Assert.Contains("provided 1, expected 2", ex.Message);
        }

        [Fact]
        public void RegisteredFunctionsWinOverUdfsOfTheSameName()
        {
            var tree = ParseText("UDF SUM(A) RETURN 99; ENDUDF SET VAR X=Sum(1,2);");
            var visitor = new InspectableVisitor();

            visitor.Draw(Mock.Of<IPdfDocumentDrawer>(), tree);

            Assert.Equal(3.0, Convert.ToDouble(visitor.Vars["X"]));
        }

        [Fact]
        public void UnknownFunctionSuggestsUdfNames()
        {
            var ex = Assert.Throws<PdfParserException>(() => Run("UDF TOTALPRICE(A) RETURN $A; ENDUDF SET VAR X=TotalPrise(1);"));

            Assert.Contains("Did you mean 'TOTALPRICE'?", ex.Message);
        }

        [Fact]
        public void UdfArgumentsCanBeUdfCalls()
        {
            var v = Run("UDF ADD(A,B) RETURN $A+$B; ENDUDF UDF TWICE(X) RETURN ADD($X,$X); ENDUDF SET VAR R=TWICE(ADD(1,2));");

            Assert.Equal(6.0, Convert.ToDouble(v.Vars["R"]));
        }

        [Fact]
        public void UdfFunctionsWorkInDrawingArguments()
        {
            var drawer = new Mock<IPdfDocumentDrawer>();
            Run("UDF HALF(X) RETURN $X/2; ENDUDF FILLRECT 0,0,HALF(100),HALF(20);", drawer);

            drawer.Verify(d => d.DrawRect(0, 0, 50, 10, true), Times.Once);
        }
    }
}
