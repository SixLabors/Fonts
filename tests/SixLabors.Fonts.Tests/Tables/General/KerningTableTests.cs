// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.General.Kern;

namespace SixLabors.Fonts.Tests.Tables.General;

public class KerningTableTests
{
    /// <summary>
    /// Verifies that an absent optional table produces an empty table.
    /// </summary>
    [Fact]
    public void ShouldReturnDefaultValueWhenTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            using FontReader reader = new(stream);
            KernTable table = KernTable.Load(reader);
            Assert.NotNull(table);
        }
    }

    /// <summary>
    /// Verifies class-pair offsets are resolved from format 2 kerning data.
    /// </summary>
    [Fact]
    public void ShouldReadFormat2ClassPairOffsets()
    {
        BigEndianBinaryWriter writer = new();

        // The class values are byte offsets: left selects a row and right selects a column.
        writer.WriteUInt16(0);
        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteUInt16(38);
        writer.WriteUInt16(0x0201);
        writer.WriteUInt16(4);
        writer.WriteUInt16(14);
        writer.WriteUInt16(22);
        writer.WriteUInt16(30);
        writer.WriteUInt16(10);
        writer.WriteUInt16(2);
        writer.WriteUInt16(30);
        writer.WriteUInt16(34);
        writer.WriteUInt16(20);
        writer.WriteUInt16(2);
        writer.WriteUInt16(0);
        writer.WriteUInt16(2);
        writer.Write((short)0);
        writer.Write((short)-10);
        writer.Write((short)-20);
        writer.Write((short)-30);

        using BigEndianBinaryReader reader = writer.GetReader();
        KernTable table = KernTable.Load(reader);

        Assert.True(table.TryGetKerningOffset(10, 21, out Vector2 firstRow));
        Assert.Equal(new Vector2(-10, 0), firstRow);
        Assert.True(table.TryGetKerningOffset(11, 20, out Vector2 secondRow));
        Assert.Equal(new Vector2(-20, 0), secondRow);
        Assert.True(table.TryGetKerningOffset(11, 21, out Vector2 lastValue));
        Assert.Equal(new Vector2(-30, 0), lastValue);
    }

    /// <summary>
    /// Verifies class-pair offsets are resolved from an Apple 1.0 kerning table.
    /// </summary>
    [Fact]
    public void ShouldReadAppleFormat2ClassPairOffsets()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteUInt32(0x00010000);
        writer.WriteUInt32(1);
        writer.WriteUInt32(40);
        writer.Write((byte)0);
        writer.Write((byte)2);
        writer.WriteUInt16(0);
        writer.WriteUInt16(4);
        writer.WriteUInt16(16);
        writer.WriteUInt16(24);
        writer.WriteUInt16(32);
        writer.WriteUInt16(10);
        writer.WriteUInt16(2);
        writer.WriteUInt16(32);
        writer.WriteUInt16(36);
        writer.WriteUInt16(20);
        writer.WriteUInt16(2);
        writer.WriteUInt16(0);
        writer.WriteUInt16(2);
        writer.Write((short)0);
        writer.Write((short)-10);
        writer.Write((short)-20);
        writer.Write((short)-30);

        using BigEndianBinaryReader reader = writer.GetReader();
        KernTable table = KernTable.Load(reader);

        Assert.True(table.TryGetKerningOffset(11, 21, out Vector2 result));
        Assert.Equal(new Vector2(-30, 0), result);
    }
}
