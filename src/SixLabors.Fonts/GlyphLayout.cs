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
            int graphemeIndex,
            CodePoint codePoint,
            Glyph glyph,
            Vector2 location,
            float width,
            float height,
            float lineHeight,
            bool startOfLine)
        {
            this.GraphemeIndex = graphemeIndex;
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
        /// Gets a value indicating whether this glyph is the first glyph on a new line.
        /// </summary>
        public bool StartOfLine { get; }

        /// <summary>
        /// Gets the Unicode code point of the glyph.
        /// </summary>
        public CodePoint CodePoint { get; }

        /// <summary>
        /// Gets the line height of the glyph.
        /// </summary>
        public float LineHeight { get; }

        /// <summary>
        /// Gets a value indicating whether the glyph represents a whitespace character.
        /// </summary>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool IsWhiteSpace() => CodePoint.IsWhiteSpace(this.CodePoint);

        /// <summary>
        /// Create a new <see cref="GlyphLayout"/> from the given layout offset by the specifid amount.
        /// </summary>
        /// <param name="glyphLayout">The glyph layout.</param>
        /// <param name="offset">The vector to offset the layout by.</param>
        /// <param name="startOfLine">Whether the glyph should be considered to fall at the start of a line.</param>
        /// <returns>The <see cref="GlyphLayout"/>.</returns>
        public static GlyphLayout Offset(GlyphLayout glyphLayout, Vector2 offset, bool startOfLine)
            => new(
                glyphLayout.GraphemeIndex,
                glyphLayout.CodePoint,
                glyphLayout.Glyph,
                glyphLayout.Location + offset,
                glyphLayout.Width,
                glyphLayout.Height,
                glyphLayout.LineHeight,
                startOfLine);

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
