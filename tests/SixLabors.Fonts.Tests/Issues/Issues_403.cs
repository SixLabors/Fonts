// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_403
{
    // https://github.com/SixLabors/Fonts/discussions/403
    [Fact]
    public void DoesNotKernIncorrectly()
    {
        Font font = TestFonts.GetFont(TestFonts.KellySlabFile, 2048);

        TextOptions options = new(font) { KerningMode = KerningMode.Auto };
        FontRectangle kerned = TextMeasurer.MeasureBounds("AY", options);

        options.KerningMode = KerningMode.None;

        FontRectangle unkerned = TextMeasurer.MeasureBounds("AY", options);

        Assert.Equal(kerned.Left, unkerned.Left);
        Assert.Equal(kerned.Top, unkerned.Top);
        Assert.True(kerned.Right < unkerned.Right);
        Assert.Equal(kerned.Bottom, unkerned.Bottom);
    }
}
