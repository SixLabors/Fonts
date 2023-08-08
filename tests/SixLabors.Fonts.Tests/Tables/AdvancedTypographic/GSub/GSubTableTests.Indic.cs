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

        private static readonly FontCollection TestFontCollection = new();
        private static readonly Font KannadaNotoSerifTTF = CreateFont(TestFonts.NotoSerifKannadaRegular);
        private static readonly Font KannadaNotoSansTTF = CreateFont(TestFonts.NotoSansKannadaRegular);
        private static readonly Font TeluguNotoSansTTF = CreateFont(TestFonts.NotoSansTeluguRegular);
        private static readonly Font TamilNotoSansTTF = CreateFont(TestFonts.NotoSansTamilRegular);
        private static readonly Font DevanagariNotoSansTTF = CreateFont(TestFonts.NotoSansDevanagariRegular);
        private static readonly Font BengaliNotoSansTTF = CreateFont(TestFonts.NotoSansBengaliRegular);
        private static readonly Font GurmukhiNotoSansTTF = CreateFont(TestFonts.NotoSansGurmukhiRegular);
        private static readonly Font GujaratiNotoSansTTF = CreateFont(TestFonts.NotoSansGujaratiRegular);
        private static readonly Font MalayalamNotoSansTTF = CreateFont(TestFonts.NotoSansMalayalamRegular);
        private static readonly Font OriyaNotoSansTTF = CreateFont(TestFonts.NotoSansOriyaRegular);
        private static readonly Font KhmerNotoSansTTF = CreateFont(TestFonts.NotoSansKhmerRegular);

        private static Font CreateFont(string testFont)
        {
            FontFamily family = TestFontCollection.Add(testFont);
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

        // Harfbuzz replaces the default ignorable with id 91 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
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

        [Theory]
        [InlineData("\u0c15\u0c46\u0c56", new int[] { 326 })]
        [InlineData("\u0c15\u0c4d", new int[] { 102 })]
        [InlineData("\u0c15\u0c4d\u0c15\u0c48", new int[] { 326, 511 })]
        [InlineData("\u0c15\u0c4d\u0c30", new int[] { 21, 549 })]
        [InlineData("\u0c15\u0c4d\u0c30\u0c3f", new int[] { 174, 549 })]
        [InlineData("\u0c15\u0c4d\u0c30\u0c48", new int[] { 326, 496 })]
        [InlineData("\u0c15\u0c4d\u0c30\u0c4d", new int[] { 102, 549 })]
        [InlineData("\u0c15\u0c4d\u0c30\u0c4d\u0c15", new int[] { 21, 549, 511 })]
        [InlineData("\u0c15\u0c4d\u0c37", new int[] { 101 })]
        [InlineData("\u0c15\u0c4d\u0c37\u0c4d", new int[] { 137 })]
        [InlineData("\u0c15\u0c4d\u0c37\u0c4d\u0c23", new int[] { 21, 605 })]
        [InlineData("\u0c3d\u0c02", new int[] { 56, 5 })]
        public void CanShapeTeluguText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(TeluguNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0ba4\u0bae\u0bbf\u0bb4\u0bcd\u0ba8\u0bbe\u0b9f\u0bc1", new int[] { 25, 29, 42, 94, 26, 41, 112 })]
        [InlineData("\u0b93\u0bb0\u0bcd", new int[] { 16, 90 })]
        [InlineData("\u0b87\u0ba8\u0bcd\u0ba4\u0bbf\u0baf", new int[] { 8, 85, 25, 149, 30 })]
        [InlineData("\u0bae\u0bbe\u0ba8\u0bbf\u0bb2\u0bae\u0bbe\u0b95\u0bc1\u0bae\u0bcd\u002e", new int[] { 29, 41, 26, 149, 33, 29, 41, 101, 88, 164 })]
        [InlineData("\u0ba4\u0bae\u0bbf\u0bb4\u0bcd\u0ba8\u0bbe\u0b9f\u0bc1\u002c", new int[] { 25, 29, 42, 94, 26, 41, 112, 162 })]
        [InlineData("\u0ba4\u0bae\u0bbf\u0bb4\u0b95\u0bae\u0bcd", new int[] { 25, 29, 42, 35, 18, 88 })]
        [InlineData("\u0b8e\u0ba9\u0bcd\u0bb1\u0bc1\u0bae\u0bcd", new int[] { 12, 86, 132, 88 })]
        [InlineData("\u0baa\u0bb0\u0bb5\u0bb2\u0bbe\u0b95", new int[] { 28, 31, 36, 33, 41, 18 })]
        [InlineData("\u0b85\u0bb4\u0bc8\u0b95\u0bcd\u0b95\u0baa\u0bcd\u0baa\u0b9f\u0bc1\u0b95\u0bbf\u0bb1\u0ba4\u0bc1\u002e", new int[] { 6, 48, 35, 77, 18, 87, 28, 112, 18, 149, 32, 116, 164 })]
        [InlineData("\u0b86\u0b99\u0bcd\u0b95\u0bbf\u0bb2\u0ba4\u0bcd\u0ba4\u0bbf\u0bb2\u0bcd", new int[] { 7, 78, 18, 149, 33, 84, 25, 149, 92 })]
        [InlineData("\u0bae\u0bc6\u0b9f\u0bcd\u0bb0\u0bbe\u0bb8\u0bcd", new int[] { 46, 29, 82, 31, 41, 98 })]
        [InlineData("\u0bb8\u0bcd\u0b9f\u0bc7\u0b9f\u0bcd", new int[] { 98, 47, 23, 82 })]
        [InlineData("\u0ba4\u0bae\u0bbf\u0bb4\u0bbf\u0bb2\u0bcd", new int[] { 25, 29, 42, 35, 42, 92 })]
        [InlineData("\u0b9a\u0bc6\u0ba9\u0bcd\u0ba9\u0bc8", new int[] { 46, 20, 86, 48, 27 })]
        [InlineData("\u0bb0\u0bbe\u0b9c\u0bcd\u0b9c\u0bbf\u0baf\u0bae\u0bcd", new int[] { 31, 41, 80, 21, 42, 30, 88 })]
        [InlineData("\u0b85\u0bb4\u0bc8\u0b95\u0bcd\u0b95\u0baa\u0bcd\u0baa\u0bc6\u0bb1\u0bcd\u0bb1\u0ba4\u0bc1\u002e", new int[] { 6, 48, 35, 77, 18, 87, 46, 28, 91, 32, 116, 164 })]
        [InlineData("\u0b87\u0ba4\u0ba9\u0bc8", new int[] { 8, 25, 48, 27 })]
        [InlineData("\u0b8e\u0ba9\u0bcd\u0bb1\u0bc1", new int[] { 12, 86, 132 })]
        [InlineData("\u0bae\u0bbe\u0bb1\u0bcd\u0bb1\u0b95\u0bcd\u0b95\u0bcb\u0bb0\u0bbf", new int[] { 29, 41, 91, 32, 77, 47, 18, 41, 31, 42 })]
        [InlineData("\u0baa\u0bcb\u0bb0\u0bbe\u0b9f\u0bcd\u0b9f\u0b99\u0bcd\u0b95\u0bb3\u0bcd", new int[] { 47, 28, 41, 31, 41, 82, 23, 78, 18, 93 })]
        [InlineData("\u0ba8\u0b9f\u0bc8\u0baa\u0bc6\u0bb1\u0bcd\u0bb1\u0ba9\u002e", new int[] { 26, 48, 23, 46, 28, 91, 32, 27, 164 })]
        [InlineData("\u0b9a\u0b99\u0bcd\u0b95\u0bb0\u0bb2\u0bbf\u0b99\u0bcd\u0b95\u0ba9\u0bbe\u0bb0\u0bcd", new int[] { 20, 78, 18, 31, 134, 78, 18, 27, 41, 90 })]
        [InlineData("\u0b8e\u0ba9\u0bcd\u0baa\u0bb5\u0bb0\u0bcd", new int[] { 12, 86, 28, 36, 90 })]
        [InlineData("\u0ba8\u0bbe\u0b9f\u0bcd\u0b95\u0bb3\u0bcd", new int[] { 26, 41, 82, 18, 93 })]
        [InlineData("\u0b89\u0ba3\u0bcd\u0ba3\u0bbe\u0bb5\u0bbf\u0bb0\u0ba4\u0bae\u0bcd", new int[] { 10, 83, 24, 41, 36, 148, 31, 25, 88 })]
        [InlineData("\u0b87\u0bb0\u0bc1\u0ba8\u0bcd\u0ba4\u0bc1", new int[] { 8, 130, 85, 116 })]
        [InlineData("\u0b89\u0baf\u0bbf\u0bb0\u0bcd\u0ba4\u0bc1\u0bb1\u0ba8\u0bcd\u0ba4\u0bbe\u0bb0\u0bcd\u002e", new int[] { 10, 30, 148, 90, 116, 32, 85, 25, 41, 90, 164 })]
        [InlineData("\u0baa\u0bbf\u0ba9\u0bcd\u0ba9\u0bb0\u0bcd", new int[] { 28, 148, 86, 27, 90 })]
        [InlineData("\u0bae\u0ba4\u0bb0\u0bbe\u0b9a\u0bc1", new int[] { 29, 25, 31, 41, 106 })]
        [InlineData("\u0b87\u0bb0\u0bc1\u0ba8\u0bcd\u0ba4", new int[] { 8, 130, 85, 25 })]
        [InlineData("\u0baa\u0bc6\u0baf\u0bb0\u0bcd", new int[] { 46, 28, 30, 90 })]
        [InlineData("\u0b86\u0bae\u0bcd\u0bb7", new int[] { 7, 88, 38 })]
        [InlineData("\u0b86\u0ba3\u0bcd\u0b9f\u0bc1", new int[] { 7, 83, 112 })]
        [InlineData("\u0bae\u0bbe\u0bb1\u0bcd\u0bb1\u0baa\u0bcd\u0baa\u0b9f\u0bcd\u0b9f\u0ba4\u0bc1\u002e", new int[] { 29, 41, 91, 32, 87, 28, 82, 23, 116, 164 })]
        [InlineData("\u0bb8\u0bcd\u0bb0\u0bc0", new int[] { 147 })]
        [InlineData("\u0b95\u0bcd\u0bb7", new int[] { 76 })]
        public void CanShapeTamilText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(TamilNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0930\u094d\u0939", new int[] { 61, 181 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0930\u094d\u200c\u0939", new int[] { 52, 81, 61 })]
        [InlineData("\u0930\u094d\u200d\u0939", new int[] { 209, 61 })]
        [InlineData("\u0931\u094d\u0939", new int[] { 209, 61 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0931\u094d\u200c\u0939", new int[] { 53, 81, 61 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0931\u094d\u200d\u0939", new int[] { 209, 61 })]

        [InlineData("\u0915\u094d\u0915", new int[] { 183, 25 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200d", new int[] { 183 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200c\u0915", new int[] { 25, 81, 25 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200d\u0915", new int[] { 183, 25 })]
        [InlineData("\u0915\u094d\u0915\u093f", new int[] { 558, 183, 25 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200c\u0915\u093f", new int[] { 25, 81, 561, 25 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200d\u0915\u093f", new int[] { 558, 183, 25 })]
        [InlineData("\u0915\u094d\u0937", new int[] { 179 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200c\u0937", new int[] { 25, 81, 59 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200d\u0937", new int[] { 183, 59 })]
        [InlineData("\u0926\u094d\u0938\u093f", new int[] { 42, 81, 563, 60 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0926\u094d\u200c\u0938\u093f", new int[] { 42, 81, 563, 60 })]
        [InlineData("\u0926\u094d\u200d\u0938\u093f", new int[] { 558, 200, 60 })]
        public void CanShapeDevanagariTextWithJoiners(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(DevanagariNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0915", new int[] { 25 })]
        [InlineData("\u0915\u093c", new int[] { 92 })]
        [InlineData("\u0915\u093f", new int[] { 561, 25 })]
        [InlineData("\u0915\u094d", new int[] { 25, 81 })]
        [InlineData("\u0915\u094d\u0915", new int[] { 183, 25 })]
        [InlineData("\u0915\u094d\u0930", new int[] { 254 })]
        [InlineData("\u0915\u094d\u0930\u094d\u0915", new int[] { 327, 25 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u0930\u094d\u200d", new int[] { 327 })]
        [InlineData("\u0915\u094d\u0937", new int[] { 179 })]
        [InlineData("\u0915\u094d\u0937\u094d", new int[] { 179, 81 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200c\u0937", new int[] { 25, 81, 59 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u094d\u200d", new int[] { 183 })]
        [InlineData("\u0915\u094d\u200d\u0937", new int[] { 183, 59 })]
        [InlineData("\u091b\u094d\u0930\u094d\u0915", new int[] { 334, 25 })]
        [InlineData("\u091c\u094d\u091e\u094d", new int[] { 180, 81 })]
        [InlineData("\u091f\u094d\u0930\u0941", new int[] { 35, 657 })]
        [InlineData("\u0930\u094d\u0915", new int[] { 25, 181 })]
        [InlineData("\u0930\u094d\u0915\u093f", new int[] { 585, 25, 606 })]
        [InlineData("\u0930\u094d\u200d", new int[] { 209 })]
        [InlineData("\u093f", new int[] { 67, 135 })]
        [InlineData("\u092b\u093c\u094d\u0930", new int[] { 314 })]
        [InlineData("\u092b\u094d\u0930", new int[] { 275 })]
        [InlineData("\u0926\u094d\u0926\u093f", new int[] { 560, 511 })]
        [InlineData("\u0930\u094d\u0905\u094d", new int[] { 9, 81, 181 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0930\u094d\u0905\u094d\u200c", new int[] { 9, 81, 181 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0930\u094d\u0905\u094d\u200d", new int[] { 52, 81, 9, 81 })]
        [InlineData("\u0930\u094d\u0906\u094d\u0930\u094d", new int[] { 10, 81, 181, 52, 81 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0915\u200c\u093f", new int[] { 561, 25 })]
        [InlineData("\u093d\u0902", new int[] { 65, 6 })]
        [InlineData("\u0930\u0941\u0901\u0903", new int[] { 413, 5, 7 })]
        [InlineData("\u0031\u093f", new int[] { 558, 748 })]
        [InlineData("\u0967\u0951", new int[] { 107, 85 })]
        public void CanShapeDevanagariText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(DevanagariNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0930\u094d\u25cc", new int[] { 135, 181 })]
        [InlineData("\u0930\u094d\u25cc\u094d\u091a", new int[] { 135, 81, 181, 30 })]
        [InlineData("\u0930\u094d\u25cc\u094d\u091a\u094d\u091b\u0947", new int[] { 135, 81, 181, 188, 31, 75 })]
        [InlineData("\u0930\u094d\u25cc\u093f", new int[] { 67, 135, 181 })]
        [InlineData("\u0930\u094d\u25cc\u094d", new int[] { 135, 81, 181 })]
        [InlineData("\u0930\u094d\u25cc\u093c", new int[] { 135, 64, 181 })]
        [InlineData("\u25cc\u094d\u091a\u094d\u091b\u0947", new int[] { 135, 81, 188, 31, 75 })]
        [InlineData("\u0930\u094d\u00a0", new int[] { 52, 81, 3 })]
        public void CanShapeDevanagariTextWithDottedCircle(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(DevanagariNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0924\u094d\u0930\u094d\u0915", new int[] { 347, 25 })]

        // Harfbuzz replaces the default ignorable with id 134 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0924\u094d\u0930\u094d\u200d\u0915", new int[] { 347, 25 })]

        // Harfbuzz replaces the default ignorable with id 133 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0924\u094d\u0930\u094d\u200c\u0915", new int[] { 269, 81, 25 })]
        public void CanShapeDevanagariTextWithEyelash(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(DevanagariNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0985\u09cd\u09af", new int[] { 7, 198 })]
        [InlineData("\u0995", new int[] { 19 })]
        [InlineData("\u0995\u09bc", new int[] { 96 })]
        [InlineData("\u0995\u09bf", new int[] { 54, 19 })]
        [InlineData("\u0995\u09cd", new int[] { 19, 64 })]
        [InlineData("\u0995\u09cd\u0995", new int[] { 280 })]
        [InlineData("\u0995\u09cd\u09b0", new int[] { 199 })]
        [InlineData("\u0995\u09cd\u09b0\u09cd\u0995", new int[] { 199, 64, 19 })]

        // Harfbuzz replaces the default ignorable with id 573 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0995\u09cd\u200c\u0995", new int[] { 19, 64, 19 })]

        // Harfbuzz replaces the default ignorable with id 574 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0995\u09cd\u200d\u0995", new int[] { 130, 19 })]
        [InlineData("\u09a6\u09cd\u09af", new int[] { 36, 198 })]
        [InlineData("\u09a8\u09cd\u0995", new int[] { 149, 19 })]
        [InlineData("\u09a8\u09cd\u09a7", new int[] { 360 })]
        [InlineData("\u09a8\u09cd\u09af", new int[] { 38, 198 })]
        [InlineData("\u09a8\u09cd\u09b0", new int[] { 219 })]

        // Harfbuzz replaces the default ignorable with id 573 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u09a8\u09cd\u200c\u0995", new int[] { 38, 64, 19 })]
        [InlineData("\u09a8\u09cd\u200c\u09a7", new int[] { 38, 64, 37 })]
        [InlineData("\u09a8\u09cd\u200c\u09ac", new int[] { 38, 64, 41 })]
        [InlineData("\u09a8\u09cd\u200c\u09b0", new int[] { 38, 64, 45 })]

        // Harfbuzz replaces the default ignorable with id 574 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u09a8\u09cd\u200d\u0995", new int[] { 149, 19 })]
        [InlineData("\u09a8\u09cd\u200d\u09a7", new int[] { 149, 37 })]
        [InlineData("\u09a8\u09cd\u200d\u09ac", new int[] { 149, 41 })]
        [InlineData("\u09a8\u09cd\u200d\u09b0", new int[] { 149, 45 })]
        [InlineData("\u09af\u09cd", new int[] { 44, 64 })]
        [InlineData("\u09b0\u09cd\u0995", new int[] { 19, 127 })]
        [InlineData("\u09b0\u09cd\u0995\u09bf", new int[] { 54, 19, 127 })]
        [InlineData("\u09b0\u09cd\u0995\u09cc", new int[] { 446, 19, 127, 66 })]

        // Harfbuzz replaces the default ignorable with id 574 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u09b0\u09cd\u09a8\u09cd\u200d", new int[] { 45, 64, 38, 64 })]
        [InlineData("\u09b0\u09cd\u09ac\u09cd\u09ac", new int[] { 263, 127 })]
        [InlineData("\u09b6\u09cd\u09af", new int[] { 47, 198 })]
        [InlineData("\u09b7\u09cd\u09af", new int[] { 48, 198 })]
        [InlineData("\u09b8\u09cd\u09af", new int[] { 49, 198 })]
        [InlineData("\u09bf", new int[] { 54, 575 })]
        [InlineData("\u0995\u09c7\u09be", new int[] { 446, 19, 53 })]
        [InlineData("\u0995\u09c7\u09d7", new int[] { 446, 19, 66 })]
        [InlineData("\u09b0\u09cd\u0995\u09be\u0982", new int[] { 19, 127, 53, 5 })]
        [InlineData("\u09b0\u09cd\u0995\u09be\u0983", new int[] { 19, 127, 53, 6 })]
        [InlineData("\u09b0\u09cd\u09ad", new int[] { 42, 127 })]
        [InlineData("\u09f0\u09cd\u09ad", new int[] { 42, 127 })]
        [InlineData("\u09f1\u09cd\u09ad", new int[] { 85, 64, 42 })]
        [InlineData("\u0985\u09d7", new int[] { 7, 66 })]
        [InlineData("\u09a8\u09cd\u09a4\u09cd\u09b0", new int[] { 365 })]
        [InlineData("\u09a4\u09cd\u09af\u09c1", new int[] { 34, 518, 198 })]
        [InlineData("\u099a\u09cd\u09af\u09cd\u09b0", new int[] { 135, 225 })]

        // Harfbuzz replaces the default ignorable with id 574 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0995\u09cd\u200d\u09b7", new int[] { 130, 48 })]
        public void CanShapeBengaliText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(BengaliNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0a15\u0a4d\u0a39", new int[] { 17, 111 })]
        [InlineData("\u0a24\u0a4d\u0a2f\u0a4b", new int[] { 32, 175, 58 })]
        public void CanShapeGurmukhiText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(GurmukhiNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0a97\u0acd\u0ab7", new int[] { 132, 52 })]
        [InlineData("\u0a97\u0acd\u0ab8", new int[] { 132, 53 })]
        [InlineData("\u0a97\u0acd\u0ab9", new int[] { 132, 54 })]
        [InlineData("\u0a98\u0acd\u0a95", new int[] { 133, 21 })]
        [InlineData("\u0a98\u0acd\u0a96", new int[] { 133, 22 })]
        [InlineData("\u0a98\u0acd\u0a97", new int[] { 133, 23 })]
        [InlineData("\u0a98\u0acd\u0a98", new int[] { 133, 24 })]
        [InlineData("\u0a98\u0acd\u0a99", new int[] { 133, 25 })]
        [InlineData("\u0a98\u0acd\u0a9a", new int[] { 133, 26 })]
        [InlineData("\u0a98\u0acd\u0a9b", new int[] { 133, 27 })]
        [InlineData("\u0a98\u0acd\u0a9c", new int[] { 133, 28 })]
        [InlineData("\u0a98\u0acd\u0a9d", new int[] { 133, 29 })]
        [InlineData("\u0a98\u0acd\u0a9e", new int[] { 133, 30 })]
        [InlineData("\u0a98\u0acd\u0a9f", new int[] { 133, 31 })]
        [InlineData("\u0a98\u0acd\u0aa0", new int[] { 133, 32 })]
        [InlineData("\u0a98\u0acd\u0aa1", new int[] { 133, 33 })]
        [InlineData("\u0a98\u0acd\u0aa2", new int[] { 133, 34 })]
        [InlineData("\u0a98\u0acd\u0aa3", new int[] { 133, 35 })]
        [InlineData("\u0a98\u0acd\u0aa4", new int[] { 133, 36 })]
        [InlineData("\u0a98\u0acd\u0aa5", new int[] { 133, 37 })]
        [InlineData("\u0a98\u0acd\u0aa6", new int[] { 133, 38 })]
        public void CanShapeGujaratiText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(GujaratiNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0d05\u0d4e\u0d24\u0d4d\u0d25\u0d02", new int[] { 6, 180, 73, 4 })]
        [InlineData("\u0d05\u0d25\u0d4e\u0d35\u0d4d\u0d35\u0d02", new int[] { 6, 36, 208, 73, 4 })]
        [InlineData("\u0d15\u0d4d\u200d", new int[] { 101 })]

        // Harfbuzz replaces the default ignorable with id 103 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0d15\u0d3e\u0d2f\u0d4d\u200c\u0d15\u0d31\u0d3f", new int[] { 20, 59, 46, 72, 20, 48, 60 })]
        [InlineData("\u0d15\u0d3e\u0d30\u0d4d\u200d\u0d15\u0d4d\u0d15\u0d4b\u0d1f\u0d15\u0d28\u0d4d\u200d", new int[] { 20, 59, 98, 67, 147, 59, 30, 20, 97 })]
        [InlineData("\u0d15\u0d41\u0d31\u0d4d\u0d31\u0d4d\u0d2f\u0d3e\u0d1f\u0d3f", new int[] { 20, 62, 203, 229, 59, 30, 60 })]
        [InlineData("\u0d15\u0d46", new int[] { 66, 20 })]
        [InlineData("\u0d15\u0d47", new int[] { 67, 20 })]
        [InlineData("\u0d15\u0d48", new int[] { 68, 20 })]
        [InlineData("\u0d15\u0d4a", new int[] { 66, 20, 59 })]
        [InlineData("\u0d15\u0d4b", new int[] { 67, 20, 59 })]
        [InlineData("\u0d15\u0d46\u0d57", new int[] { 66, 20, 74 })]
        [InlineData("\u0d15\u0d4d\u0d15\u0d46", new int[] { 66, 147 })]
        [InlineData("\u0d15\u0d4d\u0d24\u0d4d\u0d30", new int[] { 146, 148 })]
        [InlineData("\u0d15\u0d4d\u0d2f", new int[] { 20, 144 })]
        [InlineData("\u0d15\u0d4d\u0d35", new int[] { 20, 145 })]
        [InlineData("\u0d16\u0d4d\u0d2f", new int[] { 21, 144 })]
        [InlineData("\u0d16\u0d4d\u0d30", new int[] { 146, 21 })]
        [InlineData("\u0d17\u0d4d\u0d26\u0d4d\u0d27\u0d4d\u0d30\u0d4b", new int[] { 22, 72, 67, 146, 183, 59 })]
        [InlineData("\u0d1f\u0d4d\u0d1f", new int[] { 167 })]
        [InlineData("\u0d1f\u0d4d\u0d1f\u0d41\u0d4d", new int[] { 167, 225, 72 })]
        [InlineData("\u0d23\u0d4d\u200d", new int[] { 96 })]
        [InlineData("\u0d23\u0d4d\u0d1f", new int[] { 174 })]
        [InlineData("\u0d24\u0d4d\u0d24", new int[] { 179 })]
        [InlineData("\u0d24\u0d4d\u0d24\u0d46", new int[] { 66, 179 })]
        [InlineData("\u0d24\u0d4d\u0d24\u0d4a", new int[] { 66, 179, 59 })]
        [InlineData("\u0d26\u0d4d\u0d26", new int[] { 182 })]
        [InlineData("\u0d28\u0d4d\u200d", new int[] { 97 })]
        [InlineData("\u0d28\u0d4d\u0d24", new int[] { 189 })]
        [InlineData("\u0d28\u0d4d\u0d24\u0d4d\u0d2f", new int[] { 189, 144 })]
        [InlineData("\u0d28\u0d4d\u0d24\u0d4d\u0d30\u0d4d\u0d2f", new int[] { 146, 189, 144 })]
        [InlineData("\u0d2a\u0d4d\u0d30", new int[] { 146, 41 })]
        [InlineData("\u0d2a\u0d4d\u0d32\u0d4b", new int[] { 67, 192, 59 })]
        [InlineData("\u0d2e\u0d41\u0d16\u0d4d\u0d2f\u0d2e\u0d28\u0d4d\u0d24\u0d4d\u0d30\u0d3f", new int[] { 45, 62, 21, 144, 45, 146, 189, 60 })]
        [InlineData("\u0d2e\u0d4d\u0d2a", new int[] { 199 })]
        [InlineData("\u0d2f\u0d3e\u0d24\u0d4d\u0d30\u0d3e\u0d15\u0d42\u0d32\u0d3f", new int[] { 46, 59, 146, 35, 59, 20, 63, 49, 60 })]
        [InlineData("\u0d2f\u0d41\u0d02", new int[] { 46, 62, 4 })]
        [InlineData("\u0d2f\u0d4d\u0d15\u0d4d\u0d15\u0d41", new int[] { 46, 72, 147, 62 })]
        [InlineData("\u0d2f\u0d4d\u0d2f", new int[] { 202 })]
        [InlineData("\u0d30\u0d4d", new int[] { 47, 72 })]
        [InlineData("\u0d30\u0d4d\u200d", new int[] { 98 })]
        [InlineData("\u0d30\u0d4d\u0d15", new int[] { 47, 72, 20 })]
        [InlineData("\u0d30\u0d4d\u0d2f", new int[] { 47, 144 })]
        [InlineData("\u0d32\u0d4d\u200d", new int[] { 99 })]
        [InlineData("\u0d32\u0d4d\u0d2f", new int[] { 49, 144 })]
        [InlineData("\u0d32\u0d4d\u0d32", new int[] { 205 })]
        [InlineData("\u0d32\u0d4d\u0d32\u0d3e\u0d02", new int[] { 205, 59, 4 })]
        [InlineData("\u0d35\u0d4d\u0d35", new int[] { 208 })]
        [InlineData("\u0d37\u0d4d\u0d1f\u0d4d\u0d30\u0d40", new int[] { 54, 72, 146, 30, 61 })]

        // Harfbuzz replaces the default ignorable with id 103 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0d38\u0d4b\u0d2b\u0d4d\u0d31\u0d4d\u0d31\u0d4d\u200c\u0d35\u0d46\u0d2f\u0d30\u0d4d\u200d", new int[] { 67, 55, 59, 42, 72, 203, 72, 66, 52, 46, 98 })]
        [InlineData("\u0d38\u0d4d\u0d2a\u0d4d\u0d30\u0d3f", new int[] { 55, 72, 146, 41, 60 })]
        [InlineData("\u0d38\u0d4d\u0d2a\u0d4d\u0d30\u0d47", new int[] { 55, 72, 67, 146, 41 })]
        [InlineData("\u0d38\u0d4d\u0d2a\u0d4d\u0d32\u0d47", new int[] { 55, 72, 67, 192 })]
        [InlineData("\u0d38\u0d4d\u0d35\u0d3e\u0d24\u0d28\u0d4d\u0d24\u0d4d\u0d30\u0d4d\u0d2f\u0d02", new int[] { 55, 145, 59, 35, 146, 189, 144, 4 })]

        // Harfbuzz replaces the default ignorable with id 103 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0d39\u0d3e\u0d30\u0d4d\u200d\u0d21\u0d4d\u200c\u0d35\u0d46\u0d2f\u0d30\u0d4d\u200d", new int[] { 56, 59, 98, 32, 72, 66, 52, 46, 98 })]
        [InlineData("\u0d33\u0d4d\u200d", new int[] { 100 })]
        [InlineData("\u0d33\u0d4d\u0d2f\u0d02", new int[] { 50, 229, 4 })]
        [InlineData("\u0d33\u0d4d\u0d33", new int[] { 206 })]
        [InlineData("\u0d32\u0d4d\u200d\u0d2a\u0d4d\u0d2a\u0d47", new int[] { 99, 67, 191 })]

        // Harfbuzz replaces the default ignorable with id 103 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u0d36\u0d3f\u0d02\u200c", new int[] { 53, 60, 4 })]
        [InlineData("\u0d15\u0d4b\u0d02\u200c", new int[] { 67, 20, 59, 4 })]
        [InlineData("\u0d2f\u200d\u0d4d\u0d2f", new int[] { 46, 144 })]
        [InlineData("\u0d38\u0d4d\u0d31\u0d4d\u0d31\u0d4d", new int[] { 214, 72 })]
        public void CanShapeMalayalamText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(MalayalamNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u0b15\u0b4d\u0b24\u0b4d\u0b30", new int[] { 165, 527 })]
        [InlineData("\u0b24\u0b4d\u0b24\u0b4d\u0b2c", new int[] { 195, 150 })]
        [InlineData("\u0b28\u0b4d\u0b24\u0b4d\u0b2c", new int[] { 206, 525 })]
        [InlineData("\u0b28\u0b4d\u0b24\u0b4d\u0b30", new int[] { 38, 161 })]
        [InlineData("\u0b28\u0b4d\u0b24\u0b4d\u0b30\u0b4d\u0b2f", new int[] { 38, 161, 162 })]
        [InlineData("\u0b38\u0b4d\u0b24\u0b4d\u0b30", new int[] { 51, 161 })]
        [InlineData("\u0b2e\u0b41\u0b01", new int[] { 43, 4, 58 })]
        [InlineData("\u0b2e\u0b41\u0b02", new int[] { 43, 58, 5 })]
        public void CanShapeOriyaText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(OriyaNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }

        [Theory]
        [InlineData("\u1781\u17d2\u1798\u17c2", new int[] { 108, 45, 169 })]
        [InlineData("\u1787\u17b6", new int[] { 51, 96 })]
        [InlineData("\u1790\u17d2\u1784\u17c3", new int[] { 109, 60, 148 })]
        [InlineData("\u1798\u17b6", new int[] { 68, 96 })]
        [InlineData("\u1798\u17d2\u1796\u17bb", new int[] { 68, 167, 177 })]
        [InlineData("\u179a", new int[] { 70 })]
        [InlineData("\u179a\u17b8", new int[] { 70, 194 })]
        [InlineData("\u179a\u17cd", new int[] { 70, 199 })]
        [InlineData("\u179f\u17c5", new int[] { 107, 75, 111 })]
        [InlineData("\u179a\u17d2\u17a5", new int[] { 70, 124, 81 })]
        [InlineData("\u1784\u17b9\u17d2\u1788", new int[] { 48, 99, 152 })]
        [InlineData("\u1784\u17d2\u1788\u17b9", new int[] { 48, 152, 99 })]
        [InlineData("\u1784\u17d2\u1782\u17d2\u179a", new int[] { 189, 48, 146 })]
        [InlineData("\u1798\u17c9\u17d2\u179b\u17c1\u17c7", new int[] { 107, 68, 115, 172, 113 })]

        // Harfbuzz replaces the default ignorable with id 262 with a space (3) and sets the advance to 0. We skip it entirely on rendering.
        [InlineData("\u1798\u200c\u17c9\u17d2\u179b\u17c1\u17c7", new int[] { 107, 68, 115, 172, 113 })]
        [InlineData("\u1794\u17ca\u17d0", new int[] { 64, 116, 122 })]
        [InlineData("\u1793\u17c2\u17ce", new int[] { 108, 63, 120 })]
        [InlineData("\u1780\u17c1\u17d2\u179a", new int[] { 107, 171, 44 })]
        [InlineData("\u1780\u17c0\u17d2\u179a", new int[] { 171, 107, 44, 106 })]
        [InlineData("\u1780\u17c4\u17d2\u179a", new int[] { 171, 107, 44, 110 })]
        [InlineData("\u1780\u17c5\u17d2\u179a", new int[] { 171, 107, 44, 111 })]
        [InlineData("\u1796\u17d1\u17b6", new int[] { 66, 123, 96 })]
        [InlineData(
            "\u178a\u17be\u1798\u17d2\u1794\u17b8\u17b2\u17d2\u1799\u1794\u17b6\u1793\u1780\u17b6\u1793\u17cb\u178f\u17c2\u1794\u17d2\u179a\u179f\u17be\u179a\u17a1\u17be\u1784\u179f\u1798\u17d2\u179a\u17b6\u1794\u17cb\u1780\u17b6\u179a\u1792\u17d2\u179c\u17be\u178a\u17c6\u178e\u17be\u179a\u179a\u1794\u179f\u17cb\u1797\u17d2\u1789\u17c0\u179c\u1791\u17c1\u179f\u1785\u179a\u178e\u17cd",
            new int[] { 107, 54, 104, 68, 165, 98, 94, 170, 188, 96, 63, 44, 96, 63, 117, 108, 59, 171, 64, 107, 75, 104, 70, 107, 77, 104, 48, 75, 171, 68, 96, 64, 117, 44, 96, 70, 107, 62, 173, 104, 54, 112, 107, 58, 104, 70, 70, 64, 75, 117, 107, 67, 153, 192, 72, 107, 61, 75, 49, 70, 58, 119 })]
        public void CanShapeKhmerText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(KhmerNotoSansTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }
    }
}
