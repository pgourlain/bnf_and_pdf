using PdfSharpDslCore.Evaluation;
using PdfSharpDslCore.Parser;/**/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]

    public class FormulaTests : BaseTests
    {
        [Theory()]
        [InlineData("SET VAR X=180+80+80+80+80;")]
        [InlineData("SET VAR X=180+80;")]
        [InlineData("SET VAR X=180+80+80+80+80;SET VAR X=180+80;")]
        public void FormulasCheck(string formula)
        {
            var parsingResult = ParseText(formula);
            Assert.False(parsingResult.HasErrors());
        }


        [Theory()]
        [InlineData("180+80+80+80+80")]
        [InlineData("180+80")]
        public void OnlyFormulasCheck(string formula)
        {
            var parsingResult = ParseText< FormulaExpressionGrammar>(formula);
            Assert.False(parsingResult.HasErrors());
        }

        [Theory()]
        [InlineData("SET VAR X=180+80+80+80+Random();")]
        [InlineData("SET FONT Name=GetSignatureFont() Size=GetSignatureFontSize();")]
        public void FormulasWithCustomFunctionCheck(string formula)
        {
            var parsingResult = ParseText(formula);
            Assert.False(parsingResult.HasErrors());
        }


        [Theory()]
        [InlineData("SET VAR X=180>80;")]
        [InlineData("SET VAR X=180<80;")]
        [InlineData("SET VAR X=180>=80;")]
        [InlineData("SET VAR X=180<=80;")]
        [InlineData("SET VAR X=180<>80;")]
        [InlineData("SET VAR X=180==80;")]
        public void ForumlasWithCondition(string formula)
        {
            var parsingResult = ParseText(formula);
            Assert.False(parsingResult.HasErrors());
        }

        [Fact]
        public void ConvertTests()
        {
            Assert.True(Convert.ToBoolean(1.0));
            Assert.True(Convert.ToBoolean(50.0));
            Assert.False(Convert.ToBoolean(0));
            Assert.True(Convert.ToBoolean(-12));

            Assert.Equal(1.0, Convert.ToDouble(true));
            Assert.Equal(0.0, Convert.ToDouble(false));
        }

        [Theory]
        [InlineData((int)BinaryOperation.Sub, 1.0)]
        [InlineData((int)BinaryOperation.Mul, 30.0)]
        [InlineData((int)BinaryOperation.Div, 1.2)]
        [InlineData((int)BinaryOperation.Mod, 1.0)]
        public void BinaryNumericOperations(int operation, double expected)
        {
            var evaluation = new BinaryEvaluation(
                new ConstantEvaluation<object>(6),
                new ConstantEvaluation<object>(5),
            (BinaryOperation)operation);

            Assert.Equal(expected, evaluation.Value);
        }

        [Theory]
        [InlineData((int)BinaryOperation.And, true, false, false)]
        [InlineData((int)BinaryOperation.Or, false, true, true)]
        [InlineData((int)BinaryOperation.Superior, 2, 1, true)]
        [InlineData((int)BinaryOperation.SuperiorOrEquals, 2, 2, true)]
        [InlineData((int)BinaryOperation.Inferior, 1, 2, true)]
        [InlineData((int)BinaryOperation.InferiorOrEquals, 2, 2, true)]
        [InlineData((int)BinaryOperation.Equals, "same", "same", true)]
        [InlineData((int)BinaryOperation.NotEquals, "left", "right", true)]
        public void BinaryBooleanOperations(int operation, object left, object right, bool expected)
        {
            var evaluation = new BinaryEvaluation(
                new ConstantEvaluation<object>(left),
                new ConstantEvaluation<object>(right),
            (BinaryOperation)operation);

            Assert.Equal(expected, evaluation.Value);
        }

        [Fact]
        public void BinaryOperationsReportInvalidOperands()
        {
            var nullOperand = new BinaryEvaluation(
                new ConstantEvaluation<object>(null!),
                new ConstantEvaluation<object>(1),
                BinaryOperation.Add);
            var unsupportedStringOperation = new BinaryEvaluation(
                new ConstantEvaluation<object>("left"),
                new ConstantEvaluation<object>("right"),
                BinaryOperation.Sub);
            var invalidComparison = new BinaryEvaluation(
                new ConstantEvaluation<object>("left"),
                new ConstantEvaluation<object>(1),
                BinaryOperation.Superior);

            Assert.Throws<InvalidOperationException>(() => nullOperand.Value);
            Assert.Throws<NotSupportedException>(() => unsupportedStringOperation.Value);
            Assert.Throws<NotSupportedException>(() => invalidComparison.Value);
        }

        [Theory]
        [InlineData((int)BinaryOperation.Add, 5, 5.0)]
        [InlineData((int)BinaryOperation.Sub, 5, -5.0)]
        [InlineData((int)BinaryOperation.Add, "value", "value")]
        [InlineData((int)BinaryOperation.Sub, "value", "-value")]
        public void UnaryOperations(int operation, object value, object expected)
        {
            var evaluation = new UnaryEvaluation(new ConstantEvaluation<object>(value), (BinaryOperation)operation);

            Assert.Equal(expected, evaluation.Value);
        }

        [Fact]
        public void UnaryOperationsReportInvalidOperands()
        {
            var nullOperand = new UnaryEvaluation(new ConstantEvaluation<object>(null!), BinaryOperation.Add);
            var unsupportedOperation = new UnaryEvaluation(new ConstantEvaluation<object>(1), BinaryOperation.Mul);

            Assert.Throws<InvalidOperationException>(() => nullOperand.Value);
            Assert.Throws<InvalidOperationException>(() => unsupportedOperation.Value);
        }

        [Fact]
        public void MissingVariableThrows()
        {
            var evaluation = new VariableEvaluation("missing", new Dictionary<string, object?>());

            Assert.Throws<ArgumentOutOfRangeException>(() => evaluation.Value);
        }
    }
}
