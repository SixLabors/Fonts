// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.IO;

namespace SixLabors.Fonts.Benchmarks
{
    /// <summary>
    /// <para>
    /// This benchmark is not actually measuring the same operation as SkiSharp is
    /// not doing any layout or shaping operations. However it is useful as a marker to measure
    /// performance against.
    /// </para>
    /// <para>We should see if we can include the Skia HarfBuzz extensions to see how we compare.</para>
    /// </summary>
    [MediumRunJob]
    public class ImageSharpDraw
    {
        private readonly TextOptions textOptions;

        private Image<Rgba32> Image { get; set; }

        private static string? CrazyText;

        public ImageSharpDraw()
        {
            this.Image = new Image<Rgba32>(100, 100);
            const string fontFamilyName = "Arial";
            const int fontSize = 16;

            this.textOptions = new TextOptions(SystemFonts.Get(fontFamilyName).CreateFont(fontSize, FontStyle.Regular));
            CrazyText ??= File.ReadAllText(System.IO.Path.GetFullPath("..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\tests\\UnicodeTestData\\GraphemeBreakTest.txt"));
        }

        [Params("a", "Hello world", "The quick brown fox jumps over the lazy dog", "Text From File")]
        public string Text { get; set; } = string.Empty;

        [Benchmark]
        public void Render()
        {
            if (this.Text == "Text From File")
            {
                this.Image.Mutate((x) => x.DrawText(this.textOptions, CrazyText, Color.Gray));
            }
            else
            {
                this.Image.Mutate((x) => x.DrawText(this.textOptions, this.Text, Color.Gray));
            }
        }

    }
}
