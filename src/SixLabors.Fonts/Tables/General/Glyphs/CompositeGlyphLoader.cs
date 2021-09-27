// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal sealed class CompositeGlyphLoader : GlyphLoader
    {
        private readonly Bounds bounds;
        private readonly Composite[] result;

        public CompositeGlyphLoader(IEnumerable<Composite> result, Bounds bounds)
        {
            this.result = result.ToArray();
            this.bounds = bounds;
        }

        public override GlyphVector CreateGlyph(GlyphTable table)
        {
            var controlPoints = new List<Vector2>();
            var onCurves = new List<bool>();
            var endPoints = new List<ushort>();

            for (int resultIndex = 0; resultIndex < this.result.Length; resultIndex++)
            {
                ref Composite composite = ref this.result[resultIndex];

                GlyphVector glyph = table.GetGlyph(composite.GlyphIndex);
                int pointCount = glyph.PointCount;
                ushort endPointOffset = (ushort)controlPoints.Count;
                for (int i = 0; i < pointCount; i++)
                {
                    controlPoints.Add(Vector2.Transform(glyph.ControlPoints[i], composite.Transformation));
                    onCurves.Add(glyph.OnCurves[i]);
                }

                foreach (ushort p in glyph.EndPoints)
                {
                    endPoints.Add((ushort)(p + endPointOffset));
                }
            }

            return new GlyphVector(controlPoints.ToArray(), onCurves.ToArray(), endPoints.ToArray(), this.bounds);
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

            if (flags.HasFlag(CompositeGlyphFlags.WeHaveInstructions))
            {
                // TODO deal with instructions
            }

            return new CompositeGlyphLoader(result, bounds);
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
