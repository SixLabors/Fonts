// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tests.Tables.General;

public class MaximumProfileTableTests
{
    [Fact]
    public void ShouldThrowExceptionWhenTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            InvalidFontTableException exception = Assert.Throws<InvalidFontTableException>(() =>
            {
                using FontReader reader = new(stream);
                MaximumProfileTable.Load(reader);
            });

            Assert.Equal("maxp", exception.Table);
        }
    }
}
