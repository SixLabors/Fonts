// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Defines the operand interpretation for a CFF DICT operator.
/// Used to describe how operands on the DICT stack should be decoded.
/// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5176.CFF.pdf"/>
/// </summary>
internal enum OperatorOperandKind
{
    /// <summary>
    /// A string identifier referencing the String INDEX.
    /// </summary>
    SID,

    /// <summary>
    /// A boolean value (0 or 1).
    /// </summary>
    Boolean,

    /// <summary>
    /// A single numeric value (integer or real).
    /// </summary>
    Number,

    /// <summary>
    /// An array of numeric values.
    /// </summary>
    Array,

    /// <summary>
    /// A delta-encoded array of numeric values.
    /// </summary>
    Delta,

    /// <summary>
    /// Two numeric values (e.g. Private DICT size and offset).
    /// </summary>
    NumberNumber,

    /// <summary>
    /// Two SIDs followed by a number (e.g. ROS: Registry, Ordering, Supplement).
    /// </summary>
    SID_SID_Number,
}
