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
            FontCollection fonts = new FontCollection();
            var font = fonts.Install(@"..\..\tests\SixLabors.Fonts.Tests\Fonts\SixLaborsSampleAB.ttf").Family;
            var fontWoff = fonts.Install(@"..\..\tests\SixLabors.Fonts.Tests\Fonts\SixLaborsSampleAB.woff").Family;
            var font2 = fonts.Install(@"..\..\tests\SixLabors.Fonts.Tests\Fonts\OpenSans-Regular.ttf").Family;

            RenderLetter(font, 'a');
            RenderLetter(font, 'b');
            RenderLetter(font, 'u');
            RenderText(font, "abc", 72);
            RenderText(font, "ABd", 72);
            RenderText(fontWoff, "abe", 72);
            RenderText(fontWoff, "ABf", 72);
            RenderLetter(font2, 'a', 72);
            RenderLetter(font2, 'b', 72);
            RenderLetter(font2, 'u', 72);
            RenderLetter(font2, 'o', 72);
            RenderText(font2, "ov", 72);
            RenderText(font2, "a\ta", 72);
            RenderText(font2, "aa\ta", 72);
            RenderText(font2, "aaa\ta", 72);
            RenderText(font2, "aaaa\ta", 72);
            RenderText(font2, "aaaaa\ta", 72);
            RenderText(font2, "aaaaaa\ta", 72);
            RenderText(font2, "Hello\nWorld", 72);
          //  RenderText(new Font(FontCollection.SystemFonts.Find("Arial"), 10f, FontStyle.Regular), "PGEP0JK867", 200, 50);
        }

        public static void RenderText(Font font, string text, int width, int height)
        {
            var path = System.IO.Path.GetInvalidFileNameChars().Aggregate(text, (x, c) => x.Replace($"{c}", "-"));
            var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));

            using (var img = new Image(width, height))
            {
                img.Fill(Color.White);

                img.DrawText(text, font, Color.Black, new Vector2(50f, 4f));

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

                using (var fs = File.Create(fullPath+".png"))
                {
                    img.SaveAsPng(fs);
                }
            }
        }

        public static void RenderText(FontFamily font, string text, float pointSize = 12)
        {
            var builder = new GlyphBuilder();
            var renderer = new TextRenderer(builder);

            renderer.RenderText(text, new FontSpan(new Font(font, pointSize)) { ApplyKerning = true }, 96);

            builder.Paths
                .SaveImage(font.Name, text + ".png");
        }

        public static void RenderLetter(FontFamily fontFam, char character, float pointSize = 12)
        {
            var font = new Font(fontFam, pointSize);
            var g = font.GetGlyph(character);
            var builder = new GlyphBuilder();
            g.RenderTo(builder, Vector2.Zero, 72f);
            builder.Paths
                .SaveImage(fontFam.Name, character + ".png");
        }
    
        public static void SaveImage(this IEnumerable<IPath> shapes, int width, int height, params string[] path)
        {
            path = path.Select(p => System.IO.Path.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
            var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));

            using (var img = new Image(width, height))
            {
                img.Fill(Color.DarkBlue);

                foreach (var s in shapes)
                {
                    // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
                    img.Fill(Color.HotPink, s.Translate(new Vector2(0, 0)));
                }
                // img.Draw(Color.LawnGreen, 1, shape);

                // Ensure directory exists
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

                using (var fs = File.Create(fullPath))
                {
                    img.SaveAsPng(fs);
                }
            }
        }

        public static void SaveImage(this IEnumerable<IPath> shapes, params string[] path)
        {
            IPath shape = new ComplexPolygon(shapes.ToArray());
            shape = shape.Translate(shape.Bounds.Location * -1) // touch top left
                    .Translate(new Vector2(10)); // move in from top left

            StringBuilder sb = new StringBuilder();
            var converted = shape.Flatten();
            converted.Aggregate(sb, (s, p) =>
            {
                foreach (var point in p.Points)
                {
                    sb.Append(point.X);
                    sb.Append('x');
                    sb.Append(point.Y);
                    sb.Append(' ');
                }
                s.Append('\n');
                return s;
            });
            var str = sb.ToString();
            shape = new ComplexPolygon(converted.Select(x => new Polygon(new LinearLineSegment(x.Points))).ToArray());

            path = path.Select(p => System.IO.Path.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
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
