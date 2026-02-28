// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;
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
}
