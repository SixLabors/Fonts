// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_39
{
    [Fact]
    public void RenderingEmptyString_DoesNotThrow()
    {
        Font font = CreateFont("\t x");

        GlyphRenderer r = new();

        new TextRenderer(r).RenderText(string.Empty, new TextOptions(new Font(font, 30)));
    }

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = (IFontMetricsCollection)new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}
