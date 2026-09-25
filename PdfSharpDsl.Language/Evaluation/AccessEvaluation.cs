using System;
using System.Linq;
using Irony.Parsing;
using PdfSharpDslCore.Parser;

namespace PdfSharpDslCore.Evaluation
{
    /// <summary>One step after a variable: <c>[index]</c> or <c>.member</c>.</summary>
    internal sealed class AccessStep
    {
        public AccessStep(IEvaluation<object>? index, string? member, SourceLocation? location)
        {
            Index = index;
            Member = member;
            Location = location;
        }

        public IEvaluation<object>? Index { get; }
        public string? Member { get; }
        public SourceLocation? Location { get; }
    }

    /// <summary>
    /// <c>$LIST[i]</c> (0-based), <c>$RECORD.field</c> and any chain of them: <c>$orders[0].customer</c>,
    /// <c>$M[1][0]</c>.
    /// </summary>
    internal class AccessEvaluation : Evaluation<object>
    {
        private readonly IEvaluation<object> _target;
        private readonly AccessStep[] _steps;

        public AccessEvaluation(IEvaluation<object> target, AccessStep[] steps)
        {
            _target = target;
            _steps = steps;
        }

        public override object? Value
        {
            get
            {
                var current = _target.Value;
                foreach (var step in _steps)
                {
                    current = step.Member != null ? ReadMember(current, step) : ReadIndex(current, step);
                }

                return current;
            }
        }

        private static object? ReadMember(object? current, AccessStep step)
        {
            var where = PdfDslDiagnostics.AtLocation(step.Location);
            if (current is null)
            {
                throw new PdfParserException($"Cannot read '.{step.Member}' of an empty value{where}.");
            }

            if (PdfMembers.TryGet(current, step.Member!, out var value))
            {
                return value;
            }

            var names = PdfMembers.Names(current).ToList();
            var message = $"There is no field '{step.Member}'{where}.";
            var suggestion = PdfDslDiagnostics.Suggest(step.Member!, names);
            if (suggestion != null) message += $" Did you mean '{suggestion}'?";
            else if (names.Count > 0) message += $" Available: {string.Join(", ", names)}.";
            throw new PdfParserException(message);
        }

        private static object? ReadIndex(object? current, AccessStep step)
        {
            var where = PdfDslDiagnostics.AtLocation(step.Location);
            if (!PdfList.TryGetItems(current, out var items))
            {
                throw new PdfParserException($"Cannot use [index] on a value that is not a list{where}.");
            }

            var indexValue = step.Index!.Value;
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

            return items[(int)index];
        }
    }
}
