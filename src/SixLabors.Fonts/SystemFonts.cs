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
    public static class SystemFonts
    {
        private static Lazy<SystemFontCollection> lazySystemFonts = new Lazy<SystemFontCollection>(() => new SystemFontCollection());

        /// <summary>
        /// Gets the collection hosting the globably installled system fonts.
        /// </summary>
        /// <value>
        /// The system fonts.
        /// </value>
        public static IReadOnlyFontCollection Collection => lazySystemFonts.Value;

        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/>s installed on current system.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        public static IEnumerable<FontFamily> Families => Collection.Families;

        /// <summary>
        /// Finds the specified font family from the system font store.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>The family if installed otherwise null</returns>
        public static FontFamily Find(string fontFamily) => Collection.Find(fontFamily);
    
        /// <summary>
        /// Finds the specified font family from the system font store.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        public static bool TryFind(string fontFamily, out FontFamily family) => Collection.TryFind(fontFamily, out family);
    }
#endif
}
