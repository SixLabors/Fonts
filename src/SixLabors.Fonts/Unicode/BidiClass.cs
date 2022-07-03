// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Represents the Unicode Bidi value of a given <see cref="CodePoint"/>.
    /// <see href="https://unicode.org/reports/tr9/#Table"/>
    /// </summary>
    public readonly struct BidiClass
    {
        private readonly uint bidiValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BidiClass"/> struct.
        /// </summary>
        /// <param name="codePoint">The codepoint.</param>
        public BidiClass(CodePoint codePoint)
            => this.bidiValue = UnicodeData.GetBidiData((uint)codePoint.Value);

        /// <summary>
        /// Gets the Unicode Bidirectional character type.
        /// </summary>
        public BidiCharacterType CharacterType
            => (BidiCharacterType)(this.bidiValue >> 24);

        /// <summary>
        /// Gets the Unicode Bidirectional paired bracket type.
        /// </summary>
        public BidiPairedBracketType PairedBracketType
            => (BidiPairedBracketType)((this.bidiValue >> 16) & 0xFF);

        /// <summary>
        /// Gets the codepoint representing the bracket pairing for this instance.
        /// </summary>
        /// <param name="codePoint">
        /// When this method returns, contains the codepoint representing the bracket pairing for this instance;
        /// otherwise, the default value for the type of the <paramref name="codePoint"/> parameter.
        /// This parameter is passed uninitialized.
        /// .</param>
        /// <returns><see langword="true"/> if this instance has a bracket pairing; otherwise, <see langword="false"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetPairedBracket(out CodePoint codePoint)
        {
            if (this.PairedBracketType == BidiPairedBracketType.None)
            {
                codePoint = default;
                return false;
            }

            codePoint = new CodePoint(this.bidiValue & 0xFFFF);
            return true;
        }
    }
}
