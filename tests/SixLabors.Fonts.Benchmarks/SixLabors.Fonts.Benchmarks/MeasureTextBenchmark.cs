// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using BenchmarkDotNet.Attributes;
using SkiaSharp;

namespace SixLabors.Fonts.Benchmarks;

/// <summary>
/// <para>
/// This benchmark is not actually measuring the same operation as SkiSharp is
/// not doing any layout or shaping operations. However it is useful as a marker to measure
/// performance against.
/// </para>
/// <para>We should see if we can include the Skia HarfBuzz extensions to see how we compare.</para>
/// </summary>
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
    public void SixLaborsFonts() => TextMeasurer.MeasureSize(this.Text, this.textOptions);

    [Benchmark]
    public void SkiaSharp() => this.paint.MeasureText(this.Text);
}
