// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tests.Tables.General;

public class OS2TableTests
{
    [Fact]
    public void ShouldReturnNullWhenTableCouldNotBeFound()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            using var reader = new FontReader(stream);
            Assert.Null(OS2Table.Load(reader));
        }
    }
}
