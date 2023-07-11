// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs
{
    internal sealed class CompositeGlyphLoader : GlyphLoader
    {
        private readonly Bounds bounds;
        private readonly Composite[] composites;
        private readonly ReadOnlyMemory<byte> instructions;

        public CompositeGlyphLoader(IEnumerable<Composite> composites, Bounds bounds, ReadOnlyMemory<byte> instructions)
        {
            this.composites = composites.ToArray();
            this.bounds = bounds;
            this.instructions = instructions;
        }

        public override GlyphVector CreateGlyph(GlyphTable table)
        {
            List<ControlPoint> controlPoints = new();
            List<ushort> endPoints = new();
            for (int i = 0; i < this.composites.Length; i++)
            {
                Composite composite = this.composites[i];
                var clone = GlyphVector.DeepClone(table.GetGlyph(composite.GlyphIndex));
                GlyphVector.TransformInPlace(ref clone, composite.Transformation);
                ushort endPointOffset = (ushort)controlPoints.Count;

                controlPoints.AddRange(clone.ControlPoints);
                foreach (ushort p in clone.EndPoints)
                {
                    endPoints.Add((ushort)(p + endPointOffset));
                }
            }

            return new(controlPoints, endPoints, this.bounds, this.instructions, true);
        }

        public static CompositeGlyphLoader LoadCompositeGlyph(BigEndianBinaryReader reader, in Bounds bounds)
        {
            List<Composite> composites = new();
            CompositeGlyphFlags flags;
            do
            {
                flags = (CompositeGlyphFlags)reader.ReadUInt16();
                ushort glyphIndex = reader.ReadUInt16();

                LoadArguments(reader, flags, out int dx, out int dy);

                Matrix3x2 transform = Matrix3x2.Identity;
                transform.Translation = new Vector2(dx, dy);

                if ((flags & CompositeGlyphFlags.WeHaveAScale) != 0)
                {
                    float scale = reader.ReadF2dot14(); // Format 2.14
                    transform.M11 = scale;
                    transform.M22 = scale;
                }
                else if ((flags & CompositeGlyphFlags.WeHaveXAndYScale) != 0)
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }
                else if ((flags & CompositeGlyphFlags.WeHaveATwoByTwo) != 0)
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M12 = reader.ReadF2dot14();
                    transform.M21 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }

                composites.Add(new Composite(glyphIndex, flags, transform));
            }
            while ((flags & CompositeGlyphFlags.MoreComponents) != 0);

            byte[] instructions = Array.Empty<byte>();
            if ((flags & CompositeGlyphFlags.WeHaveInstructions) != 0)
            {
                // Read the instructions if they exist.
                ushort instructionSize = reader.ReadUInt16();
                instructions = reader.ReadUInt8Array(instructionSize);
            }

            return new CompositeGlyphLoader(composites, bounds, instructions);
        }

        public static void LoadArguments(BigEndianBinaryReader reader, CompositeGlyphFlags flags, out int dx, out int dy)
        {
            // are we 16 or 8 bits values?
            if ((flags & CompositeGlyphFlags.Args1And2AreWords) != 0)
            {
                // 16 bit
                // are we int or unit?
                if ((flags & CompositeGlyphFlags.ArgsAreXYValues) != 0)
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
                if ((flags & CompositeGlyphFlags.ArgsAreXYValues) != 0)
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
            public Composite(ushort glyphIndex, CompositeGlyphFlags flags, Matrix3x2 transformation)
            {
                this.GlyphIndex = glyphIndex;
                this.Flags = flags;
                this.Transformation = transformation;
            }

            public ushort GlyphIndex { get; }

            public CompositeGlyphFlags Flags { get; }

            public Matrix3x2 Transformation { get; }
        }
    }
}
