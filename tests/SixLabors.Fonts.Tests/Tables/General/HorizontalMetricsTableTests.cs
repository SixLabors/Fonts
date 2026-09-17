// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tests.Tables.General;

public class HorizontalMetricsTableTests
{
    // Three glyphs, two metric records: the last record's advance applies to the third glyph.
    private static readonly HorizontalMetricsTable Table = new([500, 600], [10, 20, 30]);

    [Theory]
    [InlineData(0, 500)]
    [InlineData(1, 600)]
    [InlineData(2, 600)]
    public void AdvanceWidth_WithinTheGlyphCount(int glyphIndex, int expected)
        => Assert.Equal(expected, Table.GetAdvancedWidth(glyphIndex));

    [Theory]
    [InlineData(3)]
    [InlineData(65535)]
    public void AdvanceWidth_BeyondTheGlyphCountIsZero(int glyphIndex)
        => Assert.Equal(0, Table.GetAdvancedWidth(glyphIndex));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(2, 30)]
    public void LeftSideBearing_WithinTheGlyphCount(int glyphIndex, int expected)
        => Assert.Equal(expected, Table.GetLeftSideBearing(glyphIndex));

    [Fact]
    public void LeftSideBearing_BeyondTheGlyphCountIsZero()
        => Assert.Equal(0, Table.GetLeftSideBearing(3));
}
