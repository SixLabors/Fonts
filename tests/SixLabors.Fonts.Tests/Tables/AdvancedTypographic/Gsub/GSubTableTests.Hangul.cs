// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.Gsub
{
    /// <content>
    /// Tests adapted from <see href="https://github.com/foliojs/fontkit/blob/417af0c79c5664271a07a783574ec7fac7ebad0c/test/shaping.js"/>.
    /// </content>
    public partial class GSubTableTests
    {
        // TODO: Switch to NotoSansKR-Regular when we have CFF support.
#if OS_WINDOWS
        private readonly Font hangulFont = SystemFonts.CreateFont("Malgun Gothic", 12);

        [Fact]
        public void ShouldUseComposedSyllables()
        {
            // arrange
            const string input = "\uD734\uAC00\u0020\uAC00\u002D\u002D\u0020\u0028\uC624\u002D\u002D\u0029";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 2953, 636, 3, 636, 16, 16, 3, 11, 2077, 16, 16, 12 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldComposeDecomposedSyllables()
        {
            // arrange
            const string input = "\u1112\u1172\u1100\u1161\u0020\u1100\u1161\u002D\u002D\u0020\u0028\u110B\u1169\u002D\u002D\u0029";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 2953, 636, 3, 636, 16, 16, 3, 11, 2077, 16, 16, 12 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldUseOTFeaturesForNonCombining_L_V_T()
        {
            // arrange
            const string input = "\ua960\ud7b0\ud7cb";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 21150, 21436, 21569 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldDecompose_LV_T_To_L_V_T_If_LVT_IsNotSupported()
        {
            // <L,V> combine at first, but the T is non-combining, so this
            // tests that the <LV> gets decomposed again in this case.

            // arrange
            const string input = "\u1100\u1161\ud7cb";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 20667, 21294, 21569 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldReorderToneMarksToBeginningOf_L_V_Syllables()
        {
            // arrange
            const string input = "\ua960\ud7b0\u302f";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 20665, 21150, 21435 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldReorderToneMarksToBeginningOf_L_V_T_Syllables()
        {
            // arrange
            const string input = "\ua960\ud7b0\ud7cb\u302f";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 20665, 21150, 21436, 21569 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldReorderToneMarksToBeginningOf_LV_Syllables()
        {
            // arrange
            const string input = "\uac00\u302f";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 20665, 636 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldReorderToneMarksToBeginningOf_LVT_Syllables()
        {
            // arrange
            const string input = "\uac01\u302f";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 20665, 637 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ShouldInsertDottedCircleForInvalidToneMarks()
        {
            // arrange
            const string input = "\u1100\u302f\u1161";
            ColorGlyphRenderer renderer = new();
            int[] expectedGlyphIndices = { 2986, 20665, 21620, 3078 };

            // act
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.hangulFont));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }
#endif
    }
}
