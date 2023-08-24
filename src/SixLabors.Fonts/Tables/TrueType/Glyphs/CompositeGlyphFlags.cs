// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs
{
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
    internal enum CompositeGlyphFlags : ushort
    {
        Args1And2AreWords = 1,    // If this is set, the arguments are words; otherwise, they are bytes.
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
}
