// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tests.Tables.General;

public class HorizontalHeadTableTests
{
    [Fact]
    public void LoadHorizontalHeadTable()
    {
        var writer = new BigEndianBinaryWriter();

        writer.WriteHorizontalHeadTable(new HorizontalHeadTable(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));

        var tbl = HorizontalHeadTable.Load(writer.GetReader());

        Assert.Equal(1, tbl.Ascender);
        Assert.Equal(2, tbl.Descender);
        Assert.Equal(3, tbl.LineGap);
        Assert.Equal(4, tbl.AdvanceWidthMax);
        Assert.Equal(5, tbl.MinLeftSideBearing);
        Assert.Equal(6, tbl.MinRightSideBearing);
        Assert.Equal(7, tbl.XMaxExtent);
        Assert.Equal(8, tbl.CaretSlopeRise);
        Assert.Equal(9, tbl.CaretSlopeRun);
        Assert.Equal(10, tbl.CaretOffset);
        Assert.Equal(11, tbl.NumberOfHMetrics);
    }

    [Fact]
    public void ShouldReturnNullWhenTableCouldNotBeFound()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            using var reader = new FontReader(stream);
            Assert.Null(HorizontalHeadTable.Load(reader));
        }
    }
}
