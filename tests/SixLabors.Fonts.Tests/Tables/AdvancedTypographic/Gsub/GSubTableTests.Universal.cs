// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GSub
{
    /// <content>
    /// Tests adapted from <see href="https://github.com/foliojs/fontkit/blob/417af0c79c5664271a07a783574ec7fac7ebad0c/test/shaping.js"/>.
    /// Text hase been converted to codepoint representation via https://www.corvelsoftware.co.uk/crowbar/
    /// as Visual Studio would not display the glyphs.
    /// All output has been visually checked.
    /// </content>
    public partial class GSubTableTests
    {
        private readonly Font balineseFontTTF = CreateBalineseFont();

        private static Font CreateBalineseFont()
        {
            var collection = new FontCollection();
            FontFamily family = collection.Add(TestFonts.NotoSansBalineseRegular);
            return family.CreateFont(12);
        }

        [Theory]
        [InlineData("\u1b13\u1b38\u1b00", new int[] { 23, 60, 4 })]
        [InlineData("\u1b15\u1b44\u1b16\u1b02", new int[] { 25, 132, 6 })]
        [InlineData("\u1b18\u1b3b", new int[] { 28, 62, 57 })]
        [InlineData("\u1b19\u1b40", new int[] { 66, 29, 57 })]
        [InlineData("\u1b1a\u1b3f", new int[] { 67, 30 })]
        [InlineData("\u1b14\u1b36", new int[] { 24, 58 })]
        [InlineData("\u1b13\u1b44\u1b13\u1b01", new int[] { 23, 129, 5 })]
        [InlineData("\u1b13\u1b44\u1b1b\u1b01", new int[] { 23, 137, 5 })]
        [InlineData("\u1b13\u1b44\u1b26\u1b03", new int[] { 23, 148, 7 })]
        [InlineData("\u1b13\u1b44\u1b13\u1b38", new int[] { 23, 129, 60 })]
        [InlineData("\u1b13\u1b44\u1b13\u1b3c", new int[] { 23, 129, 70, 170 })]
        [InlineData("\u1b13\u1b44\u1b13\u1b3d", new int[] { 23, 129, 70, 170, 57 })]
        [InlineData("\u1b13\u1b3e", new int[] { 66, 23 })]
        public void CanShapeBalineseText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.balineseFontTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }
    }
}
