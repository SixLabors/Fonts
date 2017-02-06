using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.WellKnownIds;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General.CMap
{
    public class Format0SubTableTests
    {
        [Fact]
        public void LoadFormat0()
        {
            var writer = new BinaryWriter();

            //int subtableCount = 1;
            writer.WriteCMapSubTable(new SixLabors.Fonts.Tables.General.CMap.Format0SubTable(0, PlatformIDs.Windows, 2, new byte[] {
                1,2,3,4,5,6,7,8
            }));

            var reader = writer.GetReader();
            var format = reader.ReadUInt16(); // read format before we pass along as thats whet the cmap table does
            Assert.Equal(0, format);
            var table = Format0SubTable.Load(new EncodingRecord(PlatformIDs.Windows, 2, 0), reader);

            Assert.Equal(0, table.Language);
            Assert.Equal(PlatformIDs.Windows, table.Platform);
            Assert.Equal(2, table.Encoding);
            Assert.Equal(new byte[] {
                1,2,3,4,5,6,7,8
            }, table.glyphIds);
        }
    }
}
