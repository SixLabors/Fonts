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
            string testStr = "\u0012\u0014"; // XPlacement should be adjusted by minus 200 for both glyphs.
            int[] expectedGlyphIndices = { 20, 22 };
            FontRectangle[] expectedFontRectangles =
            {
                new(1.76f, 12.8703995f, 5.28f, 4.6016f),
                new(10.3808f, 12.8639984f, 6.2592f, 4.5056f),
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

        // LookupType4SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable
        [Fact]
        public void MarkToBaseAttachmentPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType4Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0013"; // "\u0012\u0013" characters should overlap.
            int[] expectedGlyphIndices = { 20, 21 };
            FontRectangle[] expectedFontRectangles =
            {
                new(3.04f, 12.8703995f, 5.28f, 4.6016f),
                new(2.40000057f, 13.184f, 5.28f, 4.5376f),
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

        // LookupType5SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable
        [Fact]
        public void MarkToLigatureAttachmentPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType5Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0013"; // "\u0012\u0013" characters should overlap.
            int[] expectedGlyphIndices = { 20, 21 };
            FontRectangle[] expectedFontRectangles =
            {
                new(3.04f, 12.8703995f, 5.28f, 4.6016f),
                new(2.40000057f, 13.184f, 5.28f, 4.5376f),
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

        // LookupType6SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable
        [Fact]
        public void MarkToMarkAttachmentPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType6Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0013"; // "\u0012\u0013" characters should overlap.
            int[] expectedGlyphIndices = { 20, 21 };
            FontRectangle[] expectedFontRectangles =
            {
                new(3.04f, 12.8703995f, 5.28f, 4.6016f),
                new(2.40000057f, 13.376f, 5.28f, 4.5376f),
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

        // LookupType7SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables
        [Fact]
        public void ContextualPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType7Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016"; // "\u0014\u0015\u0016" XPlacement plus 20.
            int[] expectedGlyphIndices = { 22, 23, 24 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.1888f, 12.8639984f, 6.2592f, 4.5056f),
                new(11.7888f, 12.8703995f, 6.2592f, 4.5312f),
                new(21.3888016f, 13.0175991f, 6.2592f, 4.2688f),
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

        // LookupType7SubTable, Format 2
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables
        [Fact]
        public void ContextualPositioning_Format2_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType7Format2).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016"; // "\u0014\u0015\u0016" XPlacement plus 20.
            int[] expectedGlyphIndices = { 22, 23, 24 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.1888f, 12.8639984f, 6.2592f, 4.5056f),
                new(11.7888f, 12.8703995f, 6.2592f, 4.5312f),
                new(21.3888016f, 13.0175991f, 6.2592f, 4.2688f),
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

        // LookupType7SubTable, Format 3
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables
        [Fact]
        public void ContextualPositioning_Format3_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType7Format3).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016"; // "\u0014\u0015\u0016" XPlacement plus 20.
            int[] expectedGlyphIndices = { 22, 23, 24 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.1888f, 12.8639984f, 6.2592f, 4.5056f),
                new(11.9168005f, 12.8703995f, 6.2592f, 4.5312f),
                new(21.6448f, 13.0175991f, 6.2592f, 4.2688f),
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

        // LookupType8SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable
        [Fact]
        public void ChainedContextsPositioning_Format1_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType8Format1).CreateFont(8);
            var renderer = new ColorGlyphRenderer();

            // "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
            string testStr = "\u0014\u0015\u0016\u0017";
            int[] expectedGlyphIndices = { 22, 23, 24, 25 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.0608f, 12.8639984f, 6.2592f, 4.5056f),
                new(12.9408007f, 12.8703995f, 6.2592f, 4.5312f),
                new(22.5408f, 13.0175991f, 6.2592f, 4.2688f),
                new(30.8608036f, 12.9919987f, 6.2592f, 4.2944f),
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

        // LookupType8SubTable, Format 2
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable
        [Fact]
        public void ChainedContextsPositioning_Format2_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType8Format2).CreateFont(8);
            var renderer = new ColorGlyphRenderer();

            // "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
            string testStr = "\u0014\u0015\u0016\u0017";
            int[] expectedGlyphIndices = { 22, 23, 24, 25 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.0608f, 12.8639984f, 6.2592f, 4.5056f),
                new(12.9408007f, 12.8703995f, 6.2592f, 4.5312f),
                new(22.5408f, 13.0175991f, 6.2592f, 4.2688f),
                new(30.8608036f, 12.9919987f, 6.2592f, 4.2944f),
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

        // LookupType8SubTable, Format 3
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable
        [Fact]
        public void ChainedContextsPositioning_Format3_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposLookupType8Format3).CreateFont(8);
            var renderer = new ColorGlyphRenderer();

            // "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
            string testStr = "\u0014\u0015\u0016\u0017";
            int[] expectedGlyphIndices = { 22, 23, 24, 25 };
            FontRectangle[] expectedFontRectangles =
            {
                new(2.0608f, 12.8639984f, 6.2592f, 4.5056f),
                new(12.9408007f, 12.8703995f, 6.2592f, 4.5312f),
                new(22.5408f, 13.0175991f, 6.2592f, 4.2688f),
                new(30.8608036f, 12.9919987f, 6.2592f, 4.2944f),
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

        [Fact]
        public void MarkAnchoring_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.TimesNewRomanFile).CreateFont(8);
            var renderer = new ColorGlyphRenderer();

            string testStr = "\u0644\u0651"; // /lam-arab/arabicshaddacomb
            int[] expectedGlyphIndices = { 759, 989 };
            FontRectangle[] expectedFontRectangles =
            {
                new(0.9609375f, 1.03124952f, 1.41796875f, 1.2578125f),
                new(0.27734375f, 1.26953077f, 3.7734375f, 6.14453125f),
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(font)
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

        [Fact]
        public void MarkToMarkAttachment_Works()
        {
            // arrange
            Font font = new FontCollection().Add(TestFonts.MeQuranFile).CreateFont(8);
            var renderer = new ColorGlyphRenderer();

            string testStr = "\u0631\u0651\u064E";
            int[] expectedGlyphIndices = { 47, 50, 23 };
            FontRectangle[] expectedFontRectangles =
            {
                new(1.34765625f, 5.4609375f, 2.19140625f, 1.80078125f),
                new(1.640625f, 7.17578125f, 1.74609375f, 1.6171875f),
                new(0.15625f, 9.8203125f, 3.9140625f, 3.8515625f),
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(font)
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
