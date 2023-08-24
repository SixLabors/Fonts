// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.IO;
using SixLabors.Fonts.Tables.General;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class VerticalHeadTableTests
    {
        [Fact]
        public void LoadVerticalHeadTable()
        {
            var writer = new BigEndianBinaryWriter();

            writer.WriteVerticalHeadTable(new VerticalHeadTable(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));

            var tbl = VerticalHeadTable.Load(writer.GetReader());

            Assert.Equal(1, tbl.Ascender);
            Assert.Equal(2, tbl.Descender);
            Assert.Equal(3, tbl.LineGap);
            Assert.Equal(4, tbl.AdvanceHeightMax);
            Assert.Equal(5, tbl.MinTopSideBearing);
            Assert.Equal(6, tbl.MinBottomSideBearing);
            Assert.Equal(7, tbl.YMaxExtent);
            Assert.Equal(8, tbl.CaretSlopeRise);
            Assert.Equal(9, tbl.CaretSlopeRun);
            Assert.Equal(10, tbl.CaretOffset);
            Assert.Equal(11, tbl.NumberOfVMetrics);
        }

        [Fact]
        public void ShouldReturnNullWhenTableCouldNotBeFound()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteTrueTypeFileHeader();

            using MemoryStream stream = writer.GetStream();
            Assert.Null(VerticalHeadTable.Load(new FontReader(stream)));
        }
    }
}
