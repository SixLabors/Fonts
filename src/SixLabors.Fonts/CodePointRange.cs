// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a range of codepoints within a body of text.
    /// </summary>
    public readonly struct CodePointRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodePointRange"/> struct.
        /// </summary>
        /// <param name="start">The zero-based index of the range in the input text.</param>
        /// <param name="script">The Unicode script class for the range.</param>
        /// <param name="length">The number of codepoints represented by the range.</param>
        public CodePointRange(ushort start, Script script, ushort length)
        {
            this.Start = start;
            this.Script = script;
            this.Length = length;
        }

        /// <summary>
        /// Gets the zero-based index of the range in the input text.
        /// </summary>
        public ushort Start { get; }

        /// <summary>
        /// Gets the Unicode script class for the range.
        /// </summary>
        public Script Script { get; }

        /// <summary>
        /// Gets the number of codepoints represented by the range.
        /// </summary>
        public readonly ushort Length { get; }

        /// <inheritdoc/>
        public override string ToString()
            => $"CodePointRange - Start: {this.Start}, Script: {this.Script}, Length: {this.Length}";
    }
}
