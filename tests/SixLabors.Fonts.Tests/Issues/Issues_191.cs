// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

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
            const TextAttribute attributes = TextAttribute.None;

            Glyph[] a = font.GetGlyphs(new CodePoint('A'), attributes, support).ToArray();
            Glyph[] x = font.GetGlyphs(new CodePoint('x'), attributes, support).ToArray();

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
