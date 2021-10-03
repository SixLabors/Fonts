// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal sealed class CompositeGlyphLoader : GlyphLoader
    {
        private readonly Bounds bounds;
        private readonly byte[] instructions;
        private readonly Composite[] result;

        public CompositeGlyphLoader(IEnumerable<Composite> result, Bounds bounds, byte[] instructions)
        {
            this.result = result.ToArray();
            this.bounds = bounds;
            this.instructions = instructions;
        }

        public override GlyphVector CreateGlyph(GlyphTable table)
        {
            GlyphVector glyph = default;
            for (int resultIndex = 0; resultIndex < this.result.Length; resultIndex++)
            {
                ref Composite composite = ref this.result[resultIndex];
                glyph = GlyphVector.Append(glyph, GlyphVector.Transform(table.GetGlyph(composite.GlyphIndex), composite.Transformation), this.bounds);
            }

            // TODO: We're ignoring any composite glyph instructions for now and
            // instead are relying on the individual glyph instructions.
            return glyph;
        }

        public static CompositeGlyphLoader LoadCompositeGlyph(BigEndianBinaryReader reader, in Bounds bounds)
        {
            var result = new List<Composite>();
            CompositeGlyphFlags flags;
            do
            {
                flags = (CompositeGlyphFlags)reader.ReadUInt16();
                ushort glyphIndex = reader.ReadUInt16();

                LoadArguments(reader, flags, out int dx, out int dy);

                Matrix3x2 transform = Matrix3x2.Identity;
                transform.Translation = new Vector2(dx, dy);

                if (flags.HasFlag(CompositeGlyphFlags.WeHaveAScale))
                {
                    float scale = reader.ReadF2dot14(); // Format 2.14
                    transform.M11 = scale;
                    transform.M21 = scale;
                }
                else if (flags.HasFlag(CompositeGlyphFlags.WeHaveXAndYScale))
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }
                else if (flags.HasFlag(CompositeGlyphFlags.WeHaveATwoByTwo))
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M12 = reader.ReadF2dot14();
                    transform.M21 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }

                result.Add(new Composite(glyphIndex, transform));
            }
            while (flags.HasFlag(CompositeGlyphFlags.MoreComponents));

            byte[] instructions = Array.Empty<byte>();
            if (flags.HasFlag(CompositeGlyphFlags.WeHaveInstructions))
            {
                ushort instructionSize = reader.ReadUInt16();
                instructions = reader.ReadUInt8Array(instructionSize);
            }

            return new CompositeGlyphLoader(result, bounds, instructions);
        }

        public static void LoadArguments(BigEndianBinaryReader reader, CompositeGlyphFlags flags, out int dx, out int dy)
        {
            // are we 16 or 8 bits values?
            if (flags.HasFlag(CompositeGlyphFlags.Args1And2AreWords))
            {
                // 16 bit
                // are we int or unit?
                if (flags.HasFlag(CompositeGlyphFlags.ArgsAreXYValues))
                {
                    // signed
                    dx = reader.ReadInt16();
                    dy = reader.ReadInt16();
                }
                else
                {
                    // unsigned
                    dx = reader.ReadUInt16();
                    dy = reader.ReadUInt16();
                }
            }
            else
            {
                // 8 bit
                // are we sbyte or byte?
                if (flags.HasFlag(CompositeGlyphFlags.ArgsAreXYValues))
                {
                    // signed
                    dx = reader.ReadSByte();
                    dy = reader.ReadSByte();
                }
                else
                {
                    // unsigned
                    dx = reader.ReadByte();
                    dy = reader.ReadByte();
                }
            }
        }

        public readonly struct Composite
        {
            public Composite(ushort glyphIndex, Matrix3x2 transformation)
            {
                this.GlyphIndex = glyphIndex;
                this.Transformation = transformation;
            }

            public ushort GlyphIndex { get; }

            public Matrix3x2 Transformation { get; }
        }
    }
}
