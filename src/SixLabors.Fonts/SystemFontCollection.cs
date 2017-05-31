using SixLabors.Fonts.Exceptions;
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

                // linux directlty list
                "~/.fonts/",
                "/usr/local/share/fonts/",
                "/usr/share/fonts/",

                // mac fonts
                "~/Library/Fonts/",
                "/Library/Fonts/",
                "/Network/Library/Fonts/",
                "/System/Library/Fonts/",
                "/System Folder/Fonts/",
            };

            string[] expanded = paths.Select(x => Environment.ExpandEnvironmentVariables(x)).ToArray();
            string[] found = expanded.Where(x => Directory.Exists(x)).ToArray();

            IEnumerable<string> files = found.SelectMany(x => Directory.EnumerateFiles(x, "*.ttf", SearchOption.AllDirectories));

            foreach(string path in files)
            {
                try
                {
                    this.collection.Install(new FileFontInstance(path));
                }
                catch
                {
                    // we swollow exceptions installing system fonts as we hold no garantees about permissions etc.
                }
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/>.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        public IEnumerable<FontFamily> Families => this.collection.Families;

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>The family if installed otherwise null</returns>
        public FontFamily Find(string fontFamily) => this.collection.Find(fontFamily);
    }
#endif
}
