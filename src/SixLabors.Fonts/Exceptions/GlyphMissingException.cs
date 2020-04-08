// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Execption for detailing missing font familys.
    /// </summary>
    /// <seealso cref="FontException" />
    public class GlyphMissingException : FontException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphMissingException"/> class.
        /// </summary>
        /// <param name="codePoint">The code point for the glyph we where unable to find.</param>
        public GlyphMissingException(int codePoint)
            : base($"Cannot find a glyph for the code point '{codePoint}'")
        {
        }
    }
}
