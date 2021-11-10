// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Glyphs;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General.Glyphs
{
    public class CompositeGlyphLoaderTests
    {
        [Fact]
        public void LoadSingleGlyphWithUInt16Offset_unsigned_short()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteUInt16((ushort)CompositeGlyphFlags.Args1And2AreWords); // 16bit unsigned
            writer.WriteUInt16(1); // glyph id

            writer.WriteUInt16(short.MaxValue + 1); // dx
            writer.WriteUInt16(short.MaxValue + 2); // dy
            writer.GetReader();

            var bounds = new Bounds(0, 0, 100, 100);
            var glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

            var tbl = new GlyphTable(new[]
            {
                new SimpleGlyphLoader(bounds), // padding
                new SimpleGlyphLoader(new short[] { 20 }, new short[] { 21 }, new[] { true }, new ushort[] { 1 }, bounds, Array.Empty<byte>())
            });

            GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

            Vector2 point = Assert.Single(finalGlyph.GetOutline().ControlPoints.ToArray());
            Assert.Equal(new Vector2(short.MaxValue + 1 + 20, short.MaxValue + 2 + 21), point);
        }

        [Fact]
        public void LoadSingleGlyphWithInt16Offset_signed_short()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteUInt16((ushort)(CompositeGlyphFlags.Args1And2AreWords /* 16bit */ | CompositeGlyphFlags.ArgsAreXYValues /* signed */)); // flags
            writer.WriteUInt16(1); // glyph id

            writer.WriteInt16(short.MinValue + 1); // dx
            writer.WriteInt16(short.MinValue + 2); // dy
            writer.GetReader();

            var bounds = new Bounds(0, 0, 100, 100);
            var glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

            var tbl = new GlyphTable(new[]
            {
                new SimpleGlyphLoader(bounds), // padding
                new SimpleGlyphLoader(new short[] { 20 }, new short[] { 21 }, new[] { true }, new ushort[] { 1 }, bounds, Array.Empty<byte>())
            });

            GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

            Vector2 point = Assert.Single(finalGlyph.GetOutline().ControlPoints.ToArray());
            Assert.Equal(new Vector2(short.MinValue + 1 + 20, short.MinValue + 2 + 21), point);
        }

        [Fact]
        public void LoadSingleGlyphWithUInt8Offset_unsigned_byte()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteUInt16(0); // 8bit unsigned
            writer.WriteUInt16(1); // glyph id

            writer.WriteUInt8(sbyte.MaxValue + 1); // dx
            writer.WriteUInt8(sbyte.MaxValue + 2); // dy
            writer.GetReader();

            var bounds = new Bounds(0, 0, 100, 100);
            var glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

            var tbl = new GlyphTable(new[]
            {
                new SimpleGlyphLoader(bounds), // padding
                new SimpleGlyphLoader(new short[] { 20 }, new short[] { 21 }, new[] { true }, new ushort[] { 1 }, bounds, Array.Empty<byte>())
            });

            GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

            Vector2 point = Assert.Single(finalGlyph.GetOutline().ControlPoints.ToArray());
            Assert.Equal(new Vector2(sbyte.MaxValue + 1 + 20, sbyte.MaxValue + 2 + 21), point);
        }

        [Fact]
        public void LoadSingleGlyphWithInt8Offset_signed_byte()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteUInt16((ushort)CompositeGlyphFlags.ArgsAreXYValues); // signed byte
            writer.WriteUInt16(1); // glyph id

            writer.WriteInt8(sbyte.MinValue + 1); // dx
            writer.WriteInt8(sbyte.MinValue + 2); // dy
            writer.GetReader();

            var bounds = new Bounds(0, 0, 100, 100);
            var glyph = CompositeGlyphLoader.LoadCompositeGlyph(writer.GetReader(), in bounds);

            var tbl = new GlyphTable(new[]
            {
                new SimpleGlyphLoader(bounds), // padding
                new SimpleGlyphLoader(new short[] { 20 }, new short[] { 21 }, new[] { true }, new ushort[] { 1 }, bounds, Array.Empty<byte>())
            });

            GlyphVector finalGlyph = glyph.CreateGlyph(tbl);

            Vector2 point = Assert.Single(finalGlyph.GetOutline().ControlPoints.ToArray());
            Assert.Equal(new Vector2(sbyte.MinValue + 1 + 20, sbyte.MinValue + 2 + 21), point);
        }
    }
}
