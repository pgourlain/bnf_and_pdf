using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Generator;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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

        [Fact]
        public void GeneratorProducesCompilableSourceFromTaggedAdditionalFile()
        {
            var source = CSharpSyntaxTree.ParseText("namespace Consumer { internal class Marker { } }");
            var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Append(typeof(IPdfDocumentDrawer).Assembly.Location)
                .Append(typeof(PdfColor).Assembly.Location)
                .Distinct()
                .Select(path => MetadataReference.CreateFromFile(path));
            var compilation = CSharpCompilation.Create(
                "Consumer",
                new[] { source },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var additionalFile = new InMemoryAdditionalText("sample.txt", "NEWPAGE;");
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new DslGenerator() },
                new[] { additionalFile },
                (CSharpParseOptions)source.Options,
                new PdfDslOptionsProvider());

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

            Assert.Empty(generatorDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            Assert.Equal(2, outputCompilation.SyntaxTrees.Count());
            Assert.Empty(outputCompilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        }

        private sealed class InMemoryAdditionalText : AdditionalText
        {
            private readonly SourceText _text;

            public InMemoryAdditionalText(string path, string text)
            {
                Path = path;
                _text = SourceText.From(text, Encoding.UTF8);
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
        }

        private sealed class PdfDslOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private static readonly AnalyzerConfigOptions Empty = new TestAnalyzerConfigOptions(false);
            private static readonly AnalyzerConfigOptions PdfDsl = new TestAnalyzerConfigOptions(true);

            public override AnalyzerConfigOptions GlobalOptions => Empty;

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Empty;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => PdfDsl;
        }

        private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly bool _isPdfDsl;

            public TestAnalyzerConfigOptions(bool isPdfDsl) => _isPdfDsl = isPdfDsl;

            public override bool TryGetValue(string key, out string value)
            {
                if (_isPdfDsl && key == "build_metadata.additionalfiles.IsPdfSharpDsl")
                {
                    value = "true";
                    return true;
                }

                value = string.Empty;
                return false;
            }
        }
    }
}
