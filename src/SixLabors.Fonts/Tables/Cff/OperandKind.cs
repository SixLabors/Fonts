// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Identifies whether a CFF DICT operand was encoded as an integer or a real number.
/// </summary>
internal enum OperandKind
{
    /// <summary>
    /// An integer operand (encoded as 1-5 bytes in the DICT data).
    /// </summary>
    IntNumber,

    /// <summary>
    /// A real number operand (encoded as a nibble-based BCD sequence).
    /// </summary>
    RealNumber
}
