using SixLabors.Fonts.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A readonly collection of fonts.
    /// </summary>
    public static class IReadonlyFontCollectionExtensions
    {
        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family. 
        /// </summary>
        /// <param name="collection">The the ont collection to retrieve the font family from.</param>
        /// <param name="fontFamily">The family.</param>
        /// <param name="size">The size.</param>
        /// <param name="style">The style.</param>
        public static Font CreateFont(this IReadOnlyFontCollection collection, string fontFamily, float size, FontVariant style)
        {
            return new Font(collection.Find(fontFamily), size, style);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling. 
        /// </summary>
        /// <param name="collection">The the ont collection to retrieve the font family from.</param>
        /// <param name="fontFamily">The family.</param>
        /// <param name="size">The size.</param>
        public static Font CreateFont(this IReadOnlyFontCollection collection, string fontFamily, float size)
        {
            return new Font(collection.Find(fontFamily), size);
        }
    }
}
