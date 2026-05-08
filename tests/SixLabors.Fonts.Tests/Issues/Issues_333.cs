// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_333
{
    [Fact]
    public void DoesNotThrowMissingTableException()
    {
        const string text = "文字測試文字測試文字測試文字測試文字測試";
        Font font = TestFonts.GetFont(TestFonts.PMINGLIUFile, 1024);
        TextMeasurer.MeasureBounds(text, new TextOptions(font));
    }
}
