// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GSub
{
    /// <content>
    /// Tests adapted from <see href="https://github.com/foliojs/fontkit/blob/417af0c79c5664271a07a783574ec7fac7ebad0c/test/shaping.js"/>.
    /// Text has been converted to codepoint representation via https://www.corvelsoftware.co.uk/crowbar/ as Visual Studio won't
    /// display the glyphs without additional language packs. All output has been visually checked.
    /// </content>
    public partial class GSubTableTests
    {
        private readonly Font kannadaFontTTF = CreateKannadaFont();

        private static Font CreateKannadaFont()
        {
            var collection = new FontCollection();
            FontFamily family = collection.Add(TestFonts.NotoSerifKannadaRegular);
            return family.CreateFont(12);
        }

        [Theory]
        [InlineData("\u0cb2\u0ccd\u0cb2\u0cbf", new int[] { 250, 126 })]
        [InlineData("\u0c9f\u0ccd\u0cb8\u0ccd", new int[] { 194, 130 })]
        [InlineData("\u0cb3\u0cbf", new int[] { 257 })]
        [InlineData("\u0ca1\u0cbf", new int[] { 235 })]
        [InlineData("\u0cae\u0cc6", new int[] { 295 })]
        [InlineData("\u0cb0\u0cbf", new int[] { 249 })]
        [InlineData("\u0c96\u0ccd\u0caf\u0cc6", new int[] { 272, 124 })]
        [InlineData("\u0cab\u0ccd\u0cb0\u0cbf", new int[] { 244, 125 })]
        [InlineData("\u0ca8\u0cc6", new int[] { 290 })]
        [InlineData("\u0c97\u0cbf", new int[] { 225 })]
        [InlineData("\u0cb7\u0ccd\u0c9f\u0cbf", new int[] { 253, 109 })]
        [InlineData("\u0caf\u0cbf\u0c82", new int[] { 248, 73 })]
        [InlineData("\u0c9a\u0cc0", new int[] { 228, 35 })]
        [InlineData("\u0ca8\u0cbf", new int[] { 242 })]
        [InlineData("\u0c97\u0ccd\u0cb2\u0cbf", new int[] { 225, 126 })]
        [InlineData("\u0cb7\u0cbf", new int[] { 253 })]
        [InlineData("\u0c97\u0cc6", new int[] { 273 })]
        [InlineData("\u0ca6\u0ccd\u0cb5\u0cbf", new int[] { 240, 127 })]
        [InlineData("\u0ca4\u0cc0", new int[] { 238, 35 })]
        [InlineData("\u0cae\u0cbf", new int[] { 247 })]
        [InlineData("\u0cb2\u0cbf", new int[] { 250 })]
        [InlineData("\u0ca8\u0ccd", new int[] { 203 })]
        [InlineData("\u0cac\u0cbf", new int[] { 245 })]
        [InlineData("\u0ca8\u0ccd\u0ca8\u0cbf\u0c82", new int[] { 242, 118, 73 })]
        [InlineData("\u0ca7\u0cbf", new int[] { 241 })]
        [InlineData("\u0caa\u0ccc", new int[] { 168, 34 })]
        [InlineData("\u0cb5\u0cbf\u0c82", new int[] { 251, 73 })]
        [InlineData("\u0c9f\u0cbf", new int[] { 233 })]
        public void CanShapeKannadaText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.kannadaFontTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }
    }
}
