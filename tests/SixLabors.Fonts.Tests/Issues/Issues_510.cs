// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using static SixLabors.Fonts.Tables.General.CMap.Format4SubTable;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_510
{
    [Fact]
    public void Format4SubTableWithOffsetSegmentReturnsFalseWhenGlyphIdIndexOverflows()
    {
        Segment[] segments =
        [
            new Segment(
                0,
                0,
                0,
                0,
                4)
        ];

        Format4SubTable tbl = new(
            0,
            WellKnownIds.PlatformIDs.Windows,
            0,
            segments,
            [1]);

        Assert.False(tbl.TryGetGlyphId(new CodePoint(0), out ushort gid));
        Assert.Equal(0, gid);
    }

    [Fact]
    public void Format4SubTableTryGetCodePointReturnsFalseWhenGlyphIdIndexOverflows()
    {
        Segment[] segments =
        [
            new Segment(
                0,
                0,
                0,
                0,
                4)
        ];

        Format4SubTable tbl = new(
            0,
            WellKnownIds.PlatformIDs.Windows,
            0,
            segments,
            [1]);

        Assert.False(tbl.TryGetCodePoint(1, out CodePoint codePoint));
        Assert.Equal(default, codePoint);
    }
}
