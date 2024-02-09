// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;
public class Issues_375
{
    [Fact]
    public void DiacriticsAreMeasuredCorrectly()
    {
        Font font = new FontCollection().Add(TestFonts.PermanentMarkerRegularFile).CreateFont(142);

        TextOptions options = new(font);

        FontRectangle bounds = TextMeasurer.MeasureBounds("È", options);
        FontRectangle size = TextMeasurer.MeasureSize("È", options);
        FontRectangle advance = TextMeasurer.MeasureAdvance("È", options);

        Font fontWoff = new FontCollection().Add(TestFonts.PermanentMarkerRegularWoff2File).CreateFont(142);

        TextOptions optionsWoff = new(fontWoff);

        FontRectangle boundsWoff = TextMeasurer.MeasureBounds("È", optionsWoff);
        FontRectangle sizeWoff = TextMeasurer.MeasureSize("È", optionsWoff);
        FontRectangle advanceWoff = TextMeasurer.MeasureAdvance("È", optionsWoff);

        Assert.Equal(bounds, boundsWoff);
        Assert.Equal(size, sizeWoff);
        Assert.Equal(advance, advanceWoff);
    }
}
