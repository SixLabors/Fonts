// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    internal enum OperatorOperandKind
    {
        SID,
        Boolean,
        Number,
        Array,
        Delta,

        // compound
        NumberNumber,
        SID_SID_Number,
    }
}
