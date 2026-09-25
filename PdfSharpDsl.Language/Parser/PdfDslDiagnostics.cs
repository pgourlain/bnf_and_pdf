using System;
using System.Collections.Generic;
using System.Linq;
using Irony;
using Irony.Parsing;

namespace PdfSharpDslCore.Parser
{
    /// <summary>A parse or evaluation problem, with a 1-based source position.</summary>
    public sealed class PdfDslDiagnostic
    {
        public PdfDslDiagnostic(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }

        public int Line { get; }
        public int Column { get; }
        public string Message { get; }

        public override string ToString() => Message;
    }

    /// <summary>
    /// Turns Irony's raw parser messages and run-time evaluation failures into messages a beginner can act on:
    /// unknown instructions with a "did you mean", missing <c>;</c>, unclosed or mismatched blocks.
    /// </summary>
    public static class PdfDslDiagnostics
    {
        // opener -> closer. ROWTEMPLATE is special-cased (statement form vs table form).
        private static readonly Dictionary<string, string> BlockClosers = new Dictionary<string, string>
        {
            ["FOR"] = "ENDFOR",
            ["IF"] = "ENDIF",
            ["UDF"] = "ENDUDF",
            ["MASTER"] = "ENDMASTER",
            ["FLOW"] = "ENDFLOW",
            ["WHILE"] = "ENDWHILE",
            ["FOREACH"] = "ENDFOREACH",
            ["STYLE"] = "ENDSTYLE",
            ["TABLE"] = "ENDTABLE",
            ["HEAD"] = "ENDHEAD",
            ["ROW"] = "ENDROW",
        };

        private sealed class OpenBlock
        {
            public OpenBlock(string opener, string closer, Token token)
            {
                Opener = opener;
                Closer = closer;
                Token = token;
            }

            public string Opener { get; }
            public string Closer { get; }
            public Token Token { get; }
        }

        /// <summary>One diagnostic per error Irony reported, in a friendlier form. Empty when the parse succeeded.</summary>
        public static IReadOnlyList<PdfDslDiagnostic> FromParseTree(ParseTree tree)
        {
            var result = new List<PdfDslDiagnostic>();
            if (tree == null) return result;

            var tokens = tree.Tokens;
            var openBlocks = new List<OpenBlock>();
            var mismatch = ScanBlocks(tokens, openBlocks, out var mismatchToken, out var mismatchMessage);

            foreach (var message in tree.ParserMessages.Where(m => m.Level == ErrorLevel.Error))
            {
                var line = message.Location.Line + 1;
                var col = message.Location.Column + 1;
                var errorToken = FindTokenAt(tokens, message.Location);
                var expected = message.ParserState?.ExpectedTerminals;

                if (mismatch && mismatchToken != null && SameLocation(mismatchToken.Location, message.Location))
                {
                    result.Add(new PdfDslDiagnostic(line, col, mismatchMessage!));
                }
                else if (errorToken != null && IsEof(errorToken) && openBlocks.Count > 0)
                {
                    // report the innermost unclosed block first
                    for (var i = openBlocks.Count - 1; i >= 0; i--)
                    {
                        var open = openBlocks[i];
                        result.Add(new PdfDslDiagnostic(open.Token.Location.Line + 1, open.Token.Location.Column + 1,
                            $"Missing '{open.Closer}' for '{open.Opener}' opened at line {open.Token.Location.Line + 1}, col {open.Token.Location.Column + 1}."));
                    }
                }
                else if (errorToken != null && expected != null && !IsEof(errorToken) && !(errorToken.Terminal is KeyTerm)
                         && InstructionKeywords(expected).Any())
                {
                    var text = errorToken.Text;
                    var suggestion = Suggest(text, InstructionKeywords(expected));
                    var msg = $"Unknown instruction '{text}' at line {line}, col {col}.";
                    if (suggestion != null) msg += $" Did you mean '{suggestion}'?";
                    result.Add(new PdfDslDiagnostic(line, col, msg));
                }
                else if (expected != null && ExpectsSemicolon(expected))
                {
                    var previous = PreviousToken(tokens, message.Location);
                    if (previous != null)
                    {
                        var pLine = previous.Location.Line + 1;
                        var pCol = previous.Location.Column + previous.Length + 1;
                        result.Add(new PdfDslDiagnostic(pLine, pCol, $"Missing ';' after '{previous.Text}' at line {pLine}, col {pCol}."));
                    }
                    else
                    {
                        result.Add(Generic(message, line, col));
                    }
                }
                else
                {
                    result.Add(Generic(message, line, col));
                }
            }

            return result;
        }

        /// <summary>Every diagnostic as one line of text, ready to print.</summary>
        public static IEnumerable<string> FormatParseErrors(ParseTree tree) =>
            FromParseTree(tree).Select(d => d.Message);

        /// <summary>The candidate closest to <paramref name="word"/> (case-insensitive), or null when nothing is close enough.</summary>
        public static string? Suggest(string word, IEnumerable<string> candidates)
        {
            if (string.IsNullOrEmpty(word)) return null;
            var w = word.ToUpperInvariant();
            var maxDistance = Math.Max(1, w.Length / 3);
            string? best = null;
            var bestDistance = int.MaxValue;
            foreach (var candidate in candidates.Distinct())
            {
                var d = Levenshtein(w, candidate.ToUpperInvariant());
                if (d < bestDistance)
                {
                    best = candidate;
                    bestDistance = d;
                }
            }

            return bestDistance <= maxDistance && bestDistance < w.Length ? best : null;
        }

        /// <summary>Message for a variable used before being defined. <paramref name="location"/> may be null.</summary>
        public static string UndefinedVariable(string name, SourceLocation? location, IEnumerable<string> knownVariables)
        {
            var msg = $"Variable '${name}' is not defined{AtLocation(location)}.";
            var suggestion = Suggest(name, knownVariables);
            if (suggestion != null) msg += $" Did you mean '${suggestion}'?";
            return msg;
        }

        /// <summary>Message for a call to a function that is neither built-in nor registered by the host.</summary>
        public static string UnknownFunction(string name, SourceLocation? location, IEnumerable<string> knownFunctions)
        {
            var msg = $"Unknown function '{name}'{AtLocation(location)}.";
            var suggestion = Suggest(name, knownFunctions);
            if (suggestion != null) msg += $" Did you mean '{suggestion}'?";
            return msg;
        }

        [ThreadStatic]
        private static string? _currentFile;

        /// <summary>
        /// Name of the INCLUDEd file whose statements are being executed, null for the main file. Run-time
        /// messages built with <see cref="AtLocation"/> mention it, since a line number alone is ambiguous.
        /// </summary>
        internal static IDisposable UseFile(string? file) => new FileScope(file);

        internal static string AtLocation(SourceLocation? location)
        {
            if (location == null) return string.Empty;
            var text = $" at line {location.Value.Line + 1}, col {location.Value.Column + 1}";
            return _currentFile == null ? text : $"{text} of {_currentFile}";
        }

        private sealed class FileScope : IDisposable
        {
            private readonly string? _previous;

            public FileScope(string? file)
            {
                _previous = _currentFile;
                _currentFile = file;
            }

            public void Dispose() => _currentFile = _previous;
        }

        private static PdfDslDiagnostic Generic(LogMessage message, int line, int col) =>
            new PdfDslDiagnostic(line, col, $"{message.Message} at line {line}, col {col}.");

        private static bool ScanBlocks(TokenList tokens, List<OpenBlock> open, out Token? mismatchToken, out string? mismatchMessage)
        {
            mismatchToken = null;
            mismatchMessage = null;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var text = token.Text;
                // Irony scans a keyword the parser does not expect as a plain identifier, so the offending
                // (last) token may be a closer that is not a KeyTerm.
                var isErrorToken = i == tokens.Count - 1 && IsCloser(text);
                if (!(token.Terminal is KeyTerm) && !isErrorToken) continue;

                if (text == "ROWTEMPLATE")
                {
                    // statement form is "ROWTEMPLATE Count=...", the table form "ROWTEMPLATE <count> COL ... ENDROW"
                    var isStatement = i + 1 < tokens.Count && tokens[i + 1].Text == "Count";
                    open.Add(new OpenBlock(text, isStatement ? "ENDROWTEMPLATE" : "ENDROW", token));
                }
                else if (BlockClosers.TryGetValue(text, out var closer))
                {
                    open.Add(new OpenBlock(text, closer, token));
                }
                else if (text.StartsWith("END", StringComparison.Ordinal) && IsCloser(text))
                {
                    if (open.Count == 0)
                    {
                        mismatchToken = token;
                        mismatchMessage = $"'{text}' at line {token.Location.Line + 1}, col {token.Location.Column + 1} has no matching block to close.";
                        return true;
                    }

                    var top = open[open.Count - 1];
                    if (top.Closer != text)
                    {
                        mismatchToken = token;
                        mismatchMessage = $"'{text}' at line {token.Location.Line + 1}, col {token.Location.Column + 1} does not match '{top.Opener}' opened at line {top.Token.Location.Line + 1}, col {top.Token.Location.Column + 1}. Expected '{top.Closer}'.";
                        return true;
                    }

                    open.RemoveAt(open.Count - 1);
                }
            }

            return false;
        }

        private static bool IsCloser(string text) =>
            text == "ENDROWTEMPLATE" || BlockClosers.Values.Contains(text);

        private static IEnumerable<string> InstructionKeywords(TerminalSet expected) =>
            expected.OfType<KeyTerm>().Select(k => k.Text).Where(t => t.Length > 1 && t.All(char.IsUpper) && !t.StartsWith("END", StringComparison.Ordinal) && t != "ELSE");

        private static bool ExpectsSemicolon(TerminalSet expected) =>
            expected.OfType<KeyTerm>().Any(k => k.Text == ";");

        private static bool IsEof(Token token) => token.Terminal == null || token.Terminal.Name == "EOF";

        private static bool SameLocation(SourceLocation a, SourceLocation b) => a.Line == b.Line && a.Column == b.Column;

        private static Token? FindTokenAt(TokenList tokens, SourceLocation location)
        {
            for (var i = tokens.Count - 1; i >= 0; i--)
            {
                if (SameLocation(tokens[i].Location, location)) return tokens[i];
            }

            return null;
        }

        private static Token? PreviousToken(TokenList tokens, SourceLocation location)
        {
            for (var i = tokens.Count - 1; i >= 0; i--)
            {
                if (SameLocation(tokens[i].Location, location))
                {
                    return i > 0 ? tokens[i - 1] : null;
                }
            }

            return null;
        }

        private static int Levenshtein(string a, string b)
        {
            // optimal string alignment: like Levenshtein, but swapping two neighbours ("titel") costs 1
            var d = new int[a.Length + 1, b.Length + 1];
            for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (var j = 0; j <= b.Length; j++) d[0, j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                }
            }

            return d[a.Length, b.Length];
        }
    }
}
