using ImageSharp;
using System;
using System.IO;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    using Shapes;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;

    public static class Program
    {
        public static void Main(string[] args)
        {
            //var font = Font.LoadFont(@"..\..\tests\SixLabors.Fonts.Tests\Fonts\SixLaborsSamplesAB.ttf");
            //RenderLetter(font, 'a');
            //RenderLetter(font, 'b');
            //RenderLetter(font, 'u');
            var font2 = Font.LoadFont(@"..\..\tests\SixLabors.Fonts.Tests\Fonts\OpenSans-Regular.ttf");
            RenderLetter(font2, 'a', 72);
            //RenderLetter(font2, 'b', 72);
            //RenderLetter(font2, 'u', 72);
            //RenderText(font2, "Hello World", 72);
        }
        public static void RenderText(Font font, string text, float pointSize = 12)
        {
            var builder = new GlyphBuilder(72f);
            var renderer = new TextRenderer(builder);

            renderer.RenderText(text, new FontStyle(font, pointSize));

            builder.Paths
                .SaveImage(font.FontName, text + ".png");
        }

        public static void RenderLetter(Font font, char character, float pointSize = 12)
        {
            var g = font.GetGlyph(character);
            var builder = new GlyphBuilder(72f);
            g.RenderTo(builder, pointSize);
            builder.Paths
                .SaveImage(font.FontName, character+".png");
        }

        public static void SaveImage(this IEnumerable<IPath> shapes, params string[] path)
        {
            IPath shape = new ComplexPolygon(shapes.ToArray());
            shape = shape.Translate(shape.Bounds.Location * -1) // touch top left
                    .Translate(new Vector2(10)); // move in from top left

            //StringBuilder sb = new StringBuilder();
            //var converted = shape.Flatten();
            //converted.Aggregate(sb, (s, p) =>
            //{
            //    foreach (var point in p.Points) {
            //        sb.Append(point.X);
            //        sb.Append('x');
            //        sb.Append(point.Y);
            //        sb.Append(' ');
            //    }
            //    s.Append('\n');
            //    return s;
            //});
            //var str = sb.ToString();
            //shape = new ComplexPolygon(converted.Select(x => new Polygon(new LinearLineSegment(x.Points))).ToArray()); 

            var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));
            // pad even amount around shape
            int width = (int)(shape.Bounds.Left + shape.Bounds.Right);
            int height = (int)(shape.Bounds.Top + shape.Bounds.Bottom);

            using (var img = new Image(width, height))
            {
                img.Fill(Color.DarkBlue);

                // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
                img.Fill(Color.HotPink, shape);
               // img.Draw(Color.LawnGreen, 1, shape);

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
