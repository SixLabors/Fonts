// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tests.Tables.General.CMap;

public class Format10SubTableTests
{
    [Fact]
    public void LoadFormat10()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteCMapSubTable(
            new Format10SubTable(
                0,
                PlatformIDs.Windows,
                10,
                0x109423,
                [26, 27, 32]));

        BigEndianBinaryReader reader = writer.GetReader();
        ushort format = reader.ReadUInt16(); // read format before we pass along as that's what the cmap table does
        Assert.Equal(10, format);

        Format10SubTable table = Format10SubTable.Load(
            new[] { new EncodingRecord(PlatformIDs.Windows, 10, 0) },
            reader).Single();

        Assert.Equal(0U, table.Language);
        Assert.Equal(PlatformIDs.Windows, table.Platform);
        Assert.Equal(10, table.Encoding);
        Assert.Equal(0x109423U, table.StartCharCode);
        Assert.Equal(new ushort[] { 26, 27, 32 }, table.GlyphIds);
    }

    [Theory]
    [InlineData(0x109423, 26, true)]
    [InlineData(0x109424, 27, true)]
    [InlineData(0x109425, 32, true)]
    [InlineData(0x109422, 0, false)] // before the range
    [InlineData(0x109426, 0, false)] // after the range
    [InlineData(0x0021, 0, false)] // far below the range
    public void GetCharacter(int src, int expected, bool expectedFound)
    {
        Format10SubTable format = new(0, PlatformIDs.Windows, 10, 0x109423, [26, 27, 32]);

        bool found = format.TryGetGlyphId(new CodePoint(src), out ushort id);

        Assert.Equal(expectedFound, found);
        Assert.Equal(expected, id);
    }

    [Fact]
    public void GetCharacter_ZeroGlyphIsMissing()
    {
        Format10SubTable format = new(0, PlatformIDs.Windows, 10, 0x109423, [26, 0, 32]);

        bool found = format.TryGetGlyphId(new CodePoint(0x109424), out ushort id);

        Assert.False(found);
        Assert.Equal(0, id);
    }

    [Fact]
    public void GetCodePoint()
    {
        Format10SubTable format = new(0, PlatformIDs.Windows, 10, 0x109423, [26, 27, 32]);

        Assert.True(format.TryGetCodePoint(32, out CodePoint codePoint));
        Assert.Equal(0x109425, codePoint.Value);
        Assert.False(format.TryGetCodePoint(99, out _));
    }
}
