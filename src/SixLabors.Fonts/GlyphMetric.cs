// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Primitives;

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
        /// <param name="character">The character.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="isControlCharacter">Whether the character is a control character.</param>
        public GlyphMetric(char character, RectangleF bounds, bool isControlCharacter)
        {
            this.Character = character;
            this.Bounds = bounds;
            this.IsControlCharacter = isControlCharacter;
        }

        /// <summary>
        /// Gets the character.
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// Gets the character bounds.
        /// </summary>
        public RectangleF Bounds { get; }

        /// <summary>
        /// Gets a value indicating whether the character is a control character.
        /// </summary>
        public bool IsControlCharacter { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Char: {this.Character}, bounds: {this.Bounds}, is control char: {this.IsControlCharacter}";
        }
    }
}
