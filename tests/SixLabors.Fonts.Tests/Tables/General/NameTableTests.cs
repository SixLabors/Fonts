// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tests.Tables.General;

public class NameTableTests
{
    [Fact]
    public void LoadFormat0()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteNameTable(
            new Dictionary<KnownNameIds, string>
            {
                { KnownNameIds.CopyrightNotice, "copyright" },
                { KnownNameIds.FullFontName, "fullname" },
                { KnownNameIds.FontFamilyName, "family" },
                { KnownNameIds.FontSubfamilyName, "subfamily" },
                { KnownNameIds.UniqueFontID, "id" },
                { (KnownNameIds)90, "other1" },
                { (KnownNameIds)91, "other2" }
            });

        NameTable table = NameTable.Load(writer.GetReader());

        Assert.Equal("fullname", table.FontName(CultureInfo.InvariantCulture));
        Assert.Equal("family", table.FontFamilyName(CultureInfo.InvariantCulture));
        Assert.Equal("subfamily", table.FontSubFamilyName(CultureInfo.InvariantCulture));
        Assert.Equal("id", table.Id(CultureInfo.InvariantCulture));
        Assert.Equal("copyright", table.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.CopyrightNotice));
        Assert.Equal("other1", table.GetNameById(CultureInfo.InvariantCulture, 90));
        Assert.Equal("other2", table.GetNameById(CultureInfo.InvariantCulture, 91));
    }

    [Fact]
    public void LoadFormat1()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteNameTable(
            new Dictionary<KnownNameIds, string>
            {
                { KnownNameIds.CopyrightNotice, "copyright" },
                { KnownNameIds.FullFontName, "fullname" },
                { KnownNameIds.FontFamilyName, "family" },
                { KnownNameIds.FontSubfamilyName, "subfamily" },
                { KnownNameIds.UniqueFontID, "id" },
                { (KnownNameIds)90, "other1" },
                { (KnownNameIds)91, "other2" }
            },
            [
                "lang1",
                "lang2"
            ]);

        NameTable table = NameTable.Load(writer.GetReader());

        Assert.Equal("fullname", table.FontName(CultureInfo.InvariantCulture));
        Assert.Equal("family", table.FontFamilyName(CultureInfo.InvariantCulture));
        Assert.Equal("subfamily", table.FontSubFamilyName(CultureInfo.InvariantCulture));
        Assert.Equal("id", table.Id(CultureInfo.InvariantCulture));
        Assert.Equal("copyright", table.GetNameById(CultureInfo.InvariantCulture, KnownNameIds.CopyrightNotice));
        Assert.Equal("other1", table.GetNameById(CultureInfo.InvariantCulture, 90));
        Assert.Equal("other2", table.GetNameById(CultureInfo.InvariantCulture, 91));
    }

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
                NameTable.Load(reader);
            });

            Assert.Equal("name", exception.Table);
        }
    }
}
