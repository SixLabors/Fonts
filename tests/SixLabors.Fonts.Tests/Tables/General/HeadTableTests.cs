using System;

using SixLabors.Fonts.Tables.General;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class HeadTableTests
    {
        [Fact]
        public void LoadHead()
        {
            var writer = new BinaryWriter();

            writer.WriteHeadTable(new HeadTable(HeadTable.HeadFlags.None,
                HeadTable.HeadMacStyle.Italic | HeadTable.HeadMacStyle.Bold,
                1024,
                new DateTime(2017, 02, 06, 07, 47, 00),
                new DateTime(2017, 02, 07, 07, 47, 00),
                new Bounds(0, 0, 1024, 1022), 0, HeadTable.IndexLocationFormats.Offset16));

            var head = HeadTable.Load(writer.GetReader());

            Assert.Equal(HeadTable.HeadFlags.None, head.Flags);
            Assert.Equal(HeadTable.HeadMacStyle.Italic | HeadTable.HeadMacStyle.Bold, head.MacStyle);
            Assert.Equal(1024, head.UnitsPerEm);
            Assert.Equal(new DateTime(2017, 02, 06, 07, 47, 00), head.Created);
            Assert.Equal(new DateTime(2017, 02, 07, 07, 47, 00), head.Modified);
            Assert.Equal(0, head.Bounds.Min.X);
            Assert.Equal(0, head.Bounds.Min.Y);
            Assert.Equal(1024, head.Bounds.Max.X);
            Assert.Equal(1022, head.Bounds.Max.Y);
            Assert.Equal(0, head.LowestRecPPEM);
            Assert.Equal(HeadTable.IndexLocationFormats.Offset16, head.IndexLocationFormat);
        }
    }
}
