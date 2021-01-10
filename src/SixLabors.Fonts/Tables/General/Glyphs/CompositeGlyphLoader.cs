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
        private readonly Composite[] result;

        public CompositeGlyphLoader(IEnumerable<Composite> result, Bounds bounds)
        {
            this.result = result.ToArray();
            this.bounds = bounds;
        }

        /*
         ## Composite Glyph Flags

        | Mask   | Name                     | Description
        |--------|--------------------------|--------------------
        | 0x0001 | ARG_1_AND_2_ARE_WORDS    | Bit 0: If this is set, the arguments are 16-bit (uint16 or int16); otherwise, they are bytes (uint8 or int8). 
        | 0x0002 | ARGS_ARE_XY_VALUES       | Bit 1: If this is set, the arguments are signed xy values; otherwise, they are unsigned point numbers.
        | 0x0004 | ROUND_XY_TO_GRID         | Bit 2: For the xy values if the preceding is true.
        | 0x0008 | WE_HAVE_A_SCALE          | Bit 3: This indicates that there is a simple scale for the component. Otherwise, scale = 1.0.
        | 0x0020 | MORE_COMPONENTS          | Bit 5: Indicates at least one more glyph after this one.
        | 0x0040 | WE_HAVE_AN_X_AND_Y_SCALE | Bit 6: The x direction will use a different scale from the y direction.
        | 0x0080 | WE_HAVE_A_TWO_BY_TWO     | Bit 7: There is a 2 by 2 transformation that will be used to scale the component.
        | 0x0100 | WE_HAVE_INSTRUCTIONS     | Bit 8: Following the last component are instructions for the composite character.
        | 0x0200 | USE_MY_METRICS           | Bit 9: If set, this forces the aw and lsb (and rsb) for the composite to be equal to those from this original glyph. This works for hinted and unhinted characters.
        | 0x0400 | OVERLAP_COMPOUND         | Bit 10: If set, the components of the compound glyph overlap. Use of this flag is not required in OpenType — that is, it is valid to have components overlap without having this flag set. It may affect behaviors in some platforms, however. (See Apple’s specification for details regarding behavior in Apple platforms.) When used, it must be set on the flag word for the first component. See additional remarks, above, for the similar OVERLAP_SIMPLE flag used in simple-glyph descriptions.
        | 0x0800 | SCALED_COMPONENT_OFFSET  | Bit 11: The composite is designed to have the component offset scaled.
        | 0x1000 | UNSCALED_COMPONENT_OFFSET| Bit 12: The composite is designed not to have the component offset scaled.
        | 0xE010 | Reserved                 | Bits 4, 13, 14 and 15 are reserved: set to 0.
         */
        [Flags]
        private enum CompositeFlags : ushort
        {
            ArgsAreWords = 1,    // If this is set, the arguments are words; otherwise, they are bytes.
            ArgsAreXYValues = 2, // If this is set, the arguments are xy values; otherwise, they are points.
            RoundXYToGrid = 4,   // For the xy values if the preceding is true.
            WeHaveAScale = 8,    // This indicates that there is a simple scale for the component. Otherwise, scale = 1.0.
            Reserved = 16,       // This bit is reserved. Set it to 0.
            MoreComponents = 32, // Indicates at least one more glyph after this one.
            WeHaveXAndYScale = 64, // The x direction will use a different scale from the y direction.
            WeHaveATwoByTwo = 128, // There is a 2 by 2 transformation that will be used to scale the component.
            WeHaveInstructions = 256, // Following the last component are instructions for the composite character.
            UseMyMetrics = 512,  // If set, this forces the aw and lsb (and rsb) for the composite to be equal to those from this original glyph. This works for hinted and unhinted characters.
            OverlapCompound = 1024,  // If set, the components of the compound glyph overlap. Use of this flag is not required in OpenType — that is, it is valid to have components overlap without having this flag set. It may affect behaviors in some platforms, however. (See Apple’s specification for details regarding behavior in Apple platforms.)
            ScaledComponentOffset = 2048, // The composite is designed to have the component offset scaled.
            UnscaledComponentOffset = 4096 // The composite is designed not to have the component offset scaled.
        }

        public override GlyphVector CreateGlyph(GlyphTable table)
        {
            var controlPoints = new List<Vector2>();
            var onCurves = new List<bool>();
            var endPoints = new List<ushort>();
            var minBounds = new List<Vector2>();
            var maxBounds = new List<Vector2>();
            var parts = new List<GlyphInstance>();

            for (int resultIndex = 0; resultIndex < this.result.Length; resultIndex++)
            {
                ref Composite composite = ref this.result[resultIndex];

                GlyphVector glyph = table.GetGlyph(composite.GlyphIndex);
                int pointcount = glyph.PointCount;
                ushort endPointOffset = (ushort)controlPoints.Count;
                for (int i = 0; i < pointcount; i++)
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
            CompositeFlags flags;
            ushort glyphIndex;
            do
            {
                flags = (CompositeFlags)reader.ReadUInt16();
                glyphIndex = reader.ReadUInt16();

                LoadArguments(reader, flags, out short dx, out short dy);

                Matrix3x2 transform = Matrix3x2.Identity;
                transform.Translation = new Vector2(dx, dy);
                if (flags.HasFlag(CompositeFlags.WeHaveAScale))
                {
                    float scale = reader.ReadF2dot14(); // Format 2.14
                    transform.M11 = scale;
                    transform.M21 = scale;
                }
                else if (flags.HasFlag(CompositeFlags.WeHaveXAndYScale))
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }
                else if (flags.HasFlag(CompositeFlags.WeHaveATwoByTwo))
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M12 = reader.ReadF2dot14();
                    transform.M21 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }

                result.Add(new Composite(glyphIndex, transform));
            }
            while (flags.HasFlag(CompositeFlags.MoreComponents));

            if (flags.HasFlag(CompositeFlags.WeHaveInstructions))
            {
                // TODO deal with instructions
            }

            return new CompositeGlyphLoader(result, bounds);
        }

        private static void LoadArguments(BigEndianBinaryReader reader, CompositeFlags flags, out short dx, out short dy)
        {
            // are we 16 or 8 bits values?
            if (flags.HasFlag(CompositeFlags.ArgsAreWords))
            {
                // 16 bit
                // are we int or unit?
                if (flags.HasFlag(CompositeFlags.ArgsAreXYValues))
                {
                    // signed
                    dx = reader.ReadInt16();
                    dy = reader.ReadInt16();
                }
                else
                {
                    // unsigned
                    dx = (short)reader.ReadUInt16();
                    dy = (short)reader.ReadUInt16();
                }
            }
            else
            {
                // 8 bit
                // are we sbyte or byte?
                if (flags.HasFlag(CompositeFlags.ArgsAreXYValues))
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
