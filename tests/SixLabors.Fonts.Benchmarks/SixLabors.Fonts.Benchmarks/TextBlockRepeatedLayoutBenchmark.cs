// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using BenchmarkDotNet.Attributes;

namespace SixLabors.Fonts.Benchmarks;

/// <summary>
/// Defines the text shape used by <see cref="TextBlockRepeatedLayoutBenchmark"/>.
/// </summary>
public enum TextBlockBenchmarkScenario
{
    /// <summary>
    /// Latin paragraphs with normal wrapping opportunities.
    /// </summary>
    LatinParagraphs,

    /// <summary>
    /// Mixed left-to-right and right-to-left text with numbers.
    /// </summary>
    MixedBidi
}

/// <summary>
/// Compares repeated layout at several wrapping lengths with and without prepared text reuse.
/// </summary>
[MemoryDiagnoser]
[MediumRunJob]
public class TextBlockRepeatedLayoutBenchmark
{
    private readonly float[] wrappingLengths = [240, 320, 400, 520];
    private string text = string.Empty;
    private TextOptions textMeasurerOptions = null!;
    private TextBlock textBlock = null!;

    /// <summary>
    /// Gets or sets the text scenario used by the benchmark.
    /// </summary>
    [Params(TextBlockBenchmarkScenario.LatinParagraphs, TextBlockBenchmarkScenario.MixedBidi)]
    public TextBlockBenchmarkScenario Scenario { get; set; }

    /// <summary>
    /// Initializes the input text, options, and prepared block for each scenario.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        Font font = SystemFonts.Get("Arial").CreateFont(16, FontStyle.Regular);
        this.text = CreateText(this.Scenario);
        this.textMeasurerOptions = new TextOptions(font) { WrappingLength = -1 };

        TextOptions textBlockOptions = new(font) { WrappingLength = -1 };
        this.textBlock = new TextBlock(this.text, textBlockOptions);
    }

    /// <summary>
    /// Measures each wrapping length through the existing static measurement API.
    /// </summary>
    /// <returns>An aggregate value that keeps the measured work observable.</returns>
    [Benchmark(Baseline = true)]
    public float TextMeasurerMeasureSizeRepeatedWrappingLengths()
    {
        float result = 0;
        for (int i = 0; i < this.wrappingLengths.Length; i++)
        {
            this.textMeasurerOptions.WrappingLength = this.wrappingLengths[i];
            FontRectangle size = TextMeasurer.MeasureSize(this.text, this.textMeasurerOptions);
            result += size.Width + size.Height;
        }

        return result;
    }

    /// <summary>
    /// Measures each wrapping length through one prepared <see cref="TextBlock"/>.
    /// </summary>
    /// <returns>An aggregate value that keeps the measured work observable.</returns>
    [Benchmark]
    public float TextBlockMeasureSizeRepeatedWrappingLengths()
    {
        float result = 0;
        for (int i = 0; i < this.wrappingLengths.Length; i++)
        {
            FontRectangle size = this.textBlock.MeasureSize(this.wrappingLengths[i]);
            result += size.Width + size.Height;
        }

        return result;
    }

    private static string CreateText(TextBlockBenchmarkScenario scenario)
        => scenario switch
        {
            TextBlockBenchmarkScenario.LatinParagraphs =>
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. "
                + "Suspendisse lectus tortor, dignissim sit amet, adipiscing nec, ultricies sed, dolor. "
                + "Cras elementum ultrices diam. Maecenas ligula massa, varius a, semper congue, euismod non, mi.\n\n"
                + "Praesent blandit odio eu enim. Pellentesque sed dui ut augue blandit sodales. "
                + "Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae.",

            // Hebrew and Arabic runs exercise bidi resolution and per-line visual reordering.
            _ =>
                "The paragraph begins in English, then says שלום to the reader, adds the number 456, "
                + "and continues with Arabic مرحبا before returning to Latin text.\n\n"
                + "A second paragraph mentions דוד and فاطمة so line breaking crosses mixed scripts naturally."
        };
}
