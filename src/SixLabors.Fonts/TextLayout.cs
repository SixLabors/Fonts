using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public class TextLayout
    {
        public ImmutableArray<GlyphLayout> GenerateLayout(string text, FontStyle style)
        {
            AppliedFontStyle spanStyle = style.GetStyle(0, text.Length);
            List<GlyphLayout> layout = new List<GlyphLayout>(text.Length);

            float lineHeight = 0f;
            Vector2 location = Vector2.Zero;
            Glyph previousGlyph = null;
            ushort emSize = 0;
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
                    emSize = spanStyle.Font.EmSize;
                    lineHeight = (spanStyle.Font.LineHeight * spanStyle.PointSize) / scale;
                }
                var c = text[i];
                switch (c)
                {
                    case '\r':
                        // carrage return resets the XX cordinate to startXX 
                        location.X = 0;
                        previousGlyph = null;
                        break;
                    case '\n':
                        // carrage return resets the XX cordinate to startXX 
                        location.X = 0;
                        location.Y += lineHeight;
                        lineHeight = 0;// reset lighthight tracking fro next line 
                        previousGlyph = null;
                        break;
                    case '\t':
                        {
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            var tabStop = width * spanStyle.TabWidth;
                            //advance to a position > width away that 
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

                            layout.Add(new GlyphLayout(glyph, glyphLocation, width, height, spanStyle.PointSize));

                            // move foraward the actual with of the glyph, we are retaining the baseline
                            location.X += width;
                            previousGlyph = glyph;
                        }
                        break;
                }
            }

            return layout.ToImmutableArray();
        }
    }

    public struct GlyphLayout
    {
        public GlyphLayout(Glyph glyph, Vector2 location, float width, float height, float pointSize)
        {
            this.Glyph = glyph;
            this.Location = location;
            this.Width = width;
            this.Height = height;
            this.PointSize = pointSize;
        }

        public Glyph Glyph { get; private set; }
        public Vector2 Location { get; }
        public float Width { get; }
        public float Height { get; }
        public float PointSize { get; }
    }
}
