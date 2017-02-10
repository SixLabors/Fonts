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
                            var tabStop = width * 4;
                            //advance to a position > width away that 
                            var dist = tabStop - ((location.X + width) % tabStop);
                            location.X += dist;
                        }
                        break;
                    case ' ':
                        {
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            location.X += width;
                        }
                        break;
                    default:
                        {
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            var height = (glyph.Height * spanStyle.PointSize) / scale;

                            layout.Add(new GlyphLayout(glyph, location, width, height, spanStyle.PointSize));
                            if (previousGlyph != null)
                            {
                                // if there is special instructions for this glyph pair use that width
                                location.X += spanStyle.Font.GetAdvancedWidth(glyph, previousGlyph);
                            }
                            else
                            {
                                location.X += width;
                            }
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
