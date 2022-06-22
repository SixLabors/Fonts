// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GSub
{
    public partial class GSubTableTests
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
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(arabicFont));

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        // LookupType1SubTable
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#11-single-substitution-format-1
        [Fact]
        public void SingleSubstitution_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubTestFontFile1).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "A";
            int expectedGlyphIndex = 38; // we expect A to be mapped to B.

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        [Fact]
        public void ContextualFractions_WithFractionSlash_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.RobotoRegular).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "9⁄2";
            int[] expectedGlyphIndices = { 580, 404, 453 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font) { FeatureTags = new Tag[] { FeatureTags.Numerators, FeatureTags.Denominators } });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ContextualFractions_WithSlash_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.RobotoRegular).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "9/2";
            int[] expectedGlyphIndices = { 580, 404, 453 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font) { FeatureTags = new Tag[] { FeatureTags.Fractions } });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType2SubTable
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#21-multiple-substitution-format-1
        [Fact]
        public void MultipleSubstitution_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubTestFontFile1).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "C";
            int expectedGlyphIndex = 40; // we expect C to be mapped to D.

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        // LookupType3SubTable
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-3-alternate-substitution-subtable
        [Fact]
        public void AlternateSubstitution_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubTestFontFile1).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "E";
            int expectedGlyphIndex = 42; // we expect E to be mapped to F.

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        // LookupType4SubTable
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-3-alternate-substitution-subtable
        [Fact]
        public void LigatureSubstitution_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubTestFontFile1).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "ffi";
            int expectedGlyphIndex = 229;

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            GlyphRendererParameters glyphKey = Assert.Single(renderer.GlyphKeys);
            Assert.Equal(expectedGlyphIndex, glyphKey.GlyphIndex);
        }

        // LookupType5SubTable, Format 1.
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-5-contextual-substitution-subtable
        [Fact]
        public void ContextualSubstitution_Format1_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubLookupType5Format1).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0041\u0042"; // "6566" (\u0041\u0042) -> "6576"
            int[] expectedGlyphIndices = { 3, 7 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType5SubTable, Format 2.
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-5-contextual-substitution-subtable
        [Fact]
        public void ContextualSubstitution_Format2_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubLookupType5Format2).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0041\u0042"; // "6566" (\u0041\u0042) -> "6576"
            int[] expectedGlyphIndices = { 3, 7 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType5SubTable, Format 3
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-5-contextual-substitution-subtable
        [Fact]
        public void ContextualSubstitution_Format3_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubLookupType5Format3).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0041\u0042\u0043\u0044"; // "65666768" -> "657678"
            int[] expectedGlyphIndices = { 67, 78, 80 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType6SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#62-chained-contexts-substitution-format-1-class-based-glyph-contexts
        [Fact]
        public void ChainedContextsSubstitution_Format1_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubLookupType6Format1).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016\u0017"; // "20212223" -> "20636423"
            int[] expectedGlyphIndices = { 22, 63, 64, 25 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType6SubTable, Format 2
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#62-chained-contexts-substitution-format-2-class-based-glyph-contexts
        [Fact]
        public void ChainedContextsSubstitution_Format2_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubLookupType6Format2).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016\u0017"; // "20212223" -> "20216423"
            int[] expectedGlyphIndices = { 22, 23, 64, 25 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType6SubTable, Format 3
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#63-chained-contexts-substitution-format-3-coverage-based-glyph-contexts
        [Fact]
        public void ChainedContextsSubstitution_Format3_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubTestFontFile2).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "x=y"; // This should be replaced with "x>y".
            int[] expectedGlyphIndices = { 89, 31, 90 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType6SubTable, Format 3
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#63-chained-contexts-substitution-format-3-coverage-based-glyph-contexts
        [Fact]
        public void ChainedContextsSubstitution_Format3_WithCursiveScript_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.FormalScript).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "ba"; // Characters following b should have a special form and should be replaced.
            int[] expectedGlyphIndices = { 69, 102 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // LookupType8SubTable
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-8-reverse-chaining-contextual-single-substitution-subtable
        [Fact]
        public void ReverseChainingContextualSingleSubstitution_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.GSubTestFontFile2).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "X89"; // X89 -> XYZ
            int[] expectedGlyphIndices = { 57, 58, 59 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font));

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        // Related PR: https://github.com/SixLabors/Fonts/pull/248
        [Fact]
        public void OldStyleFiguresFeature_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.EbGaramond).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "123456";
            int[] expectedGlyphIndices = { 2242, 2243, 2244, 2245, 2246, 2247 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new TextOptions(font)
            {
                FeatureTags = new List<Tag> { FeatureTags.OldstyleFigures }
            });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void BillionLaughsAttackDoesNotThrowException()
        {
            // Arrange
            Font font = new FontCollection().Add(TestFonts.GSubLookupType2BillionLaughs).CreateFont(12);

            // Act
            TextMeasurer.Measure("lol", new TextOptions(font));
        }
    }
}
