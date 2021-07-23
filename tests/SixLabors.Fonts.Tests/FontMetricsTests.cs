// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontMetricsTests
    {
        [Fact]
        public void FontMetricsMatchesReference()
        {
            var collection = new FontCollection();
            FontFamily family = collection.Install(TestFonts.OpenSansFile);
            Font font = family.CreateFont(12);

            // https://fontdrop.info/
            Assert.Equal(2048, font.FontMetrics.UnitsPerEm);
            Assert.Equal(2189, font.FontMetrics.Ascender);
            Assert.Equal(-600, font.FontMetrics.Descender);
            Assert.Equal(0, font.FontMetrics.LineGap);
            Assert.Equal(2476, font.FontMetrics.AdvanceWidthMax);
            Assert.Equal(font.FontMetrics.LineHeight, font.FontMetrics.AdvanceHeightMax);
        }
    }
}
