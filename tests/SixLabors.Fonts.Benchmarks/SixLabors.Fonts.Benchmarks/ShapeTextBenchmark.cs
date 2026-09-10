// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using BenchmarkDotNet.Attributes;
using HarfBuzzSharp;
using SixLabors.Fonts.Unicode;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBFace = HarfBuzzSharp.Face;
using HBFont = HarfBuzzSharp.Font;
using HBScript = HarfBuzzSharp.Script;

namespace SixLabors.Fonts.Benchmarks;

/// <summary>
/// Defines the text shape used by <see cref="ShapeTextBenchmark"/>.
/// </summary>
public enum ShapeTextBenchmarkScenario
{
    /// <summary>
    /// Latin text with standard ligature opportunities, shaped with Open Sans.
    /// </summary>
    Latin,

    /// <summary>
    /// Arabic text exercising joining forms and mandatory ligatures, shaped with Dubai.
    /// </summary>
    Arabic,

    /// <summary>
    /// Hebrew text exercising presentation forms and mark positioning, shaped with
    /// Noto Sans Hebrew.
    /// </summary>
    Hebrew,

    /// <summary>
    /// Thai text exercising mark ordering and Sara Am decomposition, shaped with Sarabun.
    /// </summary>
    Thai,

    /// <summary>
    /// Hangul text exercising precomposed syllables and conjoining Jamo, shaped with
    /// Nanum Gothic Coding.
    /// </summary>
    Hangul,

    /// <summary>
    /// Devanagari text exercising conjuncts, matras, and reordering, shaped with
    /// Noto Sans Devanagari.
    /// </summary>
    Devanagari,

    /// <summary>
    /// Khmer text exercising split vowels, coeng sequences, and reordering, shaped
    /// with Noto Sans Khmer.
    /// </summary>
    Khmer,

    /// <summary>
    /// Myanmar text exercising medial forms and kinzi sequences, shaped with Noto
    /// Sans Myanmar.
    /// </summary>
    Myanmar,

    /// <summary>
    /// Zawgyi text exercising the explicit Qaag script override, shaped with the
    /// corpus Zawgyi font.
    /// </summary>
    MyanmarZawgyi,

    /// <summary>
    /// Balinese text exercising the universal shaping engine's syllable analysis
    /// and reordering, shaped with Noto Sans Balinese.
    /// </summary>
    Balinese
}

/// <summary>
/// Compares end to end single run text shaping between <see cref="TextShaper"/> and
/// HarfBuzz via HarfBuzzSharp. Both sides shape identical font file bytes and walk the
/// resulting glyph stream, summing the advances so the output is fully consumed.
/// </summary>
/// <remarks>
/// Both sides are handed one run whose direction is stated, so each does the same
/// work: neither divides the text, and the comparison is of shaping alone.
/// </remarks>
[Config(typeof(Config.Gate))]
public class ShapeTextBenchmark : IDisposable
{
    private const int FontSize = 16;

    private string text = string.Empty;
    private TextDirection direction;
    private Font? font;
    private readonly TextShapingBuffer shapingBuffer = new();
    private Blob? blob;
    private HBFace? face;
    private HBFont? hbFont;
    private HBBuffer buffer = null!;
    private HBScript? hbScriptOverride;

    /// <summary>
    /// Gets or sets the text scenario used by the benchmark.
    /// </summary>
    [ParamsAllValues]
    public ShapeTextBenchmarkScenario Scenario { get; set; }

    /// <summary>
    /// Initializes the input text, fonts, and HarfBuzz state for each scenario.
    /// </summary>
    [GlobalSetup]
    public void SetUp()
    {
        string fontPath;
        switch (this.Scenario)
        {
            case ShapeTextBenchmarkScenario.Latin:
                fontPath = GetRepositoryPath("tests/Fonts/OpenSans-Regular.ttf");
                this.text = "The quick brown fox jumps over the lazy dog; fifty fluffy waffles.";
                this.direction = TextDirection.LeftToRight;
                break;
            case ShapeTextBenchmarkScenario.Arabic:
                fontPath = GetRepositoryPath("tests/Fonts/Dubai-Regular.ttf");
                this.text = "سلام عليكم ورحمة الله وبركاته لا إله إلا الله";
                this.direction = TextDirection.RightToLeft;
                break;
            case ShapeTextBenchmarkScenario.Hebrew:
                fontPath = GetRepositoryPath("tests/Fonts/NotoSansHebrew-Regular.ttf");
                this.text = "שָׁלוֹם עוֹלָם; בְּרֵאשִׁית בָּרָא אֱלֹהִים";
                this.direction = TextDirection.RightToLeft;
                break;
            case ShapeTextBenchmarkScenario.Thai:
                fontPath = GetRepositoryPath("tests/Fonts/Sarabun-Regular.ttf");
                this.text = "ภาษาไทยเป็นภาษาที่มีวรรณยุกต์และสระกำกับ";
                this.direction = TextDirection.LeftToRight;
                break;
            case ShapeTextBenchmarkScenario.Hangul:
                fontPath = GetRepositoryPath("tests/Fonts/NanumGothicCoding-Regular.ttf");
                this.text = "한글을 사랑합니다 대한민국 한글";
                this.direction = TextDirection.LeftToRight;
                break;
            case ShapeTextBenchmarkScenario.Devanagari:
                fontPath = GetRepositoryPath("tests/Fonts/NotoSansDevanagari-Regular.ttf");
                this.text = "क्षत्रिय द्वारा प्रकृति की रक्षा कर्तव्य है";
                this.direction = TextDirection.LeftToRight;
                break;
            case ShapeTextBenchmarkScenario.Khmer:
                fontPath = GetRepositoryPath("tests/Fonts/NotoSansKhmer-Regular.ttf");
                this.text = "ភាសាខ្មែរមានស្រៈ និងជើងព្យញ្ជនៈច្រើន";
                this.direction = TextDirection.LeftToRight;
                break;
            case ShapeTextBenchmarkScenario.Myanmar:
                fontPath = GetRepositoryPath("tests/Fonts/NotoSansMyanmar-Regular.ttf");
                this.text = "မြန်မာစာ စမ်းသပ်မှု။ ဗျည်းပေါင်းစုံ က ခ ဂ ဃ င စ ဆ ည့် န့်";
                this.direction = TextDirection.LeftToRight;
                break;
            case ShapeTextBenchmarkScenario.MyanmarZawgyi:
                fontPath = GetRepositoryPath("tests/harfbuzz/test/shape/data/in-house/fonts/ab14b4eb9d7a67e293f51d30d719add06c9d6e06.ttf");
                this.text = "\u1000\u103A\u1004\u1037\u1039\u1041 \u1000\u103A\u1004\u1037\u1039\u1041 \u1000\u103A\u1004\u1037\u1039\u1041 \u1000\u103A\u1004\u1037\u1039\u1041";
                this.direction = TextDirection.LeftToRight;
                this.shapingBuffer.Script = ScriptClass.MyanmarZawgyi;
                this.hbScriptOverride = HBScript.Parse("Qaag");
                break;
            case ShapeTextBenchmarkScenario.Balinese:
                fontPath = GetRepositoryPath("tests/Fonts/NotoSansBalinese-Regular.ttf");
                this.text = "\u1B13\u1B44\u1B13\u1B3C \u1B1B\u1B44\u1B13\u1B38\u1B00 \u1B13\u1B44\u1B31\u1B3A \u1B13\u1B36\u1B3E";
                this.direction = TextDirection.LeftToRight;
                break;
            default:
                throw new InvalidOperationException($"Unknown shaping benchmark scenario '{this.Scenario}'.");
        }

        this.font = new FontCollection().Add(fontPath).CreateFont(FontSize);

        this.blob = Blob.FromFile(fontPath);
        this.face = new HBFace(this.blob, 0);
        this.hbFont = new HBFont(this.face);
        this.hbFont.SetFunctionsOpenType();
        this.hbFont.SetScale(FontSize, FontSize);
        this.buffer = new HBBuffer();
    }

    /// <summary>
    /// Shapes the text with <see cref="TextShaper"/>, reusing one buffer as production
    /// text stacks do, and sums the resulting advances.
    /// </summary>
    /// <returns>The advance sum, returned so the shaped stream is fully consumed.</returns>
    [Benchmark]
    public float ShapeSixLaborsFonts()
    {
        // The bang stands in for a guard clause on purpose. BenchmarkDotNet runs
        // [GlobalSetup] before any iteration, so the field is always assigned by the
        // time this is measured; the compiler cannot see that, and a guard here would
        // put a branch inside the measured region that the HarfBuzz side does not
        // pay, leaving the two sides doing different work.
        this.shapingBuffer.Add(this.text);
        this.shapingBuffer.TextDirection = this.direction;
        TextShaper.ShapeRun(this.font!, this.shapingBuffer);

        ReadOnlySpan<ShapedGlyph> glyphs = this.shapingBuffer.Glyphs;
        float advanceSum = 0;
        for (int i = 0; i < glyphs.Length; i++)
        {
            advanceSum += glyphs[i].AdvanceWidth;
        }

        return advanceSum;
    }

    /// <summary>
    /// Shapes the text with HarfBuzz, reusing one buffer as production text stacks do,
    /// and sums the resulting advances.
    /// </summary>
    /// <returns>The advance sum, returned so the shaped stream is fully consumed.</returns>
    [Benchmark(Baseline = true)]
    public float ShapeHarfBuzz()
    {
        this.buffer.Reset();
        this.buffer.AddUtf16(this.text);
        if (this.hbScriptOverride.HasValue)
        {
            // Zawgyi and ordinary Myanmar use the same Unicode characters, so the
            // explicit Qaag property is the only way to select the Zawgyi shaper.
            this.buffer.Script = this.hbScriptOverride.Value;
        }

        this.buffer.GuessSegmentProperties();
        this.hbFont!.Shape(this.buffer);

        ReadOnlySpan<GlyphPosition> positions = this.buffer.GetGlyphPositionSpan();

        float advanceSum = 0;
        for (int i = 0; i < positions.Length; i++)
        {
            advanceSum += positions[i].XAdvance;
        }

        return advanceSum;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.buffer?.Dispose();
        this.hbFont?.Dispose();
        this.face?.Dispose();
        this.blob?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Resolves a repository-relative path by walking up from the benchmark output
    /// directory to the repository root.
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
