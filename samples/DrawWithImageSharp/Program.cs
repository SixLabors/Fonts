// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using DrawWithImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing.Processing.Processors.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IOPath = System.IO.Path;

namespace SixLabors.Fonts.DrawWithImageSharp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var fonts = new FontCollection();
            FontFamily font = fonts.Add(@"Fonts\SixLaborsSampleAB.ttf");
            FontFamily fontWoff = fonts.Add(@"Fonts\SixLaborsSampleAB.woff");
            FontFamily fontWoff2 = fonts.Add(@"Fonts\OpenSans-Regular.woff2");
            FontFamily carter = fonts.Add(@"Fonts\CarterOne.ttf");
            FontFamily wendyOne = fonts.Add(@"Fonts\WendyOne-Regular.ttf");
            FontFamily whitneyBook = fonts.Add(@"Fonts\whitney-book.ttf");
            FontFamily colorEmoji = fonts.Add(@"Fonts\Twemoji Mozilla.ttf");
            FontFamily font2 = fonts.Add(@"Fonts\OpenSans-Regular.ttf");
            FontFamily sunflower = fonts.Add(@"Fonts\Sunflower-Medium.ttf");

#if OS_WINDOWS
            FontFamily emojiFont = SystemFonts.Get("Segoe UI Emoji");
            FontFamily uiFont = SystemFonts.Get("Segoe UI");

            RenderText(emojiFont, "👩🏽‍🚒a", pointSize: 72, fallbackFonts: new[] { font2 });
            RenderTextProcessorWithAlignment(emojiFont, "😀A😀", pointSize: 20, fallbackFonts: new[] { colorEmoji });
            RenderTextProcessorWithAlignment(uiFont, "this\nis\na\ntest", pointSize: 20, fallbackFonts: new[] { font2 });
            RenderTextProcessorWithAlignment(uiFont, "first\n\n\n\nlast", pointSize: 20, fallbackFonts: new[] { font2 });

            RenderText(emojiFont, "😀", pointSize: 72, fallbackFonts: new[] { font2 });
            RenderText(font2, string.Empty, pointSize: 72, fallbackFonts: new[] { emojiFont });
            RenderText(font2, "😀 Hello World! 😀", pointSize: 72, fallbackFonts: new[] { emojiFont });
#endif

            // fallback font tests
            RenderTextProcessor(colorEmoji, "a😀d", pointSize: 72, fallbackFonts: new[] { font2 });
            RenderText(colorEmoji, "a😀d", pointSize: 72, fallbackFonts: new[] { font2 });

            RenderText(colorEmoji, "😀", pointSize: 72, fallbackFonts: new[] { font2 });

            //// general
            RenderText(font, "abc", 72);
            RenderText(font, "ABd", 72);
            RenderText(fontWoff, "abe", 72);
            RenderText(fontWoff, "ABf", 72);
            RenderText(fontWoff2, "woff2", 72);
            RenderText(font2, "ov", 72);
            RenderText(font2, "a\ta", 72);
            RenderText(font2, "aa\ta", 72);
            RenderText(font2, "aaa\ta", 72);
            RenderText(font2, "aaaa\ta", 72);
            RenderText(font2, "aaaaa\ta", 72);
            RenderText(font2, "aaaaaa\ta", 72);
            RenderText(font2, "Hello\nWorld", 72);
            RenderText(carter, "Hello\0World", 72);
            RenderText(wendyOne, "Hello\0World", 72);
            RenderText(whitneyBook, "Hello\0World", 72);
            RenderText(sunflower, "í", 30);

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

#if OS_WINDOWS
            RenderText(new Font(SystemFonts.Get("Arial"), 20f, FontStyle.Regular), "á é í ó ú ç ã õ", 200, 50);
            RenderText(new Font(SystemFonts.Get("Arial"), 10f, FontStyle.Regular), "PGEP0JK867", 200, 50);
            RenderText(new RendererOptions(SystemFonts.CreateFont("consolas", 72)) { TabWidth = 4 }, "xxxxxxxxxxxxxxxx\n\txxxx\txxxx\n\t\txxxxxxxx\n\t\t\txxxx");
            BoundingBoxes.Generate("a b c y q G H T", SystemFonts.CreateFont("arial", 40f));
            TextAlignment.Generate(SystemFonts.CreateFont("arial", 50f));
            TextAlignmentWrapped.Generate(SystemFonts.CreateFont("arial", 50f));

            FontFamily simsum = SystemFonts.Get("SimSun");
            RenderText(simsum, "这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 16);

            FontFamily jhengHei = SystemFonts.Get("Microsoft JhengHei");
            RenderText(jhengHei, " ，；：！￥（）？｛｝－＝＋＼｜～！＠＃％＆", 16);

            FontFamily arial = SystemFonts.Get("Arial");
            RenderText(arial, "ìíîï", 72);
#endif
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
            string path = IOPath.GetInvalidFileNameChars().Aggregate(text, (x, c) => x.Replace($"{c}", "-"));
            string fullPath = IOPath.GetFullPath(IOPath.Combine("Output", IOPath.Combine(path)));

            using var img = new Image<Rgba32>(width, height);
            img.Mutate(x => x.Fill(Color.White));

            IPathCollection shapes = TextBuilder.GenerateGlyphs(text, new Vector2(50f, 4f), new RendererOptions(font, 72));
            img.Mutate(x => x.Fill(Color.Black, shapes));

            Directory.CreateDirectory(IOPath.GetDirectoryName(fullPath));

            using FileStream fs = File.Create(fullPath + ".png");
            img.SaveAsPng(fs);
        }

        public static void RenderText(RendererOptions options, string text)
        {
            FontRectangle size = TextMeasurer.Measure(text, options);

            var drawingOptions = new DrawingOptions
            {
                TextOptions = new TextOptions()
                {
                    ApplyKerning = options.ApplyKerning,
                    DpiX = options.DpiX,
                    DpiY = options.DpiY,
                    TabWidth = options.TabWidth,
                    LineSpacing = options.LineSpacing,
                    HorizontalAlignment = options.HorizontalAlignment,
                    VerticalAlignment = options.VerticalAlignment,
                    WrapTextWidth = options.WrappingWidth,
                    RenderColorFonts = options.ColorFontSupport != ColorFontSupport.None
                }
            };

            if (options.FallbackFontFamilies != null)
            {
                drawingOptions.TextOptions.FallbackFonts.AddRange(options.FallbackFontFamilies);
            }

            SaveImage(drawingOptions, text, options.Font, (int)size.Width + 20, (int)size.Height + 20, options.Origin + Vector2.Abs(new Vector2(size.X, size.Y)), options.Font.Name, text + ".png");
        }

        public static void RenderText(FontFamily font, string text, float pointSize = 12, IEnumerable<FontFamily> fallbackFonts = null)
            => RenderText(
                new RendererOptions(new Font(font, pointSize), 96)
                {
                    ApplyKerning = true,
                    WrappingWidth = 340,
                    FallbackFontFamilies = fallbackFonts?.ToArray(),
                    ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
                },
                text);

        public static void RenderTextProcessor(
            FontFamily fontFamily,
            string text,
            float pointSize = 12,
            IEnumerable<FontFamily> fallbackFonts = null)
        {
            var textOptions = new TextOptions
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

            var options = new DrawingOptions
            {
                TextOptions = textOptions
            };

            FontRectangle textSize = TextMeasurer.Measure(text, renderOptions);
            using var img = new Image<Rgba32>((int)Math.Ceiling(textSize.Width) + 20, (int)Math.Ceiling(textSize.Height) + 20);
            img.Mutate(x => x.Fill(Color.White).ApplyProcessor(new DrawTextProcessor(options, text, font, new SolidBrush(Color.Black), null, new PointF(5, 5))));

            string fullPath = CreatePath(font.Name, text + ".caching.png");
            Directory.CreateDirectory(IOPath.GetDirectoryName(fullPath));
            img.Save(fullPath);
        }

        public static void RenderTextProcessorWithAlignment(
            FontFamily fontFamily,
            string text,
            float pointSize = 12,
            IEnumerable<FontFamily> fallbackFonts = null)
        {
            foreach (VerticalAlignment va in (VerticalAlignment[])Enum.GetValues(typeof(VerticalAlignment)))
            {
                foreach (HorizontalAlignment ha in (HorizontalAlignment[])Enum.GetValues(typeof(HorizontalAlignment)))
                {
                    var textOptions = new TextOptions
                    {
                        ApplyKerning = true,
                        DpiX = 96,
                        DpiY = 96,
                        RenderColorFonts = true,
                        VerticalAlignment = va,
                        HorizontalAlignment = ha
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
                        FallbackFontFamilies = textOptions.FallbackFonts?.ToArray(),
                        VerticalAlignment = va,
                        HorizontalAlignment = ha
                    };

                    FontRectangle textSize = TextMeasurer.Measure(text, renderOptions);
                    using var img = new Image<Rgba32>(((int)textSize.Width * 2) + 20, ((int)textSize.Height * 2) + 20);

                    var options = new DrawingOptions
                    {
                        TextOptions = textOptions
                    };

                    Size size = img.Size();
                    img.Mutate(x => x.Fill(Color.White).ApplyProcessor(
                        new DrawTextProcessor(
                            options,
                            text,
                            font,
                            new SolidBrush(Color.Black),
                            null,
                            new PointF(size.Width / 2F, size.Height / 2F))));

                    img[size.Width / 2, size.Height / 2] = Color.Black;

                    string h = ha.ToString().Replace(nameof(HorizontalAlignment), string.Empty).ToLower();
                    string v = va.ToString().Replace(nameof(VerticalAlignment), string.Empty).ToLower();

                    string fullPath = CreatePath(font.Name, text + "-" + h + "-" + v + ".png");
                    Directory.CreateDirectory(IOPath.GetDirectoryName(fullPath));
                    img.Save(fullPath);
                }
            }
        }

        private static string CreatePath(params string[] path)
        {
            path = path.Select(p => IOPath.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
            return IOPath.GetFullPath(IOPath.Combine("Output", IOPath.Combine(path)));
        }

        private static void SaveImage(
            DrawingOptions options,
            string text,
            Font font,
            int width,
            int height,
            Vector2 origin,
            params string[] path)
        {
            string fullPath = CreatePath(path);

            using var img = new Image<Rgba32>(width, height);
            img.Mutate(x => x.Fill(Color.White));

            img.Mutate(x => x.DrawText(options, text, font, Color.HotPink, origin));

            // Ensure directory exists
            Directory.CreateDirectory(IOPath.GetDirectoryName(fullPath));

            using FileStream fs = File.Create(fullPath);
            img.SaveAsPng(fs);
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
                ReadOnlySpan<PointF> points = p.Points.Span;
                for (int i = 0; i < points.Length; i++)
                {
                    PointF point = points[i];
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

            path = path.Select(p => IOPath.GetInvalidFileNameChars().Aggregate(p, (x, c) => x.Replace($"{c}", "-"))).ToArray();
            string fullPath = IOPath.GetFullPath(IOPath.Combine("Output", IOPath.Combine(path)));

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

            using var img = new Image<Rgba32>(width, height);
            img.Mutate(x => x.Fill(Color.DarkBlue));
            img.Mutate(x => x.Fill(Color.HotPink, shape));

            // img.Draw(Color.LawnGreen, 1, shape);

            // Ensure directory exists
            Directory.CreateDirectory(IOPath.GetDirectoryName(fullPath));

            using FileStream fs = File.Create(fullPath);
            img.SaveAsPng(fs);
        }
    }
}
