// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;
using System.Text;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyphs layout and location
    /// </summary>
    internal readonly struct GlyphLayout
    {
        internal GlyphLayout(
            Glyph glyph,
            Vector2 location,
            float width,
            float height,
            float lineHeight,
            bool isStartOfLine)
        {
            this.LineHeight = lineHeight;
            this.Glyph = glyph;
            this.Location = location;
            this.Width = width;
            this.Height = height;
            this.IsStartOfLine = isStartOfLine;
        }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        public Glyph Glyph { get; }

        /// <summary>
        /// Gets the location.
        /// </summary>
        public Vector2 Location { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Gets the line height of the glyph.
        /// </summary>
        public float LineHeight { get; }

        /// <summary>
        /// Gets a value indicating whether this glyph is the first glyph on a new line.
        /// </summary>
        public bool IsStartOfLine { get; }

        /// <summary>
        /// Gets a value indicating whether the glyph represents a whitespace character.
        /// </summary>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool IsWhiteSpace() => CodePoint.IsWhiteSpace(this.Glyph.GlyphMetrics.CodePoint);

        internal FontRectangle BoundingBox(float dpi)
        {
            FontRectangle box = this.Glyph.BoundingBox(this.Location * dpi, dpi);

            if (this.IsWhiteSpace())
            {
                box = new FontRectangle(box.X, box.Y, this.Width * dpi, box.Height);
            }

            return box;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.IsStartOfLine)
            {
                sb.Append('@');
                sb.Append(' ');
            }

            if (this.IsWhiteSpace())
            {
                sb.Append('!');
            }

            sb.Append('\'');
            sb.Append(this.Glyph.GlyphMetrics.CodePoint.ToDebuggerDisplay());

            sb.Append('\'');
            sb.Append(' ');

            sb.Append(this.Location.X);
            sb.Append(',');
            sb.Append(this.Location.Y);
            sb.Append(' ');
            sb.Append(this.Width);
            sb.Append('x');
            sb.Append(this.Height);

            return sb.ToString();
        }
    }
}
