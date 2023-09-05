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

        using (System.IO.MemoryStream stream = writer.GetStream())
        {
            var table = KerningTable.Load(new FontReader(stream));
            Assert.NotNull(table);
        }
    }
}
