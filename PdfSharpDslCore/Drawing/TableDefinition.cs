using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;

namespace PdfSharpDslCore.Drawing
{
    
    public class TableDefinition
    {
        public List<ColumnDefinition> Columns { get; private set; } = new();
        public bool ShowHeader { get; set; } = true;
        public double TopMarginOnPageBreak { get; set; }
        //header height, should be measure if not specified
        public double? HeaderHeight { get; set; }
        public TrimMargins CellMargin { get; set; } = new TrimMargins() { All = 1 };
        public XBrush? HeaderBackColor { get; set; }


        public List<RowDefinition> Rows { get; private set; } = new();


        public double ColWidth(int i)
        {
            return 0;
        }

        public XStringAlignment Alignment(int i)
        {
            return Columns[i].Alignment;
        }
    }

    public class ColumnDefinition
    {
        public string ColumnHeaderName { get; set; } = string.Empty;

        public double? DesiredWidth { get; set; } = null;
        public double? MaxWidth { get; set; } = null;

        public XStringAlignment Alignment { get; set; } = XStringAlignment.Near;
        public XFont? Font { get; set; }
        public XBrush? Brush { get; set; }
        public XBrush? BackColor { get; set; }

    }

    public class RowDefinition
    {
        public double? DesiredHeight { get; set; }
        public double? MaxHeight { get; set; }

        /// <summary>
        /// string because there is only draw text
        /// </summary>
        public string[] Data { get; set; } = Array.Empty<string>();
    }
}