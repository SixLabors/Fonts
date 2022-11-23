// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontCodePointsTests
    {
        [Fact]
        public void TtfTest()
        {
            var collection = new FontCollection();
            FontFamily family = collection.Add(TestFonts.SimpleFontFile);
            Font font = family.CreateFont(12);

            IEnumerable<CodePoint> codePoints = font.GetAvailableCodePoints();
            IEnumerable<int> codePointValues = codePoints.Select(x => x.Value);

            // Compare with https://everythingfonts.com/ttfdump
            Assert.Equal(257, codePoints.Count());

            // Compare with https://fontdrop.info/
            Assert.Contains(0x0000, codePointValues);
            Assert.Contains(0x000D, codePointValues);
            Assert.Contains(0x0020, codePointValues);
            Assert.Contains(0x0041, codePointValues);
            Assert.Contains(0x0042, codePointValues);
            Assert.Contains(0x0061, codePointValues);
            Assert.Contains(0x0062, codePointValues);
            Assert.Contains(0xFFFF, codePointValues);

            var glyphIds = codePoints
                .SelectMany(x => font.GetGlyphs(x, ColorFontSupport.None))
                .Select(x => x.GlyphMetrics.GlyphId)
                .ToHashSet();

            // Compare with https://fontdrop.info/
            Assert.Equal(8, glyphIds.Count);
        }

        [Fact]
        public void WoffTest()
        {
            var collection = new FontCollection();
            FontFamily family = collection.Add(TestFonts.SimpleFontFileWoff);
            Font font = family.CreateFont(12);

            IEnumerable<CodePoint> codePoints = font.GetAvailableCodePoints();
            IEnumerable<int> codePointValues = codePoints.Select(x => x.Value);

            // Compare with https://everythingfonts.com/ttfdump
            Assert.Equal(257, codePoints.Count());

            // Compare with https://fontdrop.info/
            Assert.Contains(0x0000, codePointValues);
            Assert.Contains(0x000D, codePointValues);
            Assert.Contains(0x0020, codePointValues);
            Assert.Contains(0x0041, codePointValues);
            Assert.Contains(0x0042, codePointValues);
            Assert.Contains(0x0061, codePointValues);
            Assert.Contains(0x0062, codePointValues);
            Assert.Contains(0xFFFF, codePointValues);

            var glyphIds = codePoints
                .SelectMany(x => font.GetGlyphs(x, ColorFontSupport.None))
                .Select(x => x.GlyphMetrics.GlyphId)
                .ToHashSet();

            // Compare with https://fontdrop.info/
            Assert.Equal(8, glyphIds.Count);
        }
    }
}
