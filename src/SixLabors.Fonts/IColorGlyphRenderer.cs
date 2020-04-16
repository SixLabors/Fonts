// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.
using System;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A surface that can have a glyph rendered to it as a series of actions, where the engine support colored glyphs (emoji).
    /// </summary>
    public interface IColorGlyphRenderer : IGlyphRenderer
    {
        /// <summary>
        /// Sets the color to use for the current glyph.
        /// </summary>
        /// <param name="color">The color to override the renders brush with.</param>
        void SetColor(GlyphColor color);
    }

    /// <summary>
    /// Provides access to the the color details for the current glyph.
    /// </summary>
    public readonly struct GlyphColor
    {
        internal GlyphColor(byte blue, byte green, byte red, byte alpha)
        {
            this.Blue = blue;
            this.Green = green;
            this.Red = red;
            this.Alpha = alpha;
        }

        /// <summary>
        /// Gets the blue component
        /// </summary>
        public readonly byte Blue { get; }

        /// <summary>
        /// Gets the green component
        /// </summary>
        public readonly byte Green { get; }

        /// <summary>
        /// Gets the red component
        /// </summary>
        public readonly byte Red { get; }

        /// <summary>
        /// Gets the alpha component
        /// </summary>
        public readonly byte Alpha { get; }

        /// <summary>
        /// Compares two <see cref="GlyphColor"/> objects for equality.
        /// </summary>
        /// <param name="left">
        /// The <see cref="GlyphColor"/> on the left side of the operand.
        /// </param>
        /// <param name="right">
        /// The <see cref="GlyphColor"/> on the right side of the operand.
        /// </param>
        /// <returns>
        /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(GlyphColor left, GlyphColor right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="GlyphColor"/> objects for inequality.
        /// </summary>
        /// <param name="left">
        /// The <see cref="GlyphColor"/> on the left side of the operand.
        /// </param>
        /// <param name="right">
        /// The <see cref="GlyphColor"/> on the right side of the operand.
        /// </param>
        /// <returns>
        /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(GlyphColor left, GlyphColor right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is GlyphColor p && this.Equals(p);

        /// <summary>
        /// Compares the  <see cref="GlyphColor"/> for equality to this color.
        /// </summary>
        /// <param name="other">
        /// The other <see cref="GlyphColor"/> to compair to.
        /// </param>
        /// <returns>
        /// True if the current color is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public bool Equals(GlyphColor other)
        {
            return other.Red == this.Red
                && other.Green == this.Green
                && other.Blue == this.Blue
                && other.Alpha == this.Alpha;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                this.Red,
                this.Green,
                this.Blue,
                this.Alpha);
        }
    }
}
