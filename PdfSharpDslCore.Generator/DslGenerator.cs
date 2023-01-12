using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Generator.DrawingGenerator;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    //entry point of generator
    [Generator]
    public class DslGenerator : IIncrementalGenerator
    {
        static DslGenerator()
        {
        }

//        public void Execute(GeneratorExecutionContext context)
//        {
//            IEnumerable<AdditionalText> taggedFiles = GetTaggedTextFile(context).ToArray();

//            var nameCodeSequence = SourceFilesFromAdditionalFiles(taggedFiles);
//            if (nameCodeSequence != null)
//            {
//                foreach ((string name, string code) in nameCodeSequence)
//                {
//                    var sourceText = SourceText.From(code, Encoding.UTF8);
//                    context.AddSource($"{name}.g.cs", sourceText);
//                }
//            }

//        }

//        public void Initialize(GeneratorInitializationContext context)
//        {
//#if DEBUG1
//            if (!Debugger.IsAttached)
//            {
//                Debugger.Launch();
//            }
//#endif
//        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var firsts = context.AdditionalTextsProvider
                    .Combine(context.AnalyzerConfigOptionsProvider)
                    .Where(file => IsTaggedTextFile(context, file))
                    .Select((f,_) => f.Left);
            var nexts = firsts.Collect();
            var finals =context.CompilationProvider.Combine(nexts);
            context.RegisterSourceOutput(finals, (spc, source) =>
            {
                Execute(source.Left, spc, source.Right);
            });
        }



        private bool IsTaggedTextFile(IncrementalGeneratorInitializationContext context, (AdditionalText file, AnalyzerConfigOptionsProvider options) file)
        {
            if (Path.GetExtension(file.file.Path).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                //
                var options = file.options.GetOptions(file.file);
                if (options.TryGetValue("build_metadata.additionalfiles.IsPdfSharpDsl", out string loadTimeString))
                {
                    return true;
                }

            }
            return false;
        }

        private void Execute(Compilation left, SourceProductionContext context, ImmutableArray<AdditionalText> right)
        {
            var nameCodeSequence = SourceFilesFromAdditionalFiles(right);
            if (nameCodeSequence != null)
            {
                foreach ((string name, string code) in nameCodeSequence)
                {
                    var sourceText = SourceText.From(code, Encoding.UTF8);
                    context.AddSource($"{name}.g.cs", sourceText);
                }
            }
        }


        #region static methods

        static IEnumerable<(string, string)> SourceFilesFromAdditionalFile(AdditionalText file)
        {
            string className = Path.GetFileNameWithoutExtension(file.Path);
            className = className.Replace(' ', '_');
            string pdfText = file.GetText()?.ToString()!;
            return new (string, string)[] { (className, GenerateClassFile(className, pdfText, Path.GetDirectoryName(file.Path))) };
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

        public static string GenerateClassFile(string className, string pdfText, string directory)
        {

            var parser = new Irony.Parsing.Parser(new PdfGrammar());
            var parsingResult = parser.Parse(pdfText);
            if (parsingResult.HasErrors())
            {

            }
            StringBuilder methodBuilder = new StringBuilder();
            var state = new CSharpGeneratorState(className);            
            //var drawer = new CSharpDrawer(methodBuilder, "drawer.");
            new CSharpVisitor(directory, "drawer.").Draw(state, parsingResult);

            return state.Code.ToString();
        }


        #endregion
    }
}
