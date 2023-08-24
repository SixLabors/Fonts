// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if !SUPPORTS_ENCODING_STRING
using System;
using System.Text;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Extension methods for the <see cref="Encoder"/> type.
    /// </summary>
    internal static unsafe class EncodingExtensions
    {
        /// <summary>
        /// Gets a string from the provided buffer data.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns>The string.</returns>
        public static string GetString(this Encoding encoding, ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                return string.Empty;
            }

            fixed (byte* bytes = buffer)
            {
                return encoding.GetString(bytes, buffer.Length);
            }
        }
    }
}
#endif
