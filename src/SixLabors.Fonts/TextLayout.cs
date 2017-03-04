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
            AppliedFontStyle spanStyle = style.GetStyle(0, text.Length);
            List<GlyphLayout> layout = new List<GlyphLayout>(text.Length);

            float lineHeight = 0f;
            Vector2 location = Vector2.Zero;
            float lineHeightOfFirstLine = 0;
            bool firstLine = true;
            GlyphInstance previousGlyph = null;
            float scale = 0;
            for (var i = 0; i < text.Length; i++)
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

                var c = text[i];
                switch (c)
                {
                    case '\r':
                        // carrage return resets the XX coordinate to 0
                        location.X = 0;
                        previousGlyph = null;

                        break;
                    case '\n':
                        // carrage return resets the XX coordinate to 0
                        location.X = 0;
                        location.Y += lineHeight;
                        lineHeight = 0; // reset line height tracking for next line
                        previousGlyph = null;
                        firstLine = false;
                        break;
                    case '\t':
                        {
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            var tabStop = width * spanStyle.TabWidth;

                            // advance to a position > width away that
                            var dist = tabStop - ((location.X + width) % tabStop);
                            location.X += dist;
                            previousGlyph = null;
                        }

                        break;
                    case ' ':
                        {
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            location.X += width;
                            previousGlyph = null;
                        }

                        break;
                    default:
                        {
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            var height = (glyph.Height * spanStyle.PointSize) / scale;

                            var glyphLocation = location;
                            if (spanStyle.ApplyKerning && previousGlyph != null)
                            {
                                // if there is special instructions for this glyph pair use that width
                                var scaledOffset = (spanStyle.Font.GetOffset(glyph, previousGlyph) * spanStyle.PointSize) / scale;

                                glyphLocation += scaledOffset;

                                // only fix the 'X' of the current tracked location but use the actual 'X'/'Y' of the offset
                                location.X = glyphLocation.X;
                            }

                            layout.Add(new GlyphLayout(new Glyph(glyph, spanStyle.PointSize), glyphLocation, width, height));

                            // move foraward the actual with of the glyph, we are retaining the baseline
                            location.X += width;
                            previousGlyph = glyph;
                        }

                        break;
                }
            }

            var offset = new Vector2(0, lineHeightOfFirstLine);
            for(var i =0; i< layout.Count; i++)
            {
                var glyphLayout = layout[i];
                layout[i] = new GlyphLayout(glyphLayout.Glyph, glyphLayout.Location + offset, glyphLayout.Width, glyphLayout.Height);
            }
            return layout.ToImmutableArray();
        }
    }

    /// <summary>
    /// A glyphs layout and location
    /// </summary>
    public struct GlyphLayout
    {
        internal GlyphLayout(Glyph glyph, Vector2 location, float width, float height)
        {
            this.Glyph = glyph;
            this.Location = location;
            this.Width = width;
            this.Height = height;
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
    }
}
