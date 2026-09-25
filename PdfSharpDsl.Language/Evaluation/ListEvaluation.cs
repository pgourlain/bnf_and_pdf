using System.Linq;

namespace PdfSharpDslCore.Evaluation
{
    internal class ListEvaluation : Evaluation<object>
    {
        private readonly IEvaluation<object>[] _items;

        public ListEvaluation(IEvaluation<object>[] items)
        {
            _items = items;
        }

        public override object? Value => new PdfList(_items.Select(i => i.Value));
    }
}
