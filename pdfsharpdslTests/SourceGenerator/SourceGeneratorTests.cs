using PdfSharpDslCore.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdslTests.SourceGenerator
{

    [ExcludeFromCodeCoverage]
    public class SourceGeneratorTests
    {
        [Theory]
        [InlineData("pdf-to-csharp-source.txt")]
        public void TestSourceGeneration(string file)
        {
            var input = File.ReadAllText($"./ValidInputFiles/{file}");
            var result = DslGenerator.GenerateClassFile("test1", input, null);

            Assert.NotNull(result);
        }
    }
}
