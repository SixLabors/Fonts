// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.Kern;

namespace SixLabors.Fonts.Tests.Tables.General;

public class KerningTableTests
{
    [Fact]
    public void ShouldReturnDefaultValueWhenTableCouldNotBeFound()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            using var reader = new FontReader(stream);
            var table = KerningTable.Load(reader);
            Assert.NotNull(table);
        }
    }
}
