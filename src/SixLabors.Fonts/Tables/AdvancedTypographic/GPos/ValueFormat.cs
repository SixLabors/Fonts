// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// A ValueFormat flags field defines the types of positioning adjustment data that ValueRecords specify.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#value-record"/>
    /// </summary>
    [Flags]
    internal enum ValueFormat : ushort
    {
        // +--------+--------------------+---------------------------------------------+
        // | Mask   | Name               | Description                                 |
        // +========+====================+=============================================+
        // | 0x0001 | X_PLACEMENT        | Includes horizontal adjustment for          |
        // |        |                    | placement                                   |
        // +--------+--------------------+---------------------------------------------+
        // | 0x0002 | Y_PLACEMENT        | Includes vertical adjustment for placement  |
        // +--------+--------------------+---------------------------------------------+
        // | 0x0004 | X_ADVANCE          | Includes horizontal adjustment for          |
        // |        |                    | advance                                     |
        // +--------+--------------------+---------------------------------------------+
        // | 0x0008 | Y_ADVANCE          | Includes vertical adjustment for advance    |
        // +--------+--------------------+---------------------------------------------+
        // | 0x0010 | X_PLACEMENT_DEVICE | Includes Device table (non-variable font) / |
        // |        |                    | VariationIndex table (variable font) for    |
        // |        |                    | horizontal placement                        |
        // +--------+--------------------+---------------------------------------------+
        // | 0x0020 | Y_PLACEMENT_DEVICE | Includes Device table (non-variable font) / |
        // |        |                    | VariationIndex table (variable font) for    |
        // |        |                    | vertical placement                          |
        // +--------+--------------------+---------------------------------------------+
        // | 0x0040 | X_ADVANCE_DEVICE   | Includes Device table (non-variable font) / |
        // |        |                    | VariationIndex table (variable font) for    |
        // |        |                    | horizontal advance                          |
        // +--------+--------------------+---------------------------------------------+
        // | 0x0080 | Y_ADVANCE_DEVICE   | Includes Device table (non-variable font) / |
        // |        |                    | VariationIndex table (variable font) for    |
        // |        |                    | vertical advance                            |
        // +--------+--------------------+---------------------------------------------+
        // | 0xFF00 | Reserved           | For future use (set to zero)                |
        // +--------+--------------------+---------------------------------------------+
        XPlacement = 1,
        YPlacement = 1 << 1,
        XAdvance = 1 << 2,
        YAdvance = 1 << 3,
        XPlacementDevice = 1 << 4,
        YPlacementDevice = 1 << 5,
        XAdvanceDevice = 1 << 6,
        YAdvanceDevice = 1 << 7,
    }
}
