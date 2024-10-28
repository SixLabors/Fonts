// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_269
{
    private static readonly ApproximateFloatComparer Comparer = new(.1F);

    [Fact]
    public void CorrectlySetsMetricsForFontsNotAdheringToSpec()
    {
        // AliceFrancesHMK has invalid subtables.
        Font font = new FontCollection().Add(TestFonts.AliceFrancesHMKRegularFile).CreateFont(25);

        FontRectangle size = TextMeasurer.MeasureSize("H", new TextOptions(font));
        Assert.Equal(30.6000004F, size.Width, Comparer);
        Assert.Equal(24.75F, size.Height, Comparer);
    }
}
