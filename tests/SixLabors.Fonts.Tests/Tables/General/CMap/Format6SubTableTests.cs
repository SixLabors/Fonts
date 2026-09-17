// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tests.Tables.General.CMap;

public class Format6SubTableTests
{
    [Fact]
    public void LoadFormat6()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteCMapSubTable(
            new Format6SubTable(
                0,
                PlatformIDs.Windows,
                2,
                34,
                [17, 56, 12]));

        BigEndianBinaryReader reader = writer.GetReader();
        ushort format = reader.ReadUInt16(); // read format before we pass along as that's what the cmap table does
        Assert.Equal(6, format);

        Format6SubTable table = Format6SubTable.Load(
            new[] { new EncodingRecord(PlatformIDs.Windows, 2, 0) },
            reader).Single();

        Assert.Equal(0, table.Language);
        Assert.Equal(PlatformIDs.Windows, table.Platform);
        Assert.Equal(2, table.Encoding);
        Assert.Equal(34, table.FirstCode);
        Assert.Equal(new ushort[] { 17, 56, 12 }, table.GlyphIds);
    }

    [Theory]
    [InlineData(34, 17, true)]
    [InlineData(35, 56, true)]
    [InlineData(36, 12, true)]
    [InlineData(33, 0, false)] // before the range
    [InlineData(37, 0, false)] // after the range
    public void GetCharacter(int src, int expected, bool expectedFound)
    {
        Format6SubTable format = new(0, PlatformIDs.Windows, 2, 34, [17, 56, 12]);

        bool found = format.TryGetGlyphId(new CodePoint(src), out ushort id);

        Assert.Equal(expectedFound, found);
        Assert.Equal(expected, id);
    }

    [Fact]
    public void GetCharacter_ZeroGlyphIsMissing()
    {
        Format6SubTable format = new(0, PlatformIDs.Windows, 2, 34, [17, 0, 12]);

        bool found = format.TryGetGlyphId(new CodePoint(35), out ushort id);

        Assert.False(found);
        Assert.Equal(0, id);
    }

    [Fact]
    public void GetCodePoint()
    {
        Format6SubTable format = new(0, PlatformIDs.Windows, 2, 34, [17, 56, 12]);

        Assert.True(format.TryGetCodePoint(56, out CodePoint codePoint));
        Assert.Equal(35, codePoint.Value);
        Assert.False(format.TryGetCodePoint(99, out _));
    }
}
