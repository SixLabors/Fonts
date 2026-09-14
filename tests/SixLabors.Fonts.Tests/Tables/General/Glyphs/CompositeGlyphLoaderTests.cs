// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tests.Tables.General.Glyphs;

public class CompositeGlyphLoaderTests
{
    [Fact]
    public void LoadSingleGlyphWithUInt16Offset_unsigned_short()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteUInt16((ushort)CompositeGlyphFlags.Args1And2AreWords); // 16bit unsigned
        writer.WriteUInt16(1); // glyph id

        writer.WriteUInt16(short.MaxValue + 1); // dx
        writer.WriteUInt16(short.MaxValue + 2); // dy
        writer.GetReader();

        Bounds bounds = new(0, 0, 100, 100);
        CompositeGlyphLoader glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

        GlyphTable tbl = new([
            new SimpleGlyphLoader(bounds), // padding
            new SimpleGlyphLoader([new ControlPoint(new Vector2(20, 21), true)], [1], bounds, [])
        ]);

        GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

        ControlPoint point = Assert.Single(finalGlyph.ControlPoints);
        Assert.Equal(new Vector2(short.MaxValue + 1 + 20, short.MaxValue + 2 + 21), point.Point);
    }

    [Fact]
    public void LoadSingleGlyphWithInt16Offset_signed_short()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteUInt16((ushort)(CompositeGlyphFlags.Args1And2AreWords /* 16bit */ | CompositeGlyphFlags.ArgsAreXYValues /* signed */)); // flags
        writer.WriteUInt16(1); // glyph id

        writer.WriteInt16(short.MinValue + 1); // dx
        writer.WriteInt16(short.MinValue + 2); // dy
        writer.GetReader();

        Bounds bounds = new(0, 0, 100, 100);
        CompositeGlyphLoader glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

        GlyphTable tbl = new([
            new SimpleGlyphLoader(bounds), // padding
            new SimpleGlyphLoader([new ControlPoint(new Vector2(20, 21), true)], [1], bounds, [])
        ]);

        GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

        ControlPoint point = Assert.Single(finalGlyph.ControlPoints);
        Assert.Equal(new Vector2(short.MinValue + 1 + 20, short.MinValue + 2 + 21), point.Point);
    }

    [Fact]
    public void LoadSingleGlyphWithUInt8Offset_unsigned_byte()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteUInt16(0); // 8bit unsigned
        writer.WriteUInt16(1); // glyph id

        writer.WriteUInt8(sbyte.MaxValue + 1); // dx
        writer.WriteUInt8(sbyte.MaxValue + 2); // dy
        writer.GetReader();

        Bounds bounds = new(0, 0, 100, 100);
        CompositeGlyphLoader glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

        GlyphTable tbl = new([
            new SimpleGlyphLoader(bounds), // padding
            new SimpleGlyphLoader([new ControlPoint(new Vector2(20, 21), true)], [1], bounds, [])
        ]);

        GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

        ControlPoint point = Assert.Single(finalGlyph.ControlPoints);
        Assert.Equal(new Vector2(sbyte.MaxValue + 1 + 20, sbyte.MaxValue + 2 + 21), point.Point);
    }

    [Fact]
    public void LoadSingleGlyphWithInt8Offset_signed_byte()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteUInt16((ushort)CompositeGlyphFlags.ArgsAreXYValues); // signed byte
        writer.WriteUInt16(1); // glyph id

        writer.WriteInt8(sbyte.MinValue + 1); // dx
        writer.WriteInt8(sbyte.MinValue + 2); // dy
        writer.GetReader();

        Bounds bounds = new(0, 0, 100, 100);
        CompositeGlyphLoader glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

        GlyphTable tbl = new([
            new SimpleGlyphLoader(bounds), // padding
            new SimpleGlyphLoader([new ControlPoint(new Vector2(20, 21), true)], [1], bounds, [])
        ]);

        GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

        ControlPoint point = Assert.Single(finalGlyph.ControlPoints);
        Assert.Equal(new Vector2(sbyte.MinValue + 1 + 20, sbyte.MinValue + 2 + 21), point.Point);
    }
}
