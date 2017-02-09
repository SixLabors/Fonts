using ImageSharp;
using System;
using System.IO;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    using Shapes;
    using System.Numerics;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var font = Font.LoadFont(@"..\..\tests\SixLabors.Fonts.Tests\Fonts\SixLaborsSamplesAB.ttf");
            RenderLetter(font, 'a');
            RenderLetter(font, 'b');
            RenderLetter(font, 'u');
            var font2 = Font.LoadFont(@"..\..\tests\SixLabors.Fonts.Tests\Fonts\OpenSans-Regular.ttf");
            RenderLetter(font2, 'a');
            RenderLetter(font2, 'b');
            RenderLetter(font2, 'u');
        }

        public static void RenderLetter(Font font, char character)
        {
            var g = font.GetGlyph(character);
            var builder = new GlyphBuilder();
            g.RenderTo(builder);
            builder.Path.Scale(1f / 7f)
                .SaveImage(font.FontName, character+".png");
        }

        public static void SaveImage(this IPath shape, params string[] path)
        {
            shape = shape.Translate(shape.Bounds.Location * -1) // touch top left
                    .Translate(new Vector2(10)); // move in from top left

            var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));
            // pad even amount around shape
            int width = (int)(shape.Bounds.Left + shape.Bounds.Right);
            int height = (int)(shape.Bounds.Top + shape.Bounds.Bottom);

            using (var img = new Image(width, height))
            {
                img.Fill(Color.DarkBlue);

                // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
                img.Fill(Color.HotPink, shape);
                img.Draw(Color.LawnGreen, 1, shape);

                // Ensure directory exists
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

                using (var fs = File.Create(fullPath))
                {
                    img.SaveAsPng(fs);
                }
            }
        }
    }
}
