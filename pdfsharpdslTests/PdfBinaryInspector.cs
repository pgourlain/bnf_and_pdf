using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace pdfsharpdslTests;

internal sealed class PdfBinaryInspector
{
    private static readonly Regex PagePattern = new(@"/Type\s*/Page(?!s)\b", RegexOptions.Compiled);
    private static readonly Regex MediaBoxPattern = new(
        @"/MediaBox\s*\[\s*(?<x1>-?[\d.]+)\s+(?<y1>-?[\d.]+)\s+(?<x2>-?[\d.]+)\s+(?<y2>-?[\d.]+)\s*\]",
        RegexOptions.Compiled);

    private readonly byte[] _bytes;
    private readonly string _text;

    public PdfBinaryInspector(Stream stream)
    {
        stream.Position = 0;
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        _bytes = copy.ToArray();
        _text = Encoding.Latin1.GetString(_bytes);
    }

    public bool HasPdfHeader => _bytes.Length >= 5 && Encoding.ASCII.GetString(_bytes, 0, 5) == "%PDF-";
    public int PageCount => PagePattern.Matches(_text).Count;
    public int ContentStreamCount => Regex.Matches(_text, @"\bstream\r?\n").Count;
    public int FontCount => Regex.Matches(_text, @"/Type\s*/Font\b").Count;
    public int ImageCount => Regex.Matches(_text, @"/Subtype\s*/Image\b").Count;

    public IReadOnlyList<PdfRectInfo> MediaBoxes => MediaBoxPattern.Matches(_text)
        .Select(match => new PdfRectInfo(
            Parse(match.Groups["x1"].Value),
            Parse(match.Groups["y1"].Value),
            Parse(match.Groups["x2"].Value),
            Parse(match.Groups["y2"].Value)))
        .ToArray();

    private static double Parse(string value) => double.Parse(value, CultureInfo.InvariantCulture);
}

internal readonly record struct PdfRectInfo(double X1, double Y1, double X2, double Y2)
{
    public double Width => X2 - X1;
    public double Height => Y2 - Y1;
}