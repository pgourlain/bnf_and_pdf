using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Generator.DrawingGenerator;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PdfSharpDslCore.Generator
{
    [Generator]
    public class DslGenerator : ISourceGenerator
    {
        static DslGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                AssemblyName name = new AssemblyName(args.Name);
                Assembly loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().FullName == name.FullName);
                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }

                string resourceName = $"Namespace.{name.Name}.dll";

                using Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (resourceStream == null)
                {
                    return null;
                }

                using MemoryStream memoryStream = new MemoryStream();
                resourceStream.CopyTo(memoryStream);

                return Assembly.Load(memoryStream.ToArray());
            };
        }
        public void Execute(GeneratorExecutionContext context)
        {
            IEnumerable<AdditionalText> taggedFiles = GetTaggedTextFile(context);
            var nameCodeSequence = SourceFilesFromAdditionalFiles(taggedFiles);
            if (nameCodeSequence != null)
            {
                foreach ((string name, string code) in nameCodeSequence)
                    context.AddSource($"Pdf_{name}.g.cs", SourceText.From(code, Encoding.UTF8));
            }

        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG1
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif 
        }

        static IEnumerable<(string, string)> SourceFilesFromAdditionalFile(AdditionalText file)
        {
            string className = Path.GetFileNameWithoutExtension(file.Path);
            string pdfText = file.GetText()?.ToString()!;
            return new (string, string)[] { (className, GenerateClassFile(className, pdfText)) };
        }
        static IEnumerable<(string, string)> SourceFilesFromAdditionalFiles(IEnumerable<AdditionalText> pathsData)
            => pathsData.SelectMany(f => SourceFilesFromAdditionalFile(f));


        static IEnumerable<AdditionalText> GetTaggedTextFile(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                if (Path.GetExtension(file.Path).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    //
                    var options = context.AnalyzerConfigOptions.GetOptions(file);
                    if (options.TryGetValue("build_metadata.additionalfiles.IsPdfSharpDsl", out string loadTimeString))
                    {
                        yield return file;
                    }

                }
            }
        }

        public static string GenerateClassFile(string className, string pdfText)
        {
            StringBuilder sb = new StringBuilder();

            //// Usings
            sb.Append(@"
#nullable enable
namespace PDfDsl {
    using System.Collections.Generic;
    //another comments
");
            // Class Definition
            sb.Append($"    public class {className} {{\n");

            var parser = new Irony.Parsing.Parser(new PdfGrammar());
            var parsingResult = parser.Parse(pdfText);
            if (parsingResult.HasErrors())
            {

            }
            StringBuilder methodBuilder = new StringBuilder();
            var drawer = new CSharpDrawer(methodBuilder, "drawer.");
            new PdfDrawerVisitor().Draw(drawer, parsingResult);

            sb.AppendLine("     public void WritePdf() {");
            sb.Append(methodBuilder.ToString());
            sb.AppendLine("         }");
            sb.AppendLine("     }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
