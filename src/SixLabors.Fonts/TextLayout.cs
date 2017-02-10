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
                    var scale = spanStyle.Font.ScaleFactor;
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
                        location.Y += spanStyle.Font.LineHeight;
                        lineHeight = 0;// reset lighthight tracking fro next line 
                        previousGlyph = null;
                        break;

                    case ' ':
                        {
                            var scale = spanStyle.Font.ScaleFactor;
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;
                            location.X += width;
                        }
                        break;
                    default:
                        {
                            var scale = spanStyle.Font.ScaleFactor;
                            var glyph = spanStyle.Font.GetGlyph(c);
                            var width = (glyph.AdvanceWidth * spanStyle.PointSize) / scale;

                            layout.Add(new GlyphLayout(glyph, location, width, lineHeight, spanStyle.PointSize));
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
