// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.Gsub
{
    public class GSubTableTests
    {
        // TODO: add all other arabic letters
        [Fact]
        public void RenderArabicCharacters_WithIsolatedForm_Works()
        {
            // arrange
            Font arabicFont = new FontCollection().Add(TestFonts.ArabicFontFile).CreateFont(8);
            string testStr = "ب";
            var rendererTtf = new ColorGlyphRenderer();
            bool applyKerning = true;
            int expectedGlyphIndex = 157;

            // act
            TextRenderer.RenderTextTo(rendererTtf, testStr, new RendererOptions(arabicFont)
            {
                ApplyKerning = applyKerning,
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(rendererTtf.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }
    }
}
