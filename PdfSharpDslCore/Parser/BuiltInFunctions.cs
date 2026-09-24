using System;
using System.Globalization;
using System.Linq;

namespace PdfSharpDslCore.Parser
{
    /// <summary>
    /// Formula functions available without host registration. Registered before any host
    /// registration, so a host can still override a built-in by name (RegisterFormulaFunction
    /// replaces an existing entry).
    /// </summary>
    public static class BuiltInFunctions
    {
        public static void Register<TState>(PdfVisitor<TState> visitor)
        {
            // Math
            visitor.RegisterFormulaFunction("Min", args => { RequireAtLeast("Min", args, 1); return args.Select(D).Min(); });
            visitor.RegisterFormulaFunction("Max", args => { RequireAtLeast("Max", args, 1); return args.Select(D).Max(); });
            visitor.RegisterFormulaFunction("Sum", args => { RequireAtLeast("Sum", args, 1); return args.Select(D).Sum(); });
            visitor.RegisterFormulaFunction("Abs", args => { RequireArity("Abs", args, 1, 1); return Math.Abs(D(args[0])); });
            visitor.RegisterFormulaFunction("Round", args =>
            {
                RequireArity("Round", args, 1, 2);
                var digits = args.Length > 1 ? (int)D(args[1]) : 0;
                return Math.Round(D(args[0]), digits, MidpointRounding.AwayFromZero);
            });
            visitor.RegisterFormulaFunction("Floor", args => { RequireArity("Floor", args, 1, 1); return Math.Floor(D(args[0])); });
            visitor.RegisterFormulaFunction("Ceil", args => { RequireArity("Ceil", args, 1, 1); return Math.Ceiling(D(args[0])); });
            visitor.RegisterFormulaFunction("Sqrt", args => { RequireArity("Sqrt", args, 1, 1); return Math.Sqrt(D(args[0])); });
            visitor.RegisterFormulaFunction("Pow", args => { RequireArity("Pow", args, 2, 2); return Math.Pow(D(args[0]), D(args[1])); });

            // String
            visitor.RegisterFormulaFunction("Upper", args => { RequireArity("Upper", args, 1, 1); return S(args[0]).ToUpperInvariant(); });
            visitor.RegisterFormulaFunction("Lower", args => { RequireArity("Lower", args, 1, 1); return S(args[0]).ToLowerInvariant(); });
            visitor.RegisterFormulaFunction("Len", args => { RequireArity("Len", args, 1, 1); return (double)S(args[0]).Length; });
            visitor.RegisterFormulaFunction("Substr", args =>
            {
                RequireArity("Substr", args, 2, 3);
                var str = S(args[0]);
                var start = Math.Clamp((int)D(args[1]), 0, str.Length);
                var maxLen = str.Length - start;
                var len = args.Length > 2 ? Math.Clamp((int)D(args[2]), 0, maxLen) : maxLen;
                return str.Substring(start, len);
            });
            visitor.RegisterFormulaFunction("Replace", args =>
            {
                RequireArity("Replace", args, 3, 3);
                return S(args[0]).Replace(S(args[1]), S(args[2]));
            });
            visitor.RegisterFormulaFunction("Trim", args => { RequireArity("Trim", args, 1, 1); return S(args[0]).Trim(); });

            // Format
            visitor.RegisterFormulaFunction("Format", args =>
            {
                RequireArity("Format", args, 2, 2);
                var fmt = S(args[1]);
                return args[0] is DateTime dt
                    ? dt.ToString(fmt, CultureInfo.InvariantCulture)
                    : D(args[0]).ToString(fmt, CultureInfo.InvariantCulture);
            });

            // Date. Only Format(Now()/Today(), "...") is guaranteed a stable, invariant
            // representation; concatenating one with a string via '+' falls back to
            // DateTime's default (culture-dependent) ToString(), since the evaluator's
            // string operator does not special-case DateTime.
            visitor.RegisterFormulaFunction("Now", args => { RequireArity("Now", args, 0, 0); return DateTime.Now; });
            visitor.RegisterFormulaFunction("Today", args => { RequireArity("Today", args, 0, 0); return DateTime.Today; });

            // Logic
            visitor.RegisterFormulaFunction("Iif", args =>
            {
                RequireArity("Iif", args, 3, 3);
                return Convert.ToBoolean(args[0], CultureInfo.InvariantCulture) ? args[1] : args[2];
            });
        }

        private static double D(object o) => Convert.ToDouble(o, CultureInfo.InvariantCulture);
        private static string S(object o) => Convert.ToString(o, CultureInfo.InvariantCulture) ?? string.Empty;

        private static void RequireArity(string function, object[] args, int min, int max)
        {
            if (args.Length < min || args.Length > max)
                throw new PdfParserException(min == max
                    ? $"'{function}' expects {min} argument(s), got {args.Length}."
                    : $"'{function}' expects between {min} and {max} argument(s), got {args.Length}.");
        }

        private static void RequireAtLeast(string function, object[] args, int min)
        {
            if (args.Length < min)
                throw new PdfParserException($"'{function}' expects at least {min} argument(s), got {args.Length}.");
        }
    }
}
