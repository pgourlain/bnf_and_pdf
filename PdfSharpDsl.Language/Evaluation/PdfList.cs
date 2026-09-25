using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfSharpDslCore.Evaluation
{
    /// <summary>
    /// Value of a list literal (<c>[1, 2, 3]</c>). A host formula function may return any <see cref="IEnumerable"/>
    /// (except a string) instead, it is handled the same way by <c>FOREACH</c>, <c>Count</c> and indexing.
    /// </summary>
    public sealed class PdfList : IReadOnlyList<object?>
    {
        private readonly object?[] _items;

        public PdfList(IEnumerable<object?> items)
        {
            _items = items.ToArray();
        }

        public int Count => _items.Length;

        public object? this[int index] => _items[index];

        public IEnumerator<object?> GetEnumerator() => ((IEnumerable<object?>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        /// <summary>Displayed as <c>[a, b, c]</c> when a list is concatenated to a text.</summary>
        public override string ToString() => "[" + string.Join(", ", _items.Select(i => i is null ? "" : Convert.ToString(i))) + "]";

        /// <summary>True when <paramref name="value"/> is a list: a <see cref="PdfList"/> or any non-string enumerable.</summary>
        public static bool TryGetItems(object? value, out IReadOnlyList<object?> items)
        {
            switch (value)
            {
                case null:
                case string _:
                    items = Array.Empty<object?>();
                    return false;
                case IReadOnlyList<object?> list:
                    items = list;
                    return true;
                case IEnumerable enumerable:
                    items = enumerable.Cast<object?>().ToArray();
                    return true;
                default:
                    items = Array.Empty<object?>();
                    return false;
            }
        }
    }
}
