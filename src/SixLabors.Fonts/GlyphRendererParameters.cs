// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts
{
    /// <summary>
    /// The combined set of properties that uniquely identify the glyph that is to be rendered
    /// at a particular size and dpi.
    /// </summary>
    public readonly struct GlyphRendererParameters : IEquatable<GlyphRendererParameters>
    {
        internal GlyphRendererParameters(GlyphInstance glyph, float pointSize, Vector2 dpi)
        {
            this.Font = glyph.Font.Description.FontName?.ToUpper();
            this.FontStyle = glyph.Font.Description.Style;
            this.GlyphIndex = glyph.Index;
            this.PointSize = pointSize;
            this.DpiX = dpi.X;
            this.DpiY = dpi.Y;
        }

        /// <summary>
        /// Gets the name of the Font this glyph belongs to.
        /// </summary>
        public string Font { get; }

        /// <summary>
        /// Gets the style of the Font this glyph belongs to.
        /// </summary>
        public FontStyle FontStyle { get; }

        /// <summary>
        /// Gets the index of the glyph.
        /// </summary>
        public ushort GlyphIndex { get; }

        /// <summary>
        /// Gets the rendered point size.
        /// </summary>
        public float PointSize { get; }

        /// <summary>
        /// Gets the dpi along the X axis we are rendering at.
        /// </summary>
        public float DpiX { get; }

        /// <summary>
        /// Gets the dpi along the Y axis we are rendering at.
        /// </summary>
        public float DpiY { get; }

        /// <summary>
        /// Compares two <see cref="GlyphRendererParameters"/> objects for equality.
        /// </summary>
        /// <param name="left">
        /// The <see cref="GlyphRendererParameters"/> on the left side of the operand.
        /// </param>
        /// <param name="right">
        /// The <see cref="GlyphRendererParameters"/> on the right side of the operand.
        /// </param>
        /// <returns>
        /// True if the current left is equal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator ==(GlyphRendererParameters left, GlyphRendererParameters right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="GlyphRendererParameters"/> objects for inequality.
        /// </summary>
        /// <param name="left">
        /// The <see cref="GlyphRendererParameters"/> on the left side of the operand.
        /// </param>
        /// <param name="right">
        /// The <see cref="GlyphRendererParameters"/> on the right side of the operand.
        /// </param>
        /// <returns>
        /// True if the current left is unequal to the <paramref name="right"/> parameter; otherwise, false.
        /// </returns>
        public static bool operator !=(GlyphRendererParameters left, GlyphRendererParameters right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(GlyphRendererParameters other)
        {
            return other.PointSize == this.PointSize
                && other.FontStyle == this.FontStyle
                && other.DpiX == this.DpiX
                && other.DpiY == this.DpiY
                && other.GlyphIndex == this.GlyphIndex
                && ((other.Font is null && this.Font is null)
                || (other.Font?.Equals(this.Font, StringComparison.OrdinalIgnoreCase) == true));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is GlyphRendererParameters p && this.Equals(p);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = this.Font?.GetHashCode() ?? 0;
            hash = HashHelpers.Combine(hash, this.PointSize.GetHashCode());
            hash = HashHelpers.Combine(hash, this.GlyphIndex.GetHashCode());
            hash = HashHelpers.Combine(hash, this.FontStyle.GetHashCode());
            hash = HashHelpers.Combine(hash, this.DpiX.GetHashCode());
            return HashHelpers.Combine(hash, this.DpiY.GetHashCode());
        }
    }
}