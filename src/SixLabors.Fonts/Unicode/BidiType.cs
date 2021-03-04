// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Represents the Unicode Bidi value of a given <see cref="CodePoint"/>.
    /// <see href="https://unicode.org/reports/tr9/#Table"/>
    /// </summary>
    internal readonly struct BidiType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BidiType"/> struct.
        /// </summary>
        /// <param name="codePoint">The codepoint.</param>
        public BidiType(CodePoint codePoint)
            => this.Value = (int)UnicodeData.GetBidiData(codePoint.Value);

        /// <summary>
        /// Gets the <see cref="BidiCharacterType"/>.
        /// </summary>
        public BidiCharacterType CharacterType
            => (BidiCharacterType)(this.Value >> 24);

        /// <summary>
        /// Gets the <see cref="BidiPairedBracketType"/>.
        /// </summary>
        public BidiPairedBracketType PairedBracketType
            => (BidiPairedBracketType)((this.Value >> 16) & 0xFF);

        /// <summary>
        /// Gets the Unicode value of the Bidi type as an integer.
        /// </summary>
        public int Value { get; }
    }
}
