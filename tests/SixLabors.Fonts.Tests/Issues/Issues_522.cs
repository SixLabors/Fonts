// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_522
{
    [Fact]
    public void LineBreakEnumerator_DoesNotBreakWithinAlphaNumericRunAfterColon()
    {
        string text = "số khung: RRKWCH1UM7XJ00693";
        List<LineBreak> breaks = [.. new LineBreakEnumerator(text.AsSpan())];

        Assert.Collection(
            breaks,
            x =>
            {
                Assert.Equal(2, x.PositionMeasure);
                Assert.Equal(3, x.PositionWrap);
                Assert.False(x.Required);
            },
            x =>
            {
                Assert.Equal(9, x.PositionMeasure);
                Assert.Equal(10, x.PositionWrap);
                Assert.False(x.Required);
            },
            x =>
            {
                Assert.Equal(27, x.PositionMeasure);
                Assert.Equal(27, x.PositionWrap);
                Assert.False(x.Required);
            });
    }
}
