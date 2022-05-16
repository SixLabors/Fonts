// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics.CodeAnalysis;

namespace SixLabors.Fonts.Native
{
    /// <summary>
    /// An integer type for constants used to specify supported string encodings in various CFString functions.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Verbatim constants from the macOS SDK")]
    internal enum CFStringEncoding : uint
    {
        /// <summary>
        /// An encoding constant that identifies the UTF 8 encoding.
        /// </summary>
        kCFStringEncodingUTF8 = 0x08000100,

        /// <summary>
        /// An encoding constant that identifies kTextEncodingUnicodeDefault + kUnicodeUTF16LEFormat encoding. This constant specifies little-endian byte order.
        /// </summary>
        kCFStringEncodingUTF16LE = 0x14000100,
    }
}
