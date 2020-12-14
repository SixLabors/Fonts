// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.General.CMap;
using Xunit;
using static SixLabors.Fonts.Tables.General.CMap.Format4SubTable;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_104
    {
        [Fact]
        public void Format4SubTableWithSegmentsHasOffByOneWhenOverflowing()
        {
            Segment[] segments = new[]
            {
                new Segment(
                    0,
                    ushort.MaxValue, // end
                    ushort.MinValue, // start of range
                    short.MaxValue, // delta
                    0) // zero to force correctly tested codepath
            };
            var tbl = new Format4SubTable(
                0,
                WellKnownIds.PlatformIDs.Windows,
                0,
                segments,
                null);

            const int delta = short.MaxValue + 2; // extra 2 to handle the difference between ushort and short when offsettings

            const int codePoint = delta + 5;
            Assert.True(tbl.TryGetGlyphId(codePoint, out ushort gid));

            Assert.Equal(5, gid);
        }
    }
}
