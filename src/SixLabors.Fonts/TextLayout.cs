using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapsulated logic or laying out text.
    /// </summary>
    public class TextLayout
    {
        /// <summary>
        /// Generates the layout.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="style">The style.</param>
        /// <returns>A collection of layout that describe all thats needed to measure or render a series of glyphs.</returns>
        public ImmutableArray<GlyphLayout> GenerateLayout(string text, FontSpan style)
        {
            float maxWidth = float.MaxValue;
            float xOrigin = 0;
            if(style.WrappingWidth > 0)
            {
                maxWidth = style.WrappingWidth / style.DPI.X ;
                xOrigin = maxWidth / 2f;
            }

            AppliedFontStyle spanStyle = style.GetStyle(0, text.Length);
            List<GlyphLayout> layout = new List<GlyphLayout>(text.Length);

            float lineHeight = 0f;
            Vector2 location = Vector2.Zero;
            float lineHeightOfFirstLine = 0;
            bool firstLine = true;
            GlyphInstance previousGlyph = null;
            float scale = 0;
            int lastWrappableLocation = -1;
            bool startOfLine = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (spanStyle.End < i)
                {
                    spanStyle = style.GetStyle(i, text.Length);
                    previousGlyph = null;
                }

                if (spanStyle.Font.LineHeight > lineHeight)
                {
                    // get the larget lineheight thus far
                    scale = spanStyle.Font.EmSize * 72;
                    lineHeight = (spanStyle.Font.LineHeight * spanStyle.PointSize) / scale;
                    
                }

                if(firstLine && lineHeight > lineHeightOfFirstLine)
                {
                    lineHeightOfFirstLine = lineHeight;
                }

                char c = text[i];


                if (char.IsWhiteSpace(c))
                {
                    //find the index in the layout where we last enabled back tracking from
                    lastWrappableLocation = layout.Count;
                }

                switch (c)
                {
                    case '\r':
                        // carrage return resets the XX coordinate to 0
                        location.X = 0;
                        previousGlyph = null;
                        startOfLine = true;
                        break;
                    case '\n':
                        // carrage return resets the XX coordinate to 0
                        location.X = 0;
                        location.Y += lineHeight;
                        lineHeight = 0; // reset line height tracking for next line
                        previousGlyph = null;
                        firstLine = false;
                        lastWrappableLocation = -1;
                        startOfLine = true;
                        break;
                    case '\t':
                        {
                            GlyphInstance glyph = spanStyle.Font.GetGlyph(c);
                            float width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            float tabStop = width * spanStyle.TabWidth;

                            // advance to a position > width away that
                            float dist = tabStop - ((location.X + width) % tabStop);
                            location.X += dist;
                            previousGlyph = null;
                        }

                        break;
                    case ' ':
                        {
                            GlyphInstance glyph = spanStyle.Font.GetGlyph(c);
                            float width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            location.X += width;
                            previousGlyph = null;
                        }

                        break;
                    default:
                        {
                            GlyphInstance glyph = spanStyle.Font.GetGlyph(c);
                            float width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            float height = (glyph.Height * spanStyle.PointSize) / scale;

                            Vector2 glyphLocation = location;
                            if (spanStyle.ApplyKerning && previousGlyph != null)
                            {
                                // if there is special instructions for this glyph pair use that width
                                Vector2 scaledOffset = (spanStyle.Font.GetOffset(glyph, previousGlyph) * spanStyle.PointSize) / scale;

                                glyphLocation += scaledOffset;

                                // only fix the 'X' of the current tracked location but use the actual 'X'/'Y' of the offset
                                location.X = glyphLocation.X;
                            }

                            layout.Add(new GlyphLayout(new Glyph(glyph, spanStyle.PointSize), glyphLocation, width, height, startOfLine));
                            startOfLine = false;

                            // move foraward the actual with of the glyph, we are retaining the baseline
                            location.X += width;

                            if(location.X >= maxWidth && lastWrappableLocation > 0)
                            {
                                if (lastWrappableLocation < layout.Count)
                                {
                                    float wrappingOffset = layout[lastWrappableLocation].Location.X;
                                    startOfLine = true;
                                    // the word just extended passed the end of the box 
                                    for (int j = lastWrappableLocation; j < layout.Count; j++)
                                    {
                                        Vector2 current = layout[j].Location;
                                        layout[j] = new GlyphLayout(layout[j].Glyph, new Vector2(current.X - wrappingOffset, current.Y + lineHeight), layout[j].Width, layout[j].Height, startOfLine);
                                        startOfLine = false;

                                        location.X = layout[j].Location.X + layout[j].Width;
                                    }

                                    location.Y += lineHeight;
                                    firstLine = false;
                                    lastWrappableLocation = -1;
                                }
                            }

                            previousGlyph = glyph;
                        }

                        break;
                }
            }

            Vector2 offset = new Vector2(0, lineHeightOfFirstLine);
            for(int i =0; i< layout.Count; i++)
            {
                GlyphLayout glyphLayout = layout[i];

                if (glyphLayout.StartOfLine)
                {
                    // scan ahead measuring width
                    float width = glyphLayout.Width;
                    for (int j = i+1; j < layout.Count; j++)
                    {
                        if (layout[j].StartOfLine) { break; }
                        width = layout[j].Location.X + layout[j].Width;// rhs
                    }
                    switch (style.Alignment)
                    {
                        case TextAlignment.Right:
                            offset = new Vector2(xOrigin - width, lineHeightOfFirstLine);
                            break;
                        case TextAlignment.Center:
                            offset = new Vector2(xOrigin - (width/2f), lineHeightOfFirstLine);
                            break;
                        case TextAlignment.Left:
                        default:
                            offset = new Vector2(xOrigin, lineHeightOfFirstLine);
                            break;
                    }
                }

                // TODO calculate an offset from the 'origin' based on TextAlignment for each line
                layout[i] = new GlyphLayout(glyphLayout.Glyph, glyphLayout.Location + offset, glyphLayout.Width, glyphLayout.Height, glyphLayout.StartOfLine);
            }

            return layout.ToImmutableArray();
        }
    }

    /// <summary>
    /// A glyphs layout and location
    /// </summary>
    public struct GlyphLayout
    {
        internal GlyphLayout(Glyph glyph, Vector2 location, float width, float height, bool startOfLine)
        {
            this.Glyph = glyph;
            this.Location = location;
            this.Width = width;
            this.Height = height;
            this.StartOfLine = startOfLine;
        }

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
    }
}
