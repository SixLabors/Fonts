// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal enum OperatorOperandKind
{
    SID,
    Boolean,
    Number,
    Array,
    Delta,

    // Compound
    NumberNumber,
    SID_SID_Number,
}
