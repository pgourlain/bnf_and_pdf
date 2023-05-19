using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using PdfSharpCore.Drawing;

namespace pdfsharpdslTests
{
    [ExcludeFromCodeCoverage]
    internal class PdfDrawerForTestsVisitor : PdfDrawerVisitor
    {
        public IDictionary<string, object?> Vars => Variables;

        public readonly IDictionary<string, object?[]> UDFs = new Dictionary<string, object?[]>();
        public PdfDrawerForTestsVisitor()
        { }

        protected override void UdfCall(IPdfDocumentDrawer drawer, string fnName, string[]? parameterNames, object?[] parameterValues,
            ParseTreeNode? udfBody)
        {
            UDFs.Add(fnName, parameterValues);
        }
    }
}
