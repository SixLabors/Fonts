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
                .InstallCollection(TestFonts.HelveticaTTCFile)
                .First(x => x.IsStyleAvailable(FontStyle.Regular)).CreateFont(12);

            Glyph a = font.GetGlyph(new CodePoint('A'));
            Glyph x = font.GetGlyph(new CodePoint('x'));

            Assert.NotEqual(a, x);

            Assert.Equal(1366, a.GlyphMetrics.AdvanceWidth);
            Assert.Equal(2048, a.GlyphMetrics.AdvanceHeight);

            Assert.Equal(1024, x.GlyphMetrics.AdvanceWidth);
            Assert.Equal(2048, x.GlyphMetrics.AdvanceHeight);
        }
    }
}
