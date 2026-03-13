// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if DEBUG
using System.Globalization;
#endif

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a numeric operand value from a CFF DICT entry.
/// Operands can be integers or real numbers as encoded in the DICT data.
/// </summary>
internal readonly struct CffOperand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CffOperand"/> struct.
    /// </summary>
    /// <param name="number">The numeric value.</param>
    /// <param name="kind">The operand kind (integer or real).</param>
    public CffOperand(double number, OperandKind kind)
    {
        this.Kind = kind;
        this.RealNumValue = number;
    }

    /// <summary>
    /// Gets the kind of this operand (integer or real number).
    /// </summary>
    public readonly OperandKind Kind { get; }

    /// <summary>
    /// Gets the numeric value of this operand.
    /// </summary>
    public readonly double RealNumValue { get; }

#if DEBUG
    /// <inheritdoc/>
    public override string ToString()
        => this.Kind switch
        {
            OperandKind.IntNumber => ((int)this.RealNumValue).ToString(CultureInfo.InvariantCulture),
            _ => this.RealNumValue.ToString(CultureInfo.InvariantCulture),
        };
#endif

}
