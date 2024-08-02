// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;

namespace SixLabors.Fonts.Tables.Cff;

internal class CffDataDicEntry
{
    public CffDataDicEntry(CFFOperator @operator, CffOperand[] operands)
    {
        this.Operator = @operator;
        this.Operands = operands;
    }

    public CFFOperator Operator { get; }

    public CffOperand[] Operands { get; }

#if DEBUG
    public override string ToString()
    {
        StringBuilder builder = new();
        int j = this.Operands.Length;
        for (int i = 0; i < j; ++i)
        {
            if (i > 0)
            {
                builder.Append(" ");
            }

            builder.Append(this.Operands[i].ToString());
        }

        builder.Append(" ");
        builder.Append(this.Operator?.ToString() ?? string.Empty);
        return builder.ToString();
    }
#endif
}
