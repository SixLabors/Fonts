// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using BenchmarkDotNet.Attributes;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBDirection = HarfBuzzSharp.Direction;

namespace SixLabors.Fonts.Benchmarks;

/// <summary>
/// Defines the single directional runs used by <see cref="MeasureShapedRunBenchmark"/>.
/// </summary>
public enum MeasureShapedRunScenario
{
    /// <summary>
    /// Latin text with standard ligature and kerning opportunities.
    /// </summary>
    Latin,

    /// <summary>
    /// Arabic text exercising joining forms and mandatory ligatures.
    /// </summary>
    Arabic,

    /// <summary>
    /// Devanagari text exercising conjuncts, matras, and reordering.
    /// </summary>
    Devanagari
}

/// <summary>
/// Compares shaping and positioned glyph-run bounds measurement for one directional run.
/// Both sides use the same font file, point size, text, and stated direction.
/// </summary>
[Config(typeof(Config.Gate))]
public class MeasureShapedRunBenchmark : IDisposable
{
    private const float FontSize = 16F;

    private readonly TextShapingBuffer shapingBuffer = new();
    private string text = string.Empty;
    private TextDirection direction;

    // BenchmarkDotNet runs GlobalSetup before every benchmark case. These fields deliberately
    // use the null-forgiving operator so the measured methods do not pay guard branches that
    // the opposing implementation does not pay.
    private Font font = null!;
    private GlyphOptions glyphOptions = null!;
    private SKTypeface skTypeface = null!;
    private SKFont skFont = null!;
    private SKShaper skShaper = null!;
    private HBBuffer skBuffer = null!;

    private ushort[] skGlyphIds = [];
    private float[] skGlyphWidths = [];
    private SKRect[] skGlyphBounds = [];

    /// <summary>
    /// Gets or sets the text scenario used by the benchmark.
    /// </summary>
    [ParamsAllValues]
    public MeasureShapedRunScenario Scenario { get; set; }

    /// <summary>
    /// Loads identical font bytes into both engines and prepares reusable result storage.
    /// </summary>
    [GlobalSetup]
    public void SetUp()
    {
        string fontPath;
        switch (this.Scenario)
        {
            case MeasureShapedRunScenario.Latin:
                fontPath = GetRepositoryPath("tests/Fonts/OpenSans-Regular.ttf");
                this.text = "The quick brown fox jumps over the lazy dog; fifty fluffy waffles.";
                this.direction = TextDirection.LeftToRight;
                break;
            case MeasureShapedRunScenario.Arabic:
                fontPath = GetRepositoryPath("tests/Fonts/Dubai-Regular.ttf");
                this.text = "سلام عليكم ورحمة الله وبركاته لا إله إلا الله";
                this.direction = TextDirection.RightToLeft;
                break;
            case MeasureShapedRunScenario.Devanagari:
                fontPath = GetRepositoryPath("tests/Fonts/NotoSansDevanagari-Regular.ttf");
                this.text = "क्षत्रिय द्वारा प्रकृति की रक्षा कर्तव्य है";
                this.direction = TextDirection.LeftToRight;
                break;
            default:
                throw new InvalidOperationException($"Unknown measurement benchmark scenario '{this.Scenario}'.");
        }

        this.font = new FontCollection().Add(fontPath).CreateFont(FontSize);
        this.glyphOptions = new GlyphOptions { Font = this.font };

        this.skTypeface = SKTypeface.FromFile(fontPath);
        this.skFont = new SKFont(this.skTypeface, FontSize);
        this.skShaper = new SKShaper(this.skTypeface);
        this.skBuffer = new HBBuffer();

        // Shape once outside measurement only to size the reusable representation and
        // measurement adapters required by the Skia API.
        this.PrepareSkiaBuffer();
        SKShaper.Result result = this.skShaper.Shape(this.skBuffer, this.skFont);
        this.skGlyphIds = new ushort[result.Codepoints.Length];
        this.skGlyphWidths = new float[result.Codepoints.Length];
        this.skGlyphBounds = new SKRect[result.Codepoints.Length];
    }

    /// <summary>
    /// Shapes and measures one positioned glyph run with SixLabors.Fonts.
    /// </summary>
    /// <returns>The positioned run bounds.</returns>
    [Benchmark]
    public FontRectangle SixLaborsFonts()
    {
        this.shapingBuffer.Add(this.text);
        this.shapingBuffer.TextDirection = this.direction;
        TextShaper.ShapeRun(this.font, this.shapingBuffer);

        return TextMeasurer.MeasureBounds(this.shapingBuffer, this.glyphOptions);
    }

    /// <summary>
    /// Shapes and measures one positioned glyph run with SkiaSharp and its HarfBuzz extension.
    /// </summary>
    /// <returns>The positioned run bounds.</returns>
    [Benchmark(Baseline = true)]
    public SKRect SkiaSharp()
    {
        this.PrepareSkiaBuffer();
        SKShaper.Result result = this.skShaper.Shape(this.skBuffer, this.skFont);

        Span<ushort> glyphIds = this.skGlyphIds.AsSpan(0, result.Codepoints.Length);
        Span<float> glyphWidths = this.skGlyphWidths.AsSpan(0, result.Codepoints.Length);
        Span<SKRect> glyphBounds = this.skGlyphBounds.AsSpan(0, result.Codepoints.Length);
        for (int i = 0; i < glyphIds.Length; i++)
        {
            // HarfBuzz exposes glyph identifiers as uint while Skia fonts accept ushort.
            // OpenType glyph identifiers are 16-bit, so this is a representation conversion.
            glyphIds[i] = (ushort)result.Codepoints[i];
        }

        // SKTextBlob.Bounds encloses the position envelope with the font-wide bounds, so it
        // is deliberately conservative rather than the tight per-glyph contract measured here:
        // https://skia.googlesource.com/skia/+/6b0f264bde33/src/core/SkTextBlob.cpp#329
        this.skFont.GetGlyphWidths(glyphIds, glyphWidths, glyphBounds, null);

        ReadOnlySpan<SKPoint> points = result.Points;
        SKRect bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < glyphBounds.Length; i++)
        {
            SKRect positioned = glyphBounds[i];
            if (positioned.IsEmpty)
            {
                // SixLabors uses the horizontal advance as the bounds of an outline-less
                // glyph, so use the width returned by the same native query to match it.
                positioned.Right = glyphWidths[i];
            }

            positioned.Offset(points[i]);
            if (hasBounds)
            {
                // Union the edges directly because SKRect.Union ignores empty rectangles,
                // while the SixLabors contract retains zero-height advance bounds.
                bounds = new SKRect(
                    MathF.Min(bounds.Left, positioned.Left),
                    MathF.Min(bounds.Top, positioned.Top),
                    MathF.Max(bounds.Right, positioned.Right),
                    MathF.Max(bounds.Bottom, positioned.Bottom));
            }
            else
            {
                bounds = positioned;
                hasBounds = true;
            }
        }

        return bounds;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.skBuffer?.Dispose();
        this.skShaper?.Dispose();
        this.skFont?.Dispose();
        this.skTypeface?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Restores the caller-owned HarfBuzz buffer to the same stated run properties for each
    /// Skia invocation.
    /// </summary>
    private void PrepareSkiaBuffer()
    {
        this.skBuffer.Reset();
        this.skBuffer.AddUtf16(this.text);
        this.skBuffer.Direction = this.direction == TextDirection.RightToLeft
            ? HBDirection.RightToLeft
            : HBDirection.LeftToRight;

        // Direction is stated by the benchmark; HarfBuzz infers script and language from the
        // same homogeneous run that SixLabors receives.
        this.skBuffer.GuessSegmentProperties();
    }

    /// <summary>
    /// Resolves a repository-relative path by walking up from the benchmark output directory.
    /// </summary>
    /// <param name="relativePath">The path relative to the repository root.</param>
    /// <returns>The full font path.</returns>
    private static string GetRepositoryPath(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SixLabors.Fonts.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new IOException("Unable to locate the repository root.");
        }

        return Path.Combine(directory.FullName, relativePath);
    }
}
