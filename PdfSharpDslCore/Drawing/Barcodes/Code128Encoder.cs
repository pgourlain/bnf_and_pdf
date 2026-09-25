using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharpDslCore.Parser;

namespace PdfSharpDslCore.Drawing.Barcodes
{
    /// <summary>
    /// Code 128 encoder (code sets B and C; C is used automatically for runs of 4 or more digits).
    /// Code set A (control characters) is not supported: the text must be printable ASCII (32 to 126).
    /// </summary>
    internal static class Code128Encoder
    {
        private const int CodeB = 100;
        private const int CodeC = 99;
        private const int StartB = 104;
        private const int StartC = 105;
        private const int Stop = 106;

        /// <summary>Quiet zone, in modules, added on each side by <see cref="Encode"/>.</summary>
        public const int QuietZoneModules = 10;

        // Widths (bar, space, bar, space, bar, space) of every symbol value 0..106; the stop symbol has 7 widths.
        internal static readonly string[] Patterns =
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112",
        };

        /// <summary>Symbol values of <paramref name="text"/>: start, data, checksum and stop.</summary>
        internal static IReadOnlyList<int> Values(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new PdfParserException("BARCODE Code128 needs a text to encode.");
            foreach (var c in text)
            {
                if (c < 32 || c > 126)
                    throw new PdfParserException($"BARCODE Code128 supports printable ASCII only (32 to 126), found '{c}' (U+{(int)c:X4}).");
            }

            var values = new List<int>();
            var i = 0;
            var run = DigitRun(text, 0);
            var inC = (run == text.Length && run == 2) || (run >= 4 && run % 2 == 0);
            values.Add(inC ? StartC : StartB);

            while (i < text.Length)
            {
                run = DigitRun(text, i);
                if (inC)
                {
                    if (run >= 2)
                    {
                        values.Add(int.Parse(text.Substring(i, 2)));
                        i += 2;
                    }
                    else
                    {
                        values.Add(CodeB);
                        inC = false;
                    }
                }
                else if (run >= 4)
                {
                    //an odd run keeps its first digit in set B so that the rest pairs up
                    if (run % 2 == 1)
                    {
                        values.Add(text[i] - 32);
                        i++;
                    }

                    values.Add(CodeC);
                    inC = true;
                }
                else
                {
                    values.Add(text[i] - 32);
                    i++;
                }
            }

            var checksum = values[0];
            for (var position = 1; position < values.Count; position++)
            {
                checksum += values[position] * position;
            }

            values.Add(checksum % 103);
            values.Add(Stop);
            return values;
        }

        /// <summary>Modules of the barcode (true = bar), with a quiet zone on each side.</summary>
        public static bool[] Encode(string text)
        {
            var modules = new List<bool>();
            modules.AddRange(Enumerable.Repeat(false, QuietZoneModules));
            foreach (var value in Values(text))
            {
                var bar = true;
                foreach (var width in Patterns[value])
                {
                    modules.AddRange(Enumerable.Repeat(bar, width - '0'));
                    bar = !bar;
                }
            }

            modules.AddRange(Enumerable.Repeat(false, QuietZoneModules));
            return modules.ToArray();
        }

        private static int DigitRun(string text, int start)
        {
            var count = 0;
            while (start + count < text.Length && text[start + count] >= '0' && text[start + count] <= '9') count++;
            return count;
        }
    }
}
