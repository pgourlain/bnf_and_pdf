using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpDslCore.Drawing;
using PdfSharpDslCore.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Generator.DrawingGenerator
{

    class CSharpVisitor : PdfDrawerVisitor
    {
         public CSharpVisitor(string baseDirectory) : base (baseDirectory)
         {

         }
    }
}