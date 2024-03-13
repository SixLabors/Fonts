// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if OS_WINDOWS
namespace SixLabors.Fonts.Tests.Issues;
public class Issues_390
{
    [Fact]
    public void UniversalShaper_NullReferenceException()
    {
        const string s = " 꿹ꓴ/ꥀ냘";
        FontFamily fontFamily = SystemFonts.Get("Arial");
        Font font = new(fontFamily, 10f);
        _ = TextMeasurer.MeasureBounds(s, new TextOptions(font));
    }
}
#endif
