// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using static SixLabors.Fonts.Tables.General.CMap.Format4SubTable;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_104
{
    [Fact]
    public void Format4SubTableWithSegmentsHasOffByOneWhenOverflowing()
    {
        Segment[] segments =
        [
            new(
                0,
                ushort.MaxValue, // end
                ushort.MinValue, // start of range
                short.MaxValue, // delta
                0) // zero to force correctly tested codepath
        ];
        Format4SubTable tbl = new(
            0,
            WellKnownIds.PlatformIDs.Windows,
            0,
            segments,
            null);

        const int delta = short.MaxValue + 2; // extra 2 to handle the difference between ushort and short when offsettings

        const int codePoint = delta + 5;
        Assert.True(tbl.TryGetGlyphId(new CodePoint(codePoint), out ushort gid));

        Assert.Equal(5, gid);
    }
}
