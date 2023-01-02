using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Generator.DrawingGenerator
{
    internal class CSharpGeneratorState : IGeneratorState
    {
        private StringBuilder _methodBuilder;
        private int _indentation;

        public CSharpGeneratorState(StringBuilder methodBuilder)
        {
            this._methodBuilder = methodBuilder;
        }

        public StringBuilder Code => _methodBuilder;

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
    }
}
