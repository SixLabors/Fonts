// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GSub
{
    /// <content>
    /// Tests adapted from <see href="https://github.com/foliojs/fontkit/blob/417af0c79c5664271a07a783574ec7fac7ebad0c/test/shaping.js"/>
    /// which implement <see href="https://github.com/unicode-org/text-rendering-tests"/>.
    /// Text has been converted to codepoint representation via https://www.corvelsoftware.co.uk/crowbar/ as Visual Studio won't
    /// display the glyphs without additional language packs. All output has been visually checked.
    /// </content>
    public partial class GSubTableTests
    {
        private static readonly Font BalineseFontTTF = CreateFont(TestFonts.NotoSansBalineseRegular);

        [Theory]

        // SHBALI-1
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
        [InlineData("\u1b13\u1b36\u1b3e", new int[] { 23, 58, 66, 128 })]
        [InlineData("\u1b13\u1b38\u1b3e", new int[] { 23, 60, 66, 128 })]
        [InlineData("\u1b13\u1b44\u1b15\u1b3e", new int[] { 66, 23, 131 })]
        [InlineData("\u1b13\u1b40", new int[] { 66, 23, 57 })]
        [InlineData("\u1b13\u1b3e\u1b36", new int[] { 66, 23, 58 })]
        [InlineData("\u1b13\u1b3e\u1b38", new int[] { 66, 23, 60 })]

        // SHBALI-2
        [InlineData("\u1b13\u1b44\u1b27\u1b3e", new int[] { 66, 23, 149 })]
        [InlineData("\u1b13\u1b44\u1b28\u1b3f", new int[] { 67, 23, 150 })]
        [InlineData("\u1b13\u1b44\u1b31\u1b3e", new int[] { 66, 23, 159 })]
        [InlineData("\u1b13\u1b44\u1b32\u1b3e", new int[] { 66, 23, 60, 149 })]
        [InlineData("\u1b13\u1b44\u1b4a\u1b3e", new int[] { 66, 23, 60, 165 })]
        [InlineData("\u1b1b\u1b44\u1b13", new[] { 181, 129 })]
        [InlineData("\u1b1b\u1b44\u1b13\u1b3e", new int[] { 66, 181, 129 })]
        [InlineData("\u1b1b\u1b44\u1b13\u1b38\u1b00", new int[] { 181, 129, 60, 4 })]
        [InlineData("\u1b13\u1b44\u1b1b\u1b39", new int[] { 23, 137, 61 })]
        [InlineData("\u1b13\u1b44\u1b31\u1b3a", new int[] { 23, 159, 62 })]
        [InlineData("\u1b13\u1b44\u1b45\u1b38", new int[] { 23, 162, 60 })]
        public void CanShapeBalineseText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(BalineseFontTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }
    }
}
