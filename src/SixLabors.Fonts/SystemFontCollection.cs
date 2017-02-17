using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
#if FILESYSTEM
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public sealed class SystemFontCollection : IReadonlyFontCollection
    {
        private readonly FontCollection collection = new FontCollection();

        internal SystemFontCollection()
        {
            string[] paths = new[] {
                // windows directories
                "%SYSTEMROOT%\\Fonts",
            };

            var expanded = paths.Select(x => Environment.ExpandEnvironmentVariables(x)).ToArray();
            var found = expanded.Where(x => Directory.Exists(x)).ToArray();

            var fonts = found.SelectMany(x => Directory.EnumerateFiles(x, "*.ttf")).Select(x => new FileFontInstance(x)).ToArray();
            foreach (var f in fonts)
            {
                collection.Install(f);
            }
        }

        public IEnumerable<FontFamily> Families => collection.Families;

        public FontFamily Find(string fontFamily) => collection.Find(fontFamily);
    }
#endif
}
