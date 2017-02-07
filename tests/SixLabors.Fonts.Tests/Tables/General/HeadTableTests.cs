using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.WellKnownIds;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class HeadTableTests
    {
        [Fact]
        public void LoadHead()
        {
            var writer = new BinaryWriter();

            writer.WriteHeadTable(new HeadTable(HeadTable.Flags.None, 
                HeadTable.MacStyle.Italic | HeadTable.MacStyle.Bold, 
                1024, 
                new DateTime(2017, 02, 06, 07, 47, 00), 
                new DateTime(2017, 02, 07, 07, 47, 00), 
                new Point(0,0), 
                new Point(1024, 1022), 0, 1));

            var head = HeadTable.Load(writer.GetReader());

            Assert.Equal(HeadTable.Flags.None, head.flags);
            Assert.Equal(HeadTable.MacStyle.Italic | HeadTable.MacStyle.Bold, head.macStyle);
            Assert.Equal(1024, head.unitsPerEm);
            Assert.Equal(new DateTime(2017, 02, 06, 07, 47, 00), head.created);
            Assert.Equal(new DateTime(2017, 02, 07, 07, 47, 00), head.modified);
            Assert.Equal(0, head.min.X);
            Assert.Equal(0, head.min.Y);
            Assert.Equal(1024, head.max.X);
            Assert.Equal(1022, head.max.Y);
            Assert.Equal(0, head.lowestRecPPEM);
            Assert.Equal(1, head.indexToLocFormat);
        }
    }
}
