// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a <see cref="Glyph"/> metric.
    /// </summary>
    public readonly struct GlyphMetric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphMetric"/> struct.
        /// </summary>
        /// <param name="codePoint">Unicode codepoint of the character.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="isControlCharacter">Whether the character is a control character.</param>
        public GlyphMetric(int codePoint, FontRectangle bounds, bool isControlCharacter)
        {
            this.Codepoint = codePoint;
            this.Character = char.ConvertFromUtf32(codePoint);
            this.Bounds = bounds;
            this.IsControlCharacter = isControlCharacter;
        }

        /// <summary>
        /// Gets the Unicode codepoint of the character.
        /// </summary>
        public int Codepoint { get; }

        /// <summary>
        /// Gets the UTF-16 encoded character.
        /// </summary>
        public string Character { get; }

        /// <summary>
        /// Gets the character bounds.
        /// </summary>
        public FontRectangle Bounds { get; }

        /// <summary>
        /// Gets a value indicating whether the character is a control character.
        /// </summary>
        public bool IsControlCharacter { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Character: {this.Character}, bounds: {this.Bounds}, is control char: {this.IsControlCharacter}";
        }
    }
}
