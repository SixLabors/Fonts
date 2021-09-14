// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.Gsub
{
    public class GSubTableTests
    {
        [Theory]
        [InlineData("ا", 139)]
        [InlineData("ب", 157)]
        [InlineData("پ", 162)]
        [InlineData("ت", 167)]
        [InlineData("ث", 172)]
        [InlineData("ج", 181)]
        [InlineData("چ", 185)]
        [InlineData("ح", 189)]
        [InlineData("خ", 193)]
        [InlineData("د", 197)]
        [InlineData("ز", 207)]
        [InlineData("ر", 203)]
        [InlineData("س", 215)]
        [InlineData("ش", 219)]
        [InlineData("ص", 223)]
        [InlineData("ض", 227)]
        [InlineData("ط", 231)]
        [InlineData("ع", 239)]
        [InlineData("غ", 243)]
        [InlineData("ف", 247)]
        [InlineData("ق", 264)]
        [InlineData("ک", 273)]
        [InlineData("گ", 277)]
        [InlineData("ل", 281)]
        [InlineData("م", 287)]
        [InlineData("ن", 291)]
        [InlineData("و", 316)]
        [InlineData("ه", 298)]
        [InlineData("ی", 335)]
        public void RenderArabicCharacters_WithIsolatedForm_Works(string testStr, int expectedGlyphIndex)
        {
            // arrange
            Font arabicFont = new FontCollection().Add(TestFonts.ArabicFontFile).CreateFont(8);
            var renderer = new ColorGlyphRenderer();

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(arabicFont)
            {
                ApplyKerning = true
            });

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        [Fact]
        public void SingleSubstitution_Works()
        {
            // arrange
            Font gsubFont = new FontCollection().Add(TestFonts.GsubTestFontFile).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "A";
            int expectedGlyphIndex = 38; // we expect A to be mapped to B.

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gsubFont)
            {
                ApplyKerning = true
            });

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        [Fact]
        public void MultipleSubstitution_Works()
        {
            // arrange
            Font gsubFont = new FontCollection().Add(TestFonts.GsubTestFontFile).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "C";
            int expectedGlyphIndex = 40; // we expect C to be mapped to D.

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gsubFont)
            {
                ApplyKerning = true
            });

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        [Fact]
        public void AlternateSubstitution_Works()
        {
            // arrange
            Font gsubFont = new FontCollection().Add(TestFonts.GsubTestFontFile).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "E";
            int expectedGlyphIndex = 42; // we expect E to be mapped to F.

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gsubFont)
            {
                ApplyKerning = true
            });

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }
    }
}
