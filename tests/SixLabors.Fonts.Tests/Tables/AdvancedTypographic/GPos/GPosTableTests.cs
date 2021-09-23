// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GPos
{
    public class GPosTableTests
    {
        // LookupType1SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable
        [Fact]
        public void SingleAdjustmentPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType1Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0015\u0014"; // second character XPlacement should be adjusted by minus 200
            int[] expectedGlyphIndices = { 23, 22 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.0608f, 12.8703995f, 6.2592f, 4.5312f),
                new(9.10080051f, 12.8639984f, 6.2592f, 4.5056f),
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gPosFont)
            {
                ApplyKerning = true
            });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            Assert.Equal(expectedFontRectangles.Length, renderer.GlyphRects.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }

            for (int i = 0; i < expectedFontRectangles.Length; i++)
            {
                CompareRectangle(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType1SubTable, Format 2
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable
        [Fact]
        public void SingleAdjustmentPositioning_Format2_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType1Format2).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0015\u0014"; // second character XPlacement should be adjusted by minus 200
            int[] expectedGlyphIndices = { 23, 22 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.0608f, 12.8703995f, 6.2592f, 4.5312f),
                new(9.740801f, 12.8639984f, 6.2592f, 4.5056f),
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gPosFont)
            {
                ApplyKerning = true
            });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            Assert.Equal(expectedFontRectangles.Length, renderer.GlyphRects.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }

            for (int i = 0; i < expectedFontRectangles.Length; i++)
            {
                CompareRectangle(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType2SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable
        [Fact]
        public void PairAdjustmentPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType2Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0017\u0012\u0014"; // "\u0012\u0014" first XPlacement minus 300 and second YPlacement minus 400.
            int[] expectedGlyphIndices = { 25, 20, 22 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.0608f, 12.9919987f, 6.2592f, 4.2944f),
                new(10.72f, 12.8703995f, 5.28f, 4.6016f),
                new(21.2608013f, 15.4239988f, 6.2592f, 4.5056f),
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gPosFont)
            {
                ApplyKerning = true
            });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            Assert.Equal(expectedFontRectangles.Length, renderer.GlyphRects.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }

            for (int i = 0; i < expectedFontRectangles.Length; i++)
            {
                CompareRectangle(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType3SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable
        [Fact]
        public void CursiveAttachmentPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType3Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0012"; // "\u0012\u0012" characters should overlap.
            int[] expectedGlyphIndices = { 20, 20 };
            FontRectangle[] expectedFontRectangles =
            {
                new(3.04f, 13.5103989f, 5.28f, 4.6016f),
                new(3.68000031f, 12.8703995f, 5.28f, 4.6016f),
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gPosFont)
            {
                ApplyKerning = true
            });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            Assert.Equal(expectedFontRectangles.Length, renderer.GlyphRects.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }

            for (int i = 0; i < expectedFontRectangles.Length; i++)
            {
                CompareRectangle(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        private static void CompareRectangle(FontRectangle expected, FontRectangle actual, int precision = 4)
        {
            Assert.Equal(expected.X, actual.X, precision);
            Assert.Equal(expected.Y, actual.Y, precision);
            Assert.Equal(expected.Width, actual.Width, precision);
            Assert.Equal(expected.Height, actual.Height, precision);
        }
    }
}
