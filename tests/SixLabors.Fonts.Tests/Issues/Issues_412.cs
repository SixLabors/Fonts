// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_412
{
    [Fact]
    public void ShouldCreateCorrectTextRunCount()
    {
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.OpenSansFile);
        Font font = family.CreateFont(24);

        TextOptions options = new(font)
        {
            TextRuns = new[]
            {
                new TextRun { Start = 0, End = 4 }
            }
        };

        IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns("abcde", options);
        Assert.Equal(2, runs.Count);

        TextRun run = runs[0];
        Assert.Equal(0, run.Start);
        Assert.Equal(4, run.End);

        run = runs[1];
        Assert.Equal(4, run.Start);
        Assert.Equal(5, run.End);
    }
}
