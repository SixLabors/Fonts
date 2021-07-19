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
            int grapheme,
            CodePoint codePoint,
            Glyph glyph,
            Vector2 location,
            float width,
            float height,
            float lineHeight,
            bool startOfLine)
        {
            this.GraphemeIndex = grapheme;
            this.LineHeight = lineHeight;
            this.CodePoint = codePoint;
            this.Glyph = glyph;
            this.Location = location;
            this.Width = width;
            this.Height = height;
            this.StartOfLine = startOfLine;
        }

        /// <summary>
        /// Gets the index of the grapheme in the combined text that the glyph is a member of.
        /// </summary>
        public int GraphemeIndex { get; }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <value>
        /// The glyph.
        /// </value>
        public Glyph Glyph { get; }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        public Vector2 Location { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public float Width { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
        public float Height { get; }

        /// <summary>
        /// Gets a value indicating whether this glyph is the first glyph on a new line.
        /// </summary>
        public bool StartOfLine { get; }

        /// <summary>
        /// Gets the Unicode code point of the glyph.
        /// </summary>
        public CodePoint CodePoint { get; }

        /// <summary>
        /// Gets te line height of the glyph.
        /// </summary>
        public float LineHeight { get; }

        /// <summary>
        /// Gets a value indicating whether gets the glyphe represents a whitespace character.
        /// </summary>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool IsWhiteSpace() => CodePoint.IsWhiteSpace(this.CodePoint);

        internal FontRectangle BoundingBox(Vector2 dpi)
        {
            FontRectangle box = this.Glyph.BoundingBox(this.Location * dpi, dpi);

            if (this.IsWhiteSpace())
            {
                box = new FontRectangle(box.X, box.Y, this.Width * dpi.X, box.Height);
            }

            return box;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.StartOfLine)
            {
                sb.Append('@');
                sb.Append(' ');
            }

            if (this.IsWhiteSpace())
            {
                sb.Append('!');
            }

            sb.Append('\'');
            sb.Append(this.CodePoint.ToDebuggerDisplay());

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
