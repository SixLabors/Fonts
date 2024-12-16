// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_431
{
    [Fact]
    public void ShouldNotInsertExtraLineBreaks()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            const string text = "- Lorem ipsullll\ndolor sit amet\n-consectetur elit";

            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(4, lineCount);

            IReadOnlyList<GlyphLayout> layout = TextLayout.GenerateLayout(text, options);
            Assert.Equal(46, layout.Count);
        }
    }
}
