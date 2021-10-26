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
            string fontFile = TestFonts.GposLookupType1Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0014"; // XPlacement should be adjusted by minus 200 for both glyphs.
            int[] expectedGlyphIndices = { 20, 22 };
            FontRectangle[] expectedFontRectangles =
            {
                new(275, 2011, 825, 719),
                new(1622, 2010, 978, 704),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType1SubTable, Format 2
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-1-single-adjustment-positioning-subtable
        [Fact]
        public void SingleAdjustmentPositioning_Format2_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType1Format2;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0015\u0014"; // second character XPlacement should be adjusted by minus 200
            int[] expectedGlyphIndices = { 23, 22 };
            FontRectangle[] expectedFontRectangles =
            {
                new(322, 2011, 978, 708),
                new(1522, 2010, 978, 704),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType2SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-2-pair-adjustment-positioning-subtable
        [Fact]
        public void PairAdjustmentPositioning_Format1_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType2Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0017\u0012\u0014"; // "\u0012\u0014" first XPlacement minus 300 and second YPlacement minus 400.
            int[] expectedGlyphIndices = { 25, 20, 22 };
            FontRectangle[] expectedFontRectangles =
            {
                new(322, 2030, 978, 671),
                new(1675, 2011, 825, 719),
                new(3322, 2410, 978, 704),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType3SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-3-cursive-attachment-positioning-subtable
        [Fact]
        public void CursiveAttachmentPositioning_Format1_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType3Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0012"; // "\u0012\u0012" characters should overlap.
            int[] expectedGlyphIndices = { 20, 20 };
            FontRectangle[] expectedFontRectangles =
            {
                new(475, 2111, 825, 719),
                new(575, 2011, 825, 719),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType4SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-4-mark-to-base-attachment-positioning-subtable
        [Fact]
        public void MarkToBaseAttachmentPositioning_Format1_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType4Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0013"; // "\u0012\u0013" characters should overlap.
            int[] expectedGlyphIndices = { 20, 21 };
            FontRectangle[] expectedFontRectangles =
            {
                new(475, 2011, 825, 719),
                new(375, 2090, 825, 709),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType5SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-5-mark-to-ligature-attachment-positioning-subtable
        [Fact]
        public void MarkToLigatureAttachmentPositioning_Format1_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType5Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0013"; // "\u0012\u0013" characters should overlap.
            int[] expectedGlyphIndices = { 20, 21 };
            FontRectangle[] expectedFontRectangles =
            {
                new(475, 2011, 825, 719),
                new(375, 2090, 825, 709),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType6SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-6-mark-to-mark-attachment-positioning-subtable
        [Fact]
        public void MarkToMarkAttachmentPositioning_Format1_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType6Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0012\u0013"; // "\u0012\u0013" characters should overlap.
            int[] expectedGlyphIndices = { 20, 21 };
            FontRectangle[] expectedFontRectangles =
            {
                new(475, 2011, 825, 719),
                new(375, 2090, 825, 709),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType7SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables
        [Fact]
        public void ContextualPositioning_Format1_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType7Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016"; // "\u0014\u0015\u0016" XPlacement plus 20.
            int[] expectedGlyphIndices = { 22, 23, 24 };
            FontRectangle[] expectedFontRectangles =
            {
                new(342, 2010, 978, 704),
                new(1842, 2011, 978, 708),
                new(3342, 2034, 978, 667),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType7SubTable, Format 2
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables
        [Fact]
        public void ContextualPositioning_Format2_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType7Format2;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016"; // "\u0014\u0015\u0016" XPlacement plus 20.
            int[] expectedGlyphIndices = { 22, 23, 24 };
            FontRectangle[] expectedFontRectangles =
            {
                new(342, 2010, 978, 704),
                new(1842, 2011, 978, 708),
                new(3342, 2034, 978, 667),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType7SubTable, Format 3
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookup-type-7-contextual-positioning-subtables
        [Fact]
        public void ContextualPositioning_Format3_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType7Format3;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();
            string testStr = "\u0014\u0015\u0016"; // "\u0014\u0015\u0016" XPlacement plus 20.
            int[] expectedGlyphIndices = { 22, 23, 24 };

            FontRectangle[] expectedFontRectangles =
            {
                new(342, 2010, 978, 704),
                new(1842, 2011, 978, 708),
                new(3342, 2034, 978, 667),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType8SubTable, Format 1
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable
        [Fact]
        public void ChainedContextsPositioning_Format1_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType8Format1;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();

            // "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
            string testStr = "\u0014\u0015\u0016\u0017";
            int[] expectedGlyphIndices = { 22, 23, 24, 25 };
            FontRectangle[] expectedFontRectangles =
            {
                new(322, 2010, 978, 704),
                new(2022, 2011, 978, 708),
                new(3522, 2034, 978, 667),
                new(4822, 2030, 978, 671),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType8SubTable, Format 2
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable
        [Fact]
        public void ChainedContextsPositioning_Format2_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType8Format2;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();

            // "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
            string testStr = "\u0014\u0015\u0016\u0017";
            int[] expectedGlyphIndices = { 22, 23, 24, 25 };
            FontRectangle[] expectedFontRectangles =
            {
                new(322, 2010, 978, 704),
                new(2022, 2011, 978, 708),
                new(3522, 2034, 978, 667),
                new(4822, 2030, 978, 671),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        // LookupType8SubTable, Format 3
        // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#lookuptype-8-chained-contexts-positioning-subtable
        [Fact]
        public void ChainedContextsPositioning_Format3_Works()
        {
            // arrange
            string fontFile = TestFonts.GposLookupType8Format3;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();

            // "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
            string testStr = "\u0014\u0015\u0016\u0017";
            int[] expectedGlyphIndices = { 22, 23, 24, 25 };
            FontRectangle[] expectedFontRectangles =
            {
                new(322, 2010, 978, 704),
                new(2022, 2011, 978, 708),
                new(3522, 2034, 978, 667),
                new(4822, 2030, 978, 671),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        [Fact]
        public void MarkAnchoring_Works()
        {
            // arrange
            string fontFile = TestFonts.TimesNewRomanFile;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();

            string testStr = "\u0644\u0651"; // /lam-arab/arabicshaddacomb
            int[] expectedGlyphIndices = { 759, 989 };
            FontRectangle[] expectedFontRectangles =
            {
                new(246, 263.999878f, 363, 322),
                new(71, 324.999878f, 966, 1573),
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
            string fontFile = TestFonts.MeQuranFile;
            ushort upem = ReadFontUpem(fontFile);
            Font font = new FontCollection().Add(fontFile).CreateFont(upem);
            var renderer = new ColorGlyphRenderer();

            string testStr = "\u0631\u0651\u064E";
            int[] expectedGlyphIndices = { 47, 50, 23 };
            FontRectangle[] expectedFontRectangles =
            {
                new(345, 1398, 561, 461),
                new(420, 1837, 447, 414),
                new(40, 2514, 1002, 986),
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
                CompareRectangleExact(expectedFontRectangles[i], renderer.GlyphRects[i]);
            }
        }

        private static void CompareRectangle(FontRectangle expected, FontRectangle actual, int precision = 4)
        {
            Assert.Equal(expected.X, actual.X, precision);
            Assert.Equal(expected.Y, actual.Y, precision);
            Assert.Equal(expected.Width, actual.Width, precision);
            Assert.Equal(expected.Height, actual.Height, precision);
        }

        private static void CompareRectangleExact(FontRectangle expected, FontRectangle actual)
        {
            Assert.Equal(expected.X, actual.X);
            Assert.Equal(expected.Y, actual.Y);
            Assert.Equal(expected.Width, actual.Width);
            Assert.Equal(expected.Height, actual.Height);
        }

        private static ushort ReadFontUpem(string fileName)
        {
            // TODO: is there an easier way to read the UPEM? Maybe this is overcomplicated.
            FontFamily fontFamily = new FontCollection().Add(fileName);
            fontFamily.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

            // If no valid head table found, assume 1000, which matches typical Type1 usage.
            return metrics?.UnitsPerEm ?? 1000;
        }
    }
}
