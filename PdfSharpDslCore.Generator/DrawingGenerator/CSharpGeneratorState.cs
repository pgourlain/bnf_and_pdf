using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Generator.DrawingGenerator
{
    internal class CSharpGeneratorState : IGeneratorState
    {
        private readonly string _className;
        private StringBuilder _methodBuilder;
        private StringBuilder _membersBuilder;

        private int _indentation;

        public CSharpGeneratorState(string className)
        {
            this._className = className;
            _methodBuilder = new StringBuilder();
            _membersBuilder = new StringBuilder();
        }

        public StringBuilder Code => ConstructCode();

        public int Indentation
        {
            get => _indentation; set 
            {
                if (value < 0) throw new ArgumentOutOfRangeException("must be >= 0");
                _indentation = value;
            }
        }

        public void AppendLine(string text)
        {
            var spaces = string.Empty;
            if (_indentation > 0)
            {
                string fmt = "{0,-" + (_indentation * 4).ToString() + "}";
                spaces = string.Format(fmt, " ");
            }
            _methodBuilder.AppendLine(spaces+text);
        }

        public void AppendMembersLine(string text)
        {
            var spaces = string.Empty;
            if (_indentation > 0)
            {
                string fmt = "{0,-" + (_indentation * 4).ToString() + "}";
                spaces = string.Format(fmt, " ");
            }
            _membersBuilder.AppendLine(spaces + text);
        }


        private StringBuilder ConstructCode()
        {
            StringBuilder sb = new StringBuilder();
            //// Usings
            sb.Append(@"
#nullable enable
namespace PDfDsl {
    using System.Collections.Generic;
    using PdfSharpCore.Drawing;
    using PdfSharpDslCore.Drawing;
    //another comments
");
            // Class Definition
            sb.AppendLine(@$"    
    internal partial class {_className} 
    {{");
            sb.AppendLine(_membersBuilder.ToString());  
            sb.AppendLine(@"     
        public void WritePdf(IPdfDocumentDrawer drawer) 
        {");
            sb.Append(_methodBuilder.ToString());
            sb.AppendLine(@"
        }");
            sb.AppendLine(@"
    }");
            sb.AppendLine(@"
}");
            return sb;
        }
    }
}
