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
    internal class PdfDrawerForTestsVisitor : PdfDrawerVisitor
    {
        public IDictionary<string, object?> Vars => Variables;

        public IDictionary<string, object?[]> UDFs = new Dictionary<string, object?[]>();
        public PdfDrawerForTestsVisitor()
        {

        }


        protected override bool UdfCustomCall(IPdfDocumentDrawer drawer, string udfName, object?[] arguments)
        {
            UDFs.Add(udfName, arguments);
            return true;
        }
    }
}
