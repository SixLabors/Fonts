// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Text;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class HintingTests
{
    public static TheoryData<string, string> HintingTestData { get; } = new()
    {
        // Arial and Tahoma are legacy TrueType fonts whose bytecode was written
        // for pre-ClearType rasterizers. Under a v40-style interpreter (vertical
        // hinting only, no horizontal grid-fitting, no backward-compatibility
        // constraints), both fonts generally render cleanly, but small differences
        // in horizontal features, joins and bar heights can occur at low ppem.
        // This behaviour matches FreeType v40 expectations for older fonts that
        // relied on full-axis grid fitting in legacy engines.
        { TestFonts.Arial, nameof(TestFonts.Arial) },
        { TestFonts.Tahoma, nameof(TestFonts.Tahoma) },

        // Modern ClearType-hinted OpenType fonts (for example Open Sans) are
        // authored for the same vertical-dominant model used by v40 and therefore
        // render consistently and predictably under these semantics.
        { TestFonts.OpenSansFile, nameof(TestFonts.OpenSansFile) },
    };

    [Theory]
    [MemberData(nameof(HintingTestData))]
    public void Test_Hinting_Robustness(string path, string name)
    {
        const string copy = "The quick brown fox jumps over the lazy dog.";
        FontCollection collection = new();
        FontFamily family = collection.Add(path);
        Font font = family.CreateFont(5);

        int fontSize = 5;
        int start = 0;
        int end = copy.GetGraphemeCount();
        int length = (end - start) + 1; // include the line ending.
        List<TextRun> textRuns = [];
        StringBuilder stringBuilder = new();
        while (fontSize < 64)
        {
            stringBuilder.AppendLine(copy);
            TextRun run = new()
            {
                Start = start,
                End = end,
                Font = new Font(font, fontSize),
            };

            textRuns.Add(run);
            fontSize += 1;
            start += length;
            end += length;
        }

        string text = stringBuilder.ToString();

        TextOptions options = new(font)
        {
            TextRuns = textRuns,
            HintingMode = HintingMode.Standard,
        };

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            properties: name);
    }

    // The TrueType bytecode interpreter is pooled and reused across renders for the same
    // font. When a pooled interpreter is reused for a different pixel size it re-runs the
    // font's prep (CVT) program, which must execute from the same pristine state as a freshly
    // created interpreter. If transient interpreter state (twilight zone, storage, rounding
    // state, zone pointers, ...) is not reset first, the prep result — and therefore the
    // hinted glyph outline — depends on which sizes were rendered previously on that
    // interpreter. Because interpreters are shared through a pool, that made hinting output
    // non-deterministic when a single font family was rendered concurrently from multiple
    // threads
    [Fact]
    public void Hinting_OutputIsIndependentOfPreviouslyRenderedSizes()
    {
        const string text = "The quick brown fox 12345";
        const float dpi = 150F;
        const float targetSize = 7F;
        const float otherSize = 12F;

        static List<Vector2> RenderControlPoints(string text, float size, float dpi, float? warmUpSize)
        {
            FontCollection collection = new();
            FontFamily family = collection.Add(TestFonts.Arial);

            if (warmUpSize is { } w)
            {
                RenderTo(family, text, w, dpi, new GlyphRenderer());
            }

            GlyphRenderer renderer = new();
            RenderTo(family, text, size, dpi, renderer);
            return renderer.ControlPoints;
        }

        static void RenderTo(FontFamily family, string text, float size, float dpi, GlyphRenderer renderer)
        {
            Font font = family.CreateFont(size);
            TextOptions options = new(font)
            {
                Dpi = dpi,
                HintingMode = HintingMode.Standard,
            };

            TextRenderer.RenderTextTo(renderer, text, options);
        }

        // Render the target size on a font whose interpreter has processed nothing else.
        List<Vector2> reference = RenderControlPoints(text, targetSize, dpi, warmUpSize: null);

        // Render the same target size, but on a font whose pooled interpreter has already
        // processed a different size. With a correct per-size reset this is byte-for-byte equal.
        List<Vector2> afterOtherSize = RenderControlPoints(text, targetSize, dpi, warmUpSize: otherSize);

        Assert.Equal(reference, afterOtherSize);
    }
}
