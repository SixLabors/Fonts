// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_191
    {
        [Fact]
        public void CanLoadMacintoshGlyphs()
        {
            Font font = new FontCollection()
                .AddCollection(TestFonts.HelveticaTTCFile)
                .First(x => x.GetAvailableStyles().Contains(FontStyle.Regular)).CreateFont(12);

            const ColorFontSupport support = ColorFontSupport.None;

            Assert.True(font.TryGetGlyphs(new CodePoint('A'), support, out IReadOnlyList<Glyph> glyphsA));
            Glyph[] a = glyphsA.ToArray();

            Assert.True(font.TryGetGlyphs(new CodePoint('x'), support, out IReadOnlyList<Glyph> glyphsX));
            Glyph[] x = glyphsX.ToArray();

            Glyph ga = Assert.Single(a);
            Glyph gx = Assert.Single(x);
            Assert.NotEqual(ga, gx);

            Assert.Equal(1366, ga.GlyphMetrics.AdvanceWidth);
            Assert.Equal(2048, ga.GlyphMetrics.AdvanceHeight);

            Assert.Equal(1024, gx.GlyphMetrics.AdvanceWidth);
            Assert.Equal(2048, gx.GlyphMetrics.AdvanceHeight);
        }
    }
}
