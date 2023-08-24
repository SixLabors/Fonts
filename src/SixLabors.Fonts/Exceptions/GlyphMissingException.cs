// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Exception for detailing missing font families.
    /// </summary>
    /// <seealso cref="FontException" />
    public class GlyphMissingException : FontException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphMissingException"/> class.
        /// </summary>
        /// <param name="codePoint">The code point for the glyph we where unable to find.</param>
        public GlyphMissingException(CodePoint codePoint)
            : base($"Cannot find a glyph for the code point '{codePoint.ToDebuggerDisplay()}'")
        {
        }
    }
}
