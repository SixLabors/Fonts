// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
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
        public static T ThrowGlyphMissingException<T>(CodePoint codePoint)
            => throw new GlyphMissingException(codePoint);

        /// <summary>
        /// Throws an <see cref="FontException"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowDefaultInstance()
            => throw new FontException("Cannot use the default value type instance to create a font.");
    }
}
