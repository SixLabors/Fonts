// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.Kern;

namespace SixLabors.Fonts.Tests.Tables.General;

public class KerningTableTests
{
    [Fact]
    public void ShouldReturnDefaultValueWhenTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            using FontReader reader = new(stream);
            KerningTable table = KerningTable.Load(reader);
            Assert.NotNull(table);
        }
    }
}
