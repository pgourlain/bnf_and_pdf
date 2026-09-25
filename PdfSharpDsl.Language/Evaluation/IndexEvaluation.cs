using System;
using Irony.Parsing;
using PdfSharpDslCore.Parser;

namespace PdfSharpDslCore.Evaluation
{
    /// <summary><c>$LIST[i]</c>, and <c>$LIST[i][j]</c> for a list of lists. Indexes are 0-based.</summary>
    internal class IndexEvaluation : Evaluation<object>
    {
        private readonly IEvaluation<object> _target;
        private readonly IEvaluation<object>[] _indexes;
        private readonly SourceLocation?[] _locations;

        public IndexEvaluation(IEvaluation<object> target, IEvaluation<object>[] indexes, SourceLocation?[] locations)
        {
            _target = target;
            _indexes = indexes;
            _locations = locations;
        }

        public override object? Value
        {
            get
            {
                var current = _target.Value;
                for (var level = 0; level < _indexes.Length; level++)
                {
                    var where = PdfDslDiagnostics.AtLocation(_locations[level]);
                    if (!PdfList.TryGetItems(current, out var items))
                    {
                        throw new PdfParserException($"Cannot use [index] on a value that is not a list{where}.");
                    }

                    var indexValue = _indexes[level].Value;
                    double index;
                    try
                    {
                        index = Convert.ToDouble(indexValue);
                    }
                    catch (Exception e) when (e is FormatException || e is InvalidCastException)
                    {
                        throw new PdfParserException($"List index '{indexValue}' is not a number{where}.");
                    }

                    if (index < 0 || index >= items.Count || index != Math.Floor(index))
                    {
                        throw new PdfParserException($"List index {indexValue} is out of range (the list has {items.Count} item(s), indexes start at 0){where}.");
                    }

                    current = items[(int)index];
                }

                return current;
            }
        }
    }
}
