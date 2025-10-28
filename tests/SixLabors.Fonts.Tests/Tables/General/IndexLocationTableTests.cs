// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.TrueType;

namespace SixLabors.Fonts.Tests.Tables.General;

public class IndexLocationTableTests
{
    [Fact]
    public void ShouldThrowExceptionWhenHeadTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            MissingFontTableException exception = Assert.Throws<MissingFontTableException>(() =>
            {
                using FontReader reader = new(stream);
                IndexLocationTable.Load(reader);
            });

            Assert.Equal("head", exception.Table);
        }
    }

    [Fact]
    public void ShouldThrowExceptionWhenMaximumProfileTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader(new TableHeader("head", 0, 0, 0));

        writer.WriteHeadTable(new HeadTable(
            HeadTable.HeadFlags.None,
            HeadTable.HeadMacStyle.Italic | HeadTable.HeadMacStyle.Bold,
            1024,
            new DateTime(2017, 02, 06, 07, 47, 00),
            new DateTime(2017, 02, 07, 07, 47, 00),
            new Bounds(0, 0, 1024, 1022),
            0,
            HeadTable.IndexLocationFormats.Offset16));

        using (MemoryStream stream = writer.GetStream())
        {
            InvalidFontTableException exception = Assert.Throws<InvalidFontTableException>(() =>
            {
                using FontReader reader = new(stream);
                IndexLocationTable.Load(reader);
            });

            Assert.Equal("maxp", exception.Table);
        }
    }

    [Fact]
    public void ShouldReturnNullWhenTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader(new TableHeader("head", 0, 0, 0), new TableHeader("maxp", 0, 0, 0));

        writer.WriteHeadTable(new HeadTable(
            HeadTable.HeadFlags.None,
            HeadTable.HeadMacStyle.Italic | HeadTable.HeadMacStyle.Bold,
            1024,
            new DateTime(2017, 02, 06, 07, 47, 00),
            new DateTime(2017, 02, 07, 07, 47, 00),
            new Bounds(0, 0, 1024, 1022),
            0,
            HeadTable.IndexLocationFormats.Offset16));

        using (MemoryStream stream = writer.GetStream())
        {
            using FontReader reader = new(stream);
            Assert.Null(IndexLocationTable.Load(reader));
        }
    }
}
