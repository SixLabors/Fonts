using ImageSharp;
using ImageSharp.Drawing;
using SixLabors.Fonts;
using SixLabors.Fonts.DrawWithImageSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace DrawWithImageSharp
{
    public static class TextAlignment
    {

        public static void Generate(Font font)
        {
            using (var img = new Image<Rgba32>(1000, 1000))
            {
                img.Fill(Rgba32.White);

                foreach (VerticalAlignment v in Enum.GetValues(typeof(VerticalAlignment)))
                {
                    foreach (HorizontalAlignment h in Enum.GetValues(typeof(HorizontalAlignment)))
                    {
                        Draw(img, font, v, h);
                    }
                }
                img.Save("Output/Alignment.png");
            }
        }

        public static void Draw(Image<Rgba32> img, Font font, VerticalAlignment vert, HorizontalAlignment horiz)
        {
            Vector2 location = Vector2.Zero;

            switch (vert)
            {
                case VerticalAlignment.Top:
                    location.Y = 0;
                    break;
                case VerticalAlignment.Center:
                    location.Y = img.Height / 2;
                    break;
                case VerticalAlignment.Bottom:
                    location.Y = img.Height;
                    break;
                default:
                    break;
            }
            switch (horiz)
            {
                case HorizontalAlignment.Left:

                    location.X = 0;
                    break;
                case HorizontalAlignment.Right:
                    location.X = img.Width;
                    break;
                case HorizontalAlignment.Center:
                    location.X = img.Width / 2;
                    break;
                default:
                    break;
            }

            GlyphBuilder glyphBuilder = new GlyphBuilder(location);

            TextRenderer renderer = new TextRenderer(glyphBuilder);

            Vector2 dpi = new Vector2(72);

            FontSpan style = new FontSpan(font, dpi)
            {
                ApplyKerning = true,
                TabWidth = 4,
                WrappingWidth = 0,
                HorizontalAlignment = horiz,
                VerticalAlignment = vert
            };

            string text = $"{horiz}\n{vert}";
            renderer.RenderText(text, style);

            System.Collections.Generic.IEnumerable<SixLabors.Shapes.IPath> shapesToDraw = glyphBuilder.Paths;
            foreach (SixLabors.Shapes.IPath s in shapesToDraw)
            {
                img.Fill(Rgba32.Black, s);
            }
        }
    }
}
