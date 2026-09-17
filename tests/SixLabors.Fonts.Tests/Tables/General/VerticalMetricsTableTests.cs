// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tests.Tables.General;

public class VerticalMetricsTableTests
{
    // Three glyphs, two metric records: the last record's advance applies to the third glyph.
    private static readonly VerticalMetricsTable Table = new([700, 800], [10, 20, 30]);

    [Theory]
    [InlineData(0, 700)]
    [InlineData(1, 800)]
    [InlineData(2, 800)]
    public void AdvanceHeight_WithinTheGlyphCount(int glyphIndex, int expected)
        => Assert.Equal(expected, Table.GetAdvancedHeight(glyphIndex));

    [Theory]
    [InlineData(3)]
    [InlineData(65535)]
    public void AdvanceHeight_BeyondTheGlyphCountIsZero(int glyphIndex)
        => Assert.Equal(0, Table.GetAdvancedHeight(glyphIndex));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(2, 30)]
    public void TopSideBearing_WithinTheGlyphCount(int glyphIndex, int expected)
        => Assert.Equal(expected, Table.GetTopSideBearing(glyphIndex));

    [Fact]
    public void TopSideBearing_BeyondTheGlyphCountIsZero()
        => Assert.Equal(0, Table.GetTopSideBearing(3));
}
