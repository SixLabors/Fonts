// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;
using SixLabors.Primitives;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic or laying out text.
    /// </summary>
    internal class TextLayout
    {
        internal static TextLayout Default { get; set; } = new TextLayout();

        /// <summary>
        /// Generates the layout.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>A collection of layout that describe all thats needed to measure or render a series of glyphs.</returns>
        public ImmutableArray<GlyphLayout> GenerateLayout(string text, RendererOptions options)
        {
            var dpi = new Vector2(options.DpiX, options.DpiY);
            Vector2 origin = (Vector2)options.Origin / dpi;

            float maxWidth = float.MaxValue;
            float originX = 0;
            if (options.WrappingWidth > 0)
            {
                // trim trailing white spaces from the text
                text = text.TrimEnd(null);

                maxWidth = options.WrappingWidth / options.DpiX;

                switch (options.HorizontalAlignment)
                {
                    case HorizontalAlignment.Right:
                        originX = maxWidth;
                        break;
                    case HorizontalAlignment.Center:
                        originX = maxWidth / 2f;
                        break;
                    case HorizontalAlignment.Left:
                    default:
                        originX = 0;
                        break;
                }
            }

            AppliedFontStyle spanStyle = options.GetStyle(0, text.Length);
            List<GlyphLayout> layout = new List<GlyphLayout>(text.Length);

            float lineHeight = 0f;
            Vector2 location = Vector2.Zero;
            float lineHeightOfFirstLine = 0;
            bool firstLine = true;
            GlyphInstance previousGlyph = null;
            float scale = 0;
            int lastWrappableLocation = -1;
            bool startOfLine = true;
            float totalHeight = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (spanStyle.End < i)
                {
                    spanStyle = options.GetStyle(i, text.Length);
                    previousGlyph = null;
                }

                if (spanStyle.Font.LineHeight > lineHeight)
                {
                    // get the larget lineheight thus far
                    scale = spanStyle.Font.EmSize * 72;
                    lineHeight = (spanStyle.Font.LineHeight * spanStyle.PointSize) / scale;
                }

                if (firstLine && lineHeight > lineHeightOfFirstLine)
                {
                    lineHeightOfFirstLine = lineHeight;
                }

                char c = text[i];

                if (options.WrappingWidth > 0 && char.IsWhiteSpace(c))
                {
                    // keep a record of where to wrap text and ensure that no line starts with white space
                    for (int j = layout.Count - 1; j >= 0; j--)
                    {
                        if (!layout[j].IsWhiteSpace)
                        {
                            lastWrappableLocation = j + 1;
                            break;
                        }
                    }
                }
                GlyphInstance glyph = spanStyle.Font.GetGlyph(c);
                float glyphWidth = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                float glyphHeight = (glyph.Height * spanStyle.PointSize) / scale;

                switch (c)
                {
                    case '\r':
                        // carrage return resets the XX coordinate to 0
                        location.X = 0;
                        previousGlyph = null;
                        startOfLine = true;

                        layout.Add(new GlyphLayout(c, new Glyph(glyph, spanStyle.PointSize), location, 0, glyphHeight, lineHeight, startOfLine, true, true));
                        startOfLine = false;
                        break;
                    case '\n':
                        {
                            // carrage return resets the XX coordinate to 0
                            layout.Add(new GlyphLayout(c, new Glyph(glyph, spanStyle.PointSize), location, 0, glyphHeight, lineHeight, startOfLine, true, true));
                            location.X = 0;
                            location.Y += lineHeight;
                            lineHeight = 0; // reset line height tracking for next line
                            previousGlyph = null;
                            firstLine = false;
                            lastWrappableLocation = -1;
                            startOfLine = true;
                        }
                        break;
                    case '\t':
                        {
                            float tabStop = glyphWidth * spanStyle.TabWidth;
                            float finalWidth = 0;

                            if (tabStop > 0)
                            {
                                finalWidth = tabStop - (location.X % tabStop);
                            }

                            if (finalWidth < glyphWidth)
                            {
                                // if we are not going to tab atleast a glyph width add another tabstop to it ???
                                // should I be doing this?
                                finalWidth += tabStop;
                            }

                            layout.Add(new GlyphLayout(c, new Glyph(glyph, spanStyle.PointSize), location, finalWidth, glyphHeight, lineHeight, startOfLine, true, false));
                            startOfLine = false;

                            // advance to a position > width away that
                            location.X += finalWidth;
                            previousGlyph = null;
                        }

                        break;
                    case ' ':
                        {
                            layout.Add(new GlyphLayout(c, new Glyph(glyph, spanStyle.PointSize), location, glyphWidth, glyphHeight, lineHeight, startOfLine, true, false));
                            startOfLine = false;
                            location.X += glyphWidth;
                            previousGlyph = null;
                        }

                        break;
                    default:
                        {
                            Vector2 glyphLocation = location;
                            if (spanStyle.ApplyKerning && previousGlyph != null)
                            {
                                // if there is special instructions for this glyph pair use that width
                                Vector2 scaledOffset = (spanStyle.Font.GetOffset(glyph, previousGlyph) * spanStyle.PointSize) / scale;

                                glyphLocation += scaledOffset;

                                // only fix the 'X' of the current tracked location but use the actual 'X'/'Y' of the offset
                                location.X = glyphLocation.X;
                            }

                            layout.Add(new GlyphLayout(c, new Glyph(glyph, spanStyle.PointSize), glyphLocation, glyphWidth, glyphHeight, lineHeight, startOfLine, false, false));
                            startOfLine = false;

                            // move forward the actual width of the glyph, we are retaining the baseline
                            location.X += glyphWidth;

                            // if the word extended pass the end of the box, wrap it
                            if (location.X >= maxWidth && lastWrappableLocation > 0)
                            {
                                if (lastWrappableLocation < layout.Count)
                                {
                                    float wrappingOffset = layout[lastWrappableLocation].Location.X;
                                    startOfLine = true;

                                    // move the characters to the next line
                                    for (int j = lastWrappableLocation; j < layout.Count; j++)
                                    {
                                        if (layout[j].IsWhiteSpace)
                                        {
                                            wrappingOffset += layout[j].Width;
                                            layout.RemoveAt(j);
                                            j--;
                                            continue;
                                        }

                                        Vector2 current = layout[j].Location;
                                        layout[j] = new GlyphLayout(layout[j].Character, layout[j].Glyph, new Vector2(current.X - wrappingOffset, current.Y + lineHeight), layout[j].Width, layout[j].Height, layout[j].LineHeight, startOfLine, layout[j].IsWhiteSpace, layout[j].IsControlCharacter);
                                        startOfLine = false;

                                        location.X = layout[j].Location.X + layout[j].Width;
                                    }

                                    location.Y += lineHeight;
                                    firstLine = false;
                                    lastWrappableLocation = -1;
                                }
                            }
                            float bottom = location.Y + lineHeight;
                            if (bottom > totalHeight)
                            {
                                totalHeight = bottom;
                            }
                            previousGlyph = glyph;
                        }

                        break;
                }
            }

            Vector2 offset = new Vector2(0, lineHeightOfFirstLine);

            switch (options.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    offset += new Vector2(0, -(totalHeight / 2));
                    break;
                case VerticalAlignment.Bottom:
                    offset += new Vector2(0, -totalHeight);
                    break;
                case VerticalAlignment.Top:
                default:
                    // no change
                    break;
            }
            Vector2 lineOffset = offset;
            for (int i = 0; i < layout.Count; i++)
            {
                GlyphLayout glyphLayout = layout[i];
                if (glyphLayout.StartOfLine)
                {
                    lineOffset = offset;
                    // scan ahead measuring width
                    float width = glyphLayout.Width;
                    for (int j = i + 1; j < layout.Count; j++)
                    {
                        if (layout[j].StartOfLine)
                        {
                            break;
                        }
                        width = layout[j].Location.X + layout[j].Width; // rhs
                    }
                    switch (options.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Right:
                            lineOffset = new Vector2(originX - width, 0) + offset;
                            break;
                        case HorizontalAlignment.Center:
                            lineOffset = new Vector2(originX - (width / 2f), 0) + offset;
                            break;
                        case HorizontalAlignment.Left:
                        default:
                            lineOffset = new Vector2(originX, 0) + offset;
                            break;
                    }
                }

                // TODO calculate an offset from the 'origin' based on TextAlignment for each line
                layout[i] = new GlyphLayout(glyphLayout.Character, glyphLayout.Glyph, glyphLayout.Location + lineOffset + origin, glyphLayout.Width, glyphLayout.Height, glyphLayout.LineHeight, glyphLayout.StartOfLine, glyphLayout.IsWhiteSpace, glyphLayout.IsControlCharacter);
            }

            return layout.ToImmutableArray();
        }
    }

    /// <summary>
    /// A glyphs layout and location
    /// </summary>
    internal struct GlyphLayout
    {

        internal GlyphLayout(char character, Glyph glyph, Vector2 location, float width, float height, float lineHeight, bool startOfLine, bool isWhiteSpace, bool isControlCharacter)
        {
            this.LineHeight = lineHeight;
            this.Character = character;
            this.Glyph = glyph;
            this.Location = location;
            this.Width = width;
            this.Height = height;
            this.StartOfLine = startOfLine;
            this.IsWhiteSpace = isWhiteSpace;
            this.IsControlCharacter = isControlCharacter;
        }

        internal RectangleF BoundingBox(Vector2 dpi)
        {
            var box = this.Glyph.BoundingBox(this.Location * dpi, dpi);
            if (this.IsWhiteSpace)
            {
                box.Width = this.Width * dpi.X;
            }
            return box;
        }

        /// <summary>
        /// Gets the IsWhiteSpace.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        public bool IsWhiteSpace { get; private set; }

        /// <summary>
        /// Gets the glyph.
        /// </summary>
        /// <value>
        /// The glyph.
        /// </value>
        public Glyph Glyph { get; private set; }

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
        /// Gets weather this glyph is the first glyph on a new line.
        /// </summary>
        public bool StartOfLine { get; set; }
        public char Character { get; private set; }
        public float LineHeight { get; private set; }
        public bool IsControlCharacter { get; private set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
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
            switch (this.Character)
            {
                case '\t': sb.Append("\\t"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case ' ': sb.Append(" "); break;
                default:
                    sb.Append(this.Character);
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
