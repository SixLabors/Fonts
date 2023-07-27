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
        public enum KannadaFont
        {
            Serif,
            Sans
        }

        private static readonly Font KannadaNotoSerifTTF = CreateFont(TestFonts.NotoSerifKannadaRegular);
        private static readonly Font KannadaNotoSansTTF = CreateFont(TestFonts.NotoSansKannadaRegular);

        private static Font CreateFont(string testFont)
        {
            var collection = new FontCollection();
            FontFamily family = collection.Add(testFont);
            return family.CreateFont(12);
        }

        [Theory]

        // SHKNDA-1
        [InlineData(KannadaFont.Serif, "\u0cb2\u0ccd\u0cb2\u0cbf", new int[] { 250, 126 })]
        [InlineData(KannadaFont.Serif, "\u0c9f\u0ccd\u0cb8\u0ccd", new int[] { 194, 130 })]
        [InlineData(KannadaFont.Serif, "\u0cb3\u0cbf", new int[] { 257 })]
        [InlineData(KannadaFont.Serif, "\u0ca1\u0cbf", new int[] { 235 })]
        [InlineData(KannadaFont.Serif, "\u0cae\u0cc6", new int[] { 295 })]
        [InlineData(KannadaFont.Serif, "\u0cb0\u0cbf", new int[] { 249 })]
        [InlineData(KannadaFont.Serif, "\u0c96\u0ccd\u0caf\u0cc6", new int[] { 272, 124 })]
        [InlineData(KannadaFont.Serif, "\u0cab\u0ccd\u0cb0\u0cbf", new int[] { 244, 125 })]
        [InlineData(KannadaFont.Serif, "\u0ca8\u0cc6", new int[] { 290 })]
        [InlineData(KannadaFont.Serif, "\u0c97\u0cbf", new int[] { 225 })]
        [InlineData(KannadaFont.Serif, "\u0cb7\u0ccd\u0c9f\u0cbf", new int[] { 253, 109 })]
        [InlineData(KannadaFont.Serif, "\u0caf\u0cbf\u0c82", new int[] { 248, 73 })]
        [InlineData(KannadaFont.Serif, "\u0c9a\u0cc0", new int[] { 228, 35 })]
        [InlineData(KannadaFont.Serif, "\u0ca8\u0cbf", new int[] { 242 })]
        [InlineData(KannadaFont.Serif, "\u0c97\u0ccd\u0cb2\u0cbf", new int[] { 225, 126 })]
        [InlineData(KannadaFont.Serif, "\u0cb7\u0cbf", new int[] { 253 })]
        [InlineData(KannadaFont.Serif, "\u0c97\u0cc6", new int[] { 273 })]
        [InlineData(KannadaFont.Serif, "\u0ca6\u0ccd\u0cb5\u0cbf", new int[] { 240, 127 })]
        [InlineData(KannadaFont.Serif, "\u0ca4\u0cc0", new int[] { 238, 35 })]
        [InlineData(KannadaFont.Serif, "\u0cae\u0cbf", new int[] { 247 })]
        [InlineData(KannadaFont.Serif, "\u0cb2\u0cbf", new int[] { 250 })]
        [InlineData(KannadaFont.Serif, "\u0ca8\u0ccd", new int[] { 203 })]
        [InlineData(KannadaFont.Serif, "\u0cac\u0cbf", new int[] { 245 })]
        [InlineData(KannadaFont.Serif, "\u0ca8\u0ccd\u0ca8\u0cbf\u0c82", new int[] { 242, 118, 73 })]
        [InlineData(KannadaFont.Serif, "\u0ca7\u0cbf", new int[] { 241 })]
        [InlineData(KannadaFont.Serif, "\u0caa\u0ccc", new int[] { 168, 34 })]
        [InlineData(KannadaFont.Serif, "\u0cb5\u0cbf\u0c82", new int[] { 251, 73 })]
        [InlineData(KannadaFont.Serif, "\u0c9f\u0cbf", new int[] { 233 })]

        // SHKNDA-2
        [InlineData(KannadaFont.Sans, "\u0ca8\u0ccd\u0ca8\u0cbe", new int[] { 150, 57, 116 })]
        [InlineData(KannadaFont.Sans, "\u0ca4\u0ccd\u0ca4\u0cbe", new int[] { 146, 57, 112 })]
        [InlineData(KannadaFont.Sans, "\u0c9f\u0ccd\u0c9f\u0cbe", new int[] { 141, 57, 107 })]
        [InlineData(KannadaFont.Sans, "\u0ca1\u0ccb\u0c82\u0c97\u0cbf", new int[] { 249, 61, 71, 4, 207 })]
        [InlineData(KannadaFont.Sans, "\u0c9c\u0cbf\u0cbc\u0cd5\u0cac\u0cc6\u0ca8\u0ccd", new int[] { 211, 55, 71, 259, 186 })]
        [InlineData(KannadaFont.Sans, "\u0c9c\u0cbe\u0cbc\u0c95\u0cbf\u0cb0\u0ccd", new int[] { 139, 57, 55, 205, 193 })]
        [InlineData(KannadaFont.Sans, "\u0c87\u0ca8\u0ccd\u0cab\u0ccd\u0cb2\u0cc6\u0c95\u0ccd\u0cb7\u0ca8\u0cb2\u0ccd", new int[] { 8, 256, 118, 335, 282, 39, 195 })]
        [InlineData(KannadaFont.Sans, "\u0c87\u0ca8\u0ccd\u0cab\u0ccd\u0cb2\u0cc6\u0c95\u0ccd\u0cb7\u0ca8\u0ccd", new int[] { 8, 256, 118, 335, 282, 186 })]
        [InlineData(KannadaFont.Sans, "\u0ca6\u0c9f\u0ccd\u0cb8\u0ccd", new int[] { 37, 177, 130 })]
        [InlineData(KannadaFont.Sans, "\u0c8e\u0c95\u0ccd\u0cb8\u0ccd", new int[] { 14, 167, 130 })]
        [InlineData(KannadaFont.Sans, "\u0cae\u0cbe\u0cb0\u0ccd\u0c9a\u0ccd", new int[] { 155, 57, 172, 94 })]
        [InlineData(KannadaFont.Sans, "\u0c9f\u0cc6\u0c95\u0ccd\u0cb8\u0ccd\u0c9f\u0ccd", new int[] { 247, 167, 130, 317 })]
        [InlineData(KannadaFont.Sans, "\u0cac\u0cc1\u0c95\u0ccd\u0cb8\u0ccd", new int[] { 42, 60, 167, 130 })]
        [InlineData(KannadaFont.Sans, "\u0cb8\u0cbe\u0cab\u0ccd\u0c9f\u0ccd", new int[] { 163, 57, 188, 107 })]
        [InlineData(KannadaFont.Sans, "\u0c9c\u0cb8\u0ccd\u0c9f\u0ccd", new int[] { 27, 200, 107 })]

        // SHKNDA-3
        [InlineData(KannadaFont.Sans, "\u0c95\u0ccb\u0c82", new int[] { 239, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c96\u0ccb\u0c82", new int[] { 240, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c97\u0ccb\u0c82", new int[] { 241, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c98\u0ccb\u0c82", new int[] { 242, 279, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c99\u0ccb\u0c82", new int[] { 24, 67, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c9a\u0ccb\u0c82", new int[] { 243, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c9b\u0ccb\u0c82", new int[] { 244, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c9c\u0ccb\u0c82", new int[] { 245, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c9d\u0ccb\u0c82", new int[] { 246, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c9e\u0ccb\u0c82", new int[] { 29, 67, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0c9f\u0ccb\u0c82", new int[] { 247, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca0\u0ccb\u0c82", new int[] { 248, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca1\u0ccb\u0c82", new int[] { 249, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca2\u0ccb\u0c82", new int[] { 250, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca3\u0ccb\u0c82", new int[] { 251, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca4\u0ccb\u0c82", new int[] { 252, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca5\u0ccb\u0c82", new int[] { 253, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca6\u0ccb\u0c82", new int[] { 254, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca7\u0ccb\u0c82", new int[] { 255, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0ca8\u0ccb\u0c82", new int[] { 256, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0caa\u0ccb\u0c82", new int[] { 257, 275, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cab\u0ccb\u0c82", new int[] { 258, 277, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cac\u0ccb\u0c82", new int[] { 259, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cad\u0ccb\u0c82", new int[] { 260, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cae\u0ccb\u0c82", new int[] { 280, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0caf\u0ccb\u0c82", new int[] { 281, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cb0\u0ccb\u0c82", new int[] { 263, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cb1\u0ccb\u0c82", new int[] { 47, 67, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cb2\u0ccb\u0c82", new int[] { 264, 61, 71, 4 })]
        [InlineData(KannadaFont.Sans, "\u0cb5\u0ccb\u0c82", new int[] { 266, 275, 71, 4 })]

        // Harfbuzz replaces the default ignorable with id 91 with a space and sets the advance to 0. We skip it entirely on rendering.
        [InlineData(KannadaFont.Sans, "\u0c86\u0ccd\u0caf\u0c95\u0ccd\u0cb7\u0cbf\u0cb8\u0ccd\u200c", new int[] { 7, 122, 285, 200 })]
        public void CanShapeKannadaText(KannadaFont font, string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(font == KannadaFont.Serif ? KannadaNotoSerifTTF : KannadaNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }
    }
}
