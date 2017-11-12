using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.Shapes.Temp;
using System;
using System.Numerics;

namespace DrawWithImageSharp
{
    public static class TextAlignmentWrapped
    {

        public static void Generate(Font font)
        {
            int wrappingWidth = 400;
            var size = (wrappingWidth + wrappingWidth/3) * 3;
            using (var img = new Image<Rgba32>(size, size))
            {
                img.Mutate(x => x.Fill(Rgba32.White));

                foreach (VerticalAlignment v in Enum.GetValues(typeof(VerticalAlignment)))
                {
                    foreach (HorizontalAlignment h in Enum.GetValues(typeof(HorizontalAlignment)))
                    {
                        Draw(img, font, v, h, wrappingWidth);
                    }
                }
                img.Save("Output/AlignmentWrapped.png");
            }
        }

        public static void Draw(Image<Rgba32> img, Font font, VerticalAlignment vert, HorizontalAlignment horiz, float wrappingWidth)
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
                    location.X = img.Width - wrappingWidth;
                    break;
                case HorizontalAlignment.Center:
                    location.X = (img.Width - wrappingWidth) / 2;
                    break;
                default:
                    break;
            }

            GlyphBuilder glyphBuilder = new GlyphBuilder();

            TextRenderer renderer = new TextRenderer(glyphBuilder);

            RendererOptions style = new RendererOptions(font, 72, location)
            {
                ApplyKerning = true,
                TabWidth = 4,
                WrappingWidth = wrappingWidth,
                HorizontalAlignment = horiz,
                VerticalAlignment = vert
            };

            string text = $"    {horiz}     {vert}         {horiz}     {vert}         {horiz}     {vert}     ";
            renderer.RenderText(text, style);

            System.Collections.Generic.IEnumerable<SixLabors.Shapes.IPath> shapesToDraw = glyphBuilder.Paths;
            img.Mutate(x => x.Fill(Rgba32.Black, glyphBuilder.Paths));

            var f = Rgba32.Fuchsia;
            f.A = 128;
            img.Mutate(x => x.Fill(Rgba32.Black, glyphBuilder.Paths));
            img.Mutate(x => x.Draw(f, 1, glyphBuilder.Boxes));

            img.Mutate(x => x.Draw(Rgba32.Lime, 1, glyphBuilder.TextBox));
        }
    }
}
