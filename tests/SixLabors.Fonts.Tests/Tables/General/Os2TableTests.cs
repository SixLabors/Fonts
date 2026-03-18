// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tests.Tables.General;

public class OS2TableTests
{
    [Fact]
    public void ShouldReturnNullWhenTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            using FontReader reader = new(stream);
            Assert.Null(OS2Table.Load(reader));
        }
    }
}
