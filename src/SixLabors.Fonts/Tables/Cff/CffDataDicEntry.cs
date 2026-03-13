// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if DEBUG
using System.Text;
#endif

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a single key-value entry parsed from a CFF DICT structure.
/// The key is a <see cref="CFFOperator"/> and the value is an array of <see cref="CffOperand"/>.
/// </summary>
internal class CffDataDicEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CffDataDicEntry"/> class.
    /// </summary>
    /// <param name="operator">The DICT operator.</param>
    /// <param name="operands">The operand values for this operator.</param>
    public CffDataDicEntry(CFFOperator @operator, CffOperand[] operands)
    {
        this.Operator = @operator;
        this.Operands = operands;
    }

    /// <summary>
    /// Gets the DICT operator that identifies this entry.
    /// </summary>
    public CFFOperator Operator { get; }

    /// <summary>
    /// Gets the operand values associated with this operator.
    /// </summary>
    public CffOperand[] Operands { get; }

#if DEBUG
    /// <inheritdoc/>
    public override string ToString()
    {
        StringBuilder builder = new();
        int j = this.Operands.Length;
        for (int i = 0; i < j; ++i)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(this.Operands[i].ToString());
        }

        builder.Append(' ')
               .Append(this.Operator?.ToString() ?? string.Empty);
        return builder.ToString();
    }
#endif
}
