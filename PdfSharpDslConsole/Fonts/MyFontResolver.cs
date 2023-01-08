using PdfSharpCore.Fonts;
using PdfSharpCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslConsole.Fonts
{
    internal class MyFontResolver : IFontResolver
    {
        private readonly IFontResolver _fontResolver;
        Dictionary<string, string> fontmap = new Dictionary<string, string>();
        public string DefaultFontName => "Arial";
        public MyFontResolver(IEnumerable<string> fontFiles)
        {
            _fontResolver = new FontResolver();
            if (fontFiles != null && fontFiles.Any())
            {
                foreach (var fontFile in fontFiles)
                {
                    fontmap.Add(Path.GetFileName(fontFile).ToLowerInvariant(), fontFile);
                }
                FontResolver.SetupFontsFiles(fontFiles.ToArray());
            }
        }
        public byte[] GetFont(string faceName)
        {
            if (fontmap.TryGetValue(faceName.ToLowerInvariant(), out var file))
            {
                return File.ReadAllBytes(file);
            }

            return _fontResolver.GetFont(faceName);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return _fontResolver.ResolveTypeface(familyName, isBold, isItalic);
        }
    }
}
