// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;
using System.Text;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyphs layout and location
    /// </summary>
    internal readonly struct GlyphLayout
    {
        internal GlyphLayout(int codePoint, Glyph glyph, Vector2 location, float width, float height, float lineHeight, bool startOfLine, bool isWhiteSpace, bool isControlCharacter)
        {
            this.LineHeight = lineHeight;
            this.CodePoint = codePoint;
            this.Glyph = glyph;
            this.Location = location;
            this.Width = width;
            this.Height = height;
            this.StartOfLine = startOfLine;
            this.IsWhiteSpace = isWhiteSpace;
            this.IsControlCharacter = isControlCharacter;
        }

        /// <summary>
        /// Gets a value indicating whether gets the glyphe represents a whitespace character.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public bool IsWhiteSpace { get; }

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
        /// Gets the Unicode code point of the character.
        /// </summary>
        public int CodePoint { get; }

        public float LineHeight { get; }

        public bool IsControlCharacter { get; }

        internal FontRectangle BoundingBox(Vector2 dpi)
        {
            FontRectangle box = this.Glyph.BoundingBox(this.Location * dpi, dpi);

            if (this.IsWhiteSpace)
            {
                box = new FontRectangle(box.X, box.Y, this.Width * dpi.X, box.Height);
            }

            return box;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.StartOfLine)
            {
                sb.Append('@');
                sb.Append(' ');
            }

            if (this.IsWhiteSpace)
            {
                sb.Append('!');
            }

            sb.Append('\'');
            switch (this.CodePoint)
            {
                case '\t': sb.Append("\\t"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case ' ': sb.Append(" "); break;
                default:
                    sb.Append(char.ConvertFromUtf32(this.CodePoint));
                    break;
            }

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
