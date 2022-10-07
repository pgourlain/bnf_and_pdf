using PdfSharpCore.Drawing;

namespace Pdf
{
    public class TableDefinition
    {
        public List<ColumnDefinition> Columns { get; init; } = new List<ColumnDefinition>();
        public bool ShowHeader { get; set; } = true;

        public double ColWidth(int i)
        {
            return Columns[i].Width;
        }

        public XStringAlignment Alignment(int i)
        {
            return Columns[i].Alignment;
        }
    }

    public class ColumnDefinition
    {
        public string ColumnHeaderName { get; set; }
        public double Width { get; set; }

        public XStringAlignment Alignment { get; set; } = XStringAlignment.Near;

    }
}