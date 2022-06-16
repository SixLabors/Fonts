// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace SixLabors.Fonts.Benchmarks
{
    [MediumRunJob]
    public class MeasureTextBenchmark : IDisposable
    {
        private readonly TextOptions textOptions;
        private readonly SKTypeface arialTypeface;
        private readonly SKFont font;
        private readonly SKPaint paint;

        public MeasureTextBenchmark()
        {
            const string fontFamilyName = "Arial";
            const int fontSize = 16;

            this.textOptions = new TextOptions(SystemFonts.Get(fontFamilyName).CreateFont(fontSize, FontStyle.Regular));

            this.arialTypeface = SKTypeface.FromFamilyName(fontFamilyName, SKFontStyle.Normal);
            this.font = new SKFont(this.arialTypeface, fontSize);
            this.paint = new SKPaint(this.font);
        }

        public void Dispose()
        {
            this.arialTypeface.Dispose();
            this.font.Dispose();
            this.paint.Dispose();
        }

        [Params("a", "Hello world", "The quick brown fox jumps over the lazy dog")]
        public string Text { get; set; } = string.Empty;

        [Benchmark]
        public void SixLaborsFonts() => TextMeasurer.Measure(this.Text, this.textOptions);

        [Benchmark]
        public void SkiaSharp() => this.paint.MeasureText(this.Text);
    }
}
