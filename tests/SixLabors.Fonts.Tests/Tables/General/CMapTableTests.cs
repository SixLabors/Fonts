using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.WellKnownIds;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class CMapTableTests
    {
        [Fact]
        public void LoadFormat0()
        {
            var writer = new BinaryWriter();

            writer.WriteCMapTable(new []{
                new SixLabors.Fonts.Tables.General.CMap.Format0SubTable(0, PlatformIDs.Windows, 9, new byte[] { 0, 1, 2 })
            });

            var table = CMapTable.Load(writer.GetReader());

            Assert.Equal(1, table.Tables.Where(x=>x != null).Count());

            var format0Tables = table.Tables.OfType<Format0SubTable>().ToArray();
            Assert.Equal(1, format0Tables.Length);
        }       
    }
}
