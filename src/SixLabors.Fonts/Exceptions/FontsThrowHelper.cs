// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Exceptions
{
    /// <summary>
    /// Helper methods to throw exceptions
    /// </summary>
    internal static class FontsThrowHelper
    {
        /// <summary>
        /// Throws an <see cref="GlyphMissingException"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T ThrowGlyphMissingException<T>(int codePoint)
        {
            throw new GlyphMissingException(codePoint);
        }
    }
}
