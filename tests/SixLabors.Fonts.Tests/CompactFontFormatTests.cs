// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class CompactFontFormatTests
    {
        // https://github.com/adobe-fonts/fdarray-test/
        [Theory]
        [InlineData("\u0041", 66)]
        [InlineData("\u211D", 30)]
        [InlineData("\u24EA", 235)]
        public void FDSelectFormat0_Works(string testStr, int expectedGlyphIndex)
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.FDArrayTest257File).CreateFont(8);
            var renderer = new ColorGlyphRenderer();

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphId);
        }
    }
}
