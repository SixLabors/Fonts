using SixLabors.ImageSharp;
using System.IO;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    using global::DrawWithImageSharp;
    using Shapes;
    using SixLabors.ImageSharp.Drawing;
    using SixLabors.ImageSharp.Drawing.Processing;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using SixLabors.Shapes.Temp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var fonts = new FontCollection();
            FontFamily font = fonts.Install(@"Fonts\SixLaborsSampleAB.ttf");
            FontFamily fontWoff = fonts.Install(@"Fonts\SixLaborsSampleAB.woff");
            FontFamily carter = fonts.Install(@"Fonts\CarterOne.ttf");
            FontFamily Wendy_One = fonts.Install(@"Fonts\WendyOne-Regular.ttf");
            FontFamily ColorEmoji = fonts.Install(@"Fonts\Twemoji Mozilla.ttf");
            FontFamily font2 = fonts.Install(@"Fonts\OpenSans-Regular.ttf");
            var emojiFont = SystemFonts.Find("Segoe UI Emoji");
            // fallback font tests
            RenderTextProcessor(ColorEmoji, "a😀d", pointSize: 72, fallbackFonts: new[] { font2 });
            RenderText(ColorEmoji, "a😀d", pointSize: 72, fallbackFonts: new[] { font2 });

            RenderText(ColorEmoji, "😀", pointSize: 72, fallbackFonts: new[] { font2 });
            RenderText(emojiFont, "😀", pointSize: 72, fallbackFonts: new[] { font2 });
            RenderText(font2, "", pointSize: 72, fallbackFonts: new[] { emojiFont });
            RenderText(font2, "😀 Hello World! 😀", pointSize: 72, fallbackFonts: new[] { emojiFont });

            //// general

            RenderText(font, "abc", 72);
            RenderText(font, "ABd", 72);
            RenderText(fontWoff, "abe", 72);
            RenderText(fontWoff, "ABf", 72);
            RenderText(font2, "ov", 72);
            RenderText(font2, "a\ta", 72);
            RenderText(font2, "aa\ta", 72);
            RenderText(font2, "aaa\ta", 72);
            RenderText(font2, "aaaa\ta", 72);
            RenderText(font2, "aaaaa\ta", 72);
            RenderText(font2, "aaaaaa\ta", 72);
            RenderText(font2, "Hello\nWorld", 72);
            RenderText(carter, "Hello\0World", 72);
            RenderText(Wendy_One, "Hello\0World", 72);

            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 4 }, "\t\tx");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 4 }, "\t\t\tx");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 4 }, "\t\t\t\tx");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 4 }, "\t\t\t\t\tx");

            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 0 }, "Zero\tTab");

            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 0 }, "Zero\tTab");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 1 }, "One\tTab");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 6 }, "\tTab Then Words");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 1 }, "Tab Then Words");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 1 }, "Words Then Tab\t");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 1 }, "                 Spaces Then Words");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 1 }, "Words Then Spaces                 ");
            RenderText(new RendererOptions(new Font(font2, 72)) { TabWidth = 1 }, "\naaaabbbbccccddddeeee\n\t\t\t3 tabs\n\t\t\t\t\t5 tabs");

            RenderText(new Font(SystemFonts.Find("Arial"), 20f, FontStyle.Regular), "á é í ó ú ç ã õ", 200, 50);
            RenderText(new Font(SystemFonts.Find("Arial"), 10f, FontStyle.Regular), "PGEP0JK867", 200, 50);

            RenderText(new RendererOptions(SystemFonts.CreateFont("consolas", 72)) { TabWidth = 4 }, "xxxxxxxxxxxxxxxx\n\txxxx\txxxx\n\t\txxxxxxxx\n\t\t\txxxx");

            BoundingBoxes.Generate("a b c y q G H T", SystemFonts.CreateFont("arial", 40f));

            TextAlignment.Generate(SystemFonts.CreateFont("arial", 50f));
            TextAlignmentWrapped.Generate(SystemFonts.CreateFont("arial", 50f));

            var sb = new StringBuilder();
            for (char c = 'a'; c <= 'z'; c++)
            {
                sb.Append(c);
            }
            for (char c = 'A'; c <= 'Z'; c++)
            {
                sb.Append(c);
            }
            for (char c = '0'; c <= '9'; c++)
            {
                sb.Append(c);
            }
            string text = sb.ToString();

            foreach (FontFamily f in fonts.Families)
            {
                RenderText(f, text, 72);
            }
        }

        public static void RenderText(Font font, string text, int width, int height)
        {
            string path = System.IO.Path.GetInvalidFileNameChars().Aggregate(text, (x, c) => x.Replace($"{c}", "-"));
            string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));

            using (var img = new Image<Rgba32>(width, height))
            {
                img.Mutate(x => x.Fill(Color.White));

                IPathCollection shapes = SixLabors.Shapes.Temp.TextBuilder.GenerateGlyphs(text, new Vector2(50f, 4f), new RendererOptions(font, 72));
                img.Mutate(x => x.Fill(Color.Black, shapes));

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

                using (FileStream fs = File.Create(fullPath + ".png"))
                {
                    img.SaveAsPng(fs);
                }
            }
        }

        public static void RenderText(RendererOptions font, string text)
        {
            var builder = new GlyphBuilder();
            var renderer = new TextRenderer(builder);
            FontRectangle size = TextMeasurer.Measure(text, font);
            font.ColorFontSupport = ColorFontSupport.MicrosoftColrFormat;
            renderer.RenderText(text, font);

            builder.Paths
                .SaveImage(builder.PathColors, (int)size.Width + 20, (int)size.Height + 20, font.Font.Name, text + ".png");
        }
        public static void RenderText(FontFamily font, string text, float pointSize = 12, IEnumerable<FontFamily> fallbackFonts = null)
        {
            RenderText(new RendererOptions(new Font(font, pointSize), 96)
            {
                ApplyKerning = true,
                WrappingWidth = 340,
                FallbackFontFamilies = fallbackFonts?.ToArray(),
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            }, text);
        }
        public static void RenderTextProcessor(FontFamily fontFamily, string text, float pointSize = 12, IEnumerable<FontFamily> fallbackFonts = null)
        {
            var textOptions = new TextGraphicsOptionsCopy
            {
                ApplyKerning = true,
                DpiX = 96,
                DpiY = 96,
                RenderColorFonts = true,
            };
            if (fallbackFonts != null)
            {
                textOptions.FallbackFonts.AddRange(fallbackFonts);
            }
            var font = new Font(fontFamily, pointSize);
            var renderOptions = new RendererOptions(font, textOptions.DpiX, textOptions.DpiY)
            {
                ApplyKerning = true,
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat,
                FallbackFontFamilies = textOptions.FallbackFonts?.ToArray()
            };

            var textSize = TextMeasurer.Measure(text, renderOptions);
            using (var img = new Image<Rgba32>((int)Math.Ceiling(textSize.Width) + 20, (int)Math.Ceiling(textSize.Height) + 20))
            {
                img.Mutate(x => x.Fill(Color.White).ApplyProcessor(new DrawTextProcessorCopy(textOptions, text, font, new SolidBrushCopy(Color.Black), null, new PointF(5, 5))));

                string fullPath = CreatePath(font.Name, text + ".caching.png");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
                img.Save(fullPath);
            }
        }

        public static void SaveImage(this IEnumerable<IPath> shapes, int width, int height, params string[] path)
        {
            shapes.SaveImage(new Color?[shapes.Count()], width, height, path);
        }

        private static string CreatePath(params string[] path)
        {

            path = path.Select(p => System.IO.Path.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
            string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));
            return fullPath;
        }

        public static void SaveImage(this IEnumerable<IPath> shapes, IEnumerable<Color?> colors, int width, int height, params string[] path)
        {
            string fullPath = CreatePath(path);
            using (var img = new Image<Rgba32>(width, height))
            {
                img.Mutate(x => x.Fill(Color.White));
                var shapesArray = shapes.ToArray();
                var colorsArray = colors.ToArray();

                for (var i = 0; i < shapesArray.Length; i++)
                {
                    var s = shapesArray[i];
                    var c = colorsArray[i] ?? Color.HotPink;

                    // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
                    img.Mutate(x => x.Fill(new ShapeGraphicsOptions
                    {
                        IntersectionRule = IntersectionRule.Nonzero
                    }, c, s.Translate(new Vector2(0, 0))));
                }
                // img.Draw(Color.LawnGreen, 1, shape);

                // Ensure directory exists
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

                using (FileStream fs = File.Create(fullPath))
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

            var sb = new StringBuilder();
            IEnumerable<ISimplePath> converted = shape.Flatten();
            converted.Aggregate(sb, (s, p) =>
            {
                foreach (Vector2 point in p.Points)
                {
                    sb.Append(point.X);
                    sb.Append('x');
                    sb.Append(point.Y);
                    sb.Append(' ');
                }
                s.Append('\n');
                return s;
            });
            string str = sb.ToString();
            shape = new ComplexPolygon(converted.Select(x => new Polygon(new LinearLineSegment(x.Points.ToArray()))).ToArray());

            path = path.Select(p => System.IO.Path.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
            string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine("Output", System.IO.Path.Combine(path)));
            // pad even amount around shape
            int width = (int)(shape.Bounds.Left + shape.Bounds.Right);
            int height = (int)(shape.Bounds.Top + shape.Bounds.Bottom);
            if (width < 1)
            {
                width = 1;
            }
            if (height < 1)
            {
                height = 1;
            }
            using (var img = new Image<Rgba32>(width, height))
            {
                img.Mutate(x => x.Fill(Color.DarkBlue));

                // In ImageSharp.Drawing.Paths there is an extension method that takes in an IShape directly.
                img.Mutate(x => x.Fill(Color.HotPink, shape));
                // img.Draw(Color.LawnGreen, 1, shape);

                // Ensure directory exists
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));

                using (FileStream fs = File.Create(fullPath))
                {
                    img.SaveAsPng(fs);
                }
            }
        }
    }
}
