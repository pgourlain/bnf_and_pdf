﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Parser
{
    public class PdfParserException : Exception
    {
        public PdfParserException(string message) : base(message) 
        { }
        
        public PdfParserException(string message, Exception? innerException) : base(message, innerException)
        { }
    }
}
