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
    public interface IReadOnlyFontCollection
    {
        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/>.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        IEnumerable<FontFamily> Families { get; }

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFountException"/></returns>
        FontFamily Find(string fontFamily);

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        bool TryFind(string fontFamily, out FontFamily family);
    }
}
