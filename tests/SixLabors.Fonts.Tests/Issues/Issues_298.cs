// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_298
{
    [Fact]
    public void DoesNotThrowOutOfBounds()
    {
        const string content = "Please enter the text";

        Font font = TestFonts.GetFont(TestFonts.Issues.Issue298, 16);

        TextOptions renderOptions = new(font)
        {
            Dpi = 96,
            WrappingLength = 0f,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            KerningMode = KerningMode.Auto
        };

        FontRectangle bounds = TextMeasurer.MeasureBounds(content.AsSpan(), renderOptions);
        Assert.NotEqual(default, bounds);
    }
}
