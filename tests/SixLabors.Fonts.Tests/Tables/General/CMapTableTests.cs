// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tests.Tables.General;

public class CMapTableTests
{
    [Fact]
    public void LoadFormat0()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteCMapTable(new[]
        {
            new Format0SubTable(0, PlatformIDs.Windows, 9, [0, 1, 2])
        });

        CMapTable table = CMapTable.Load(writer.GetReader());

        Assert.Single(table.Tables.Where(x => x != null));

        Format0SubTable[] format0Tables = table.Tables.OfType<Format0SubTable>().ToArray();
        Assert.Single(format0Tables);
    }

    [Fact]
    public void ShouldThrowExceptionWhenTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader();

        using (System.IO.MemoryStream stream = writer.GetStream())
        {
            InvalidFontTableException exception = Assert.Throws<InvalidFontTableException>(() =>
            {
                using FontReader reader = new(stream);
                CMapTable.Load(reader);
            });

            Assert.Equal("cmap", exception.Table);
        }
    }
}
