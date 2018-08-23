
using SixLabors.Fonts.Tables.General;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class HoizontalHeadTableTests
    {
        [Fact]
        public void LoadHoizontalHeadTable()
        {
            var writer = new BinaryWriter();

            writer.WriteHoizontalHeadTable(new HoizontalHeadTable(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));

            HoizontalHeadTable tbl = HoizontalHeadTable.Load(writer.GetReader());

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
    }
}
