// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Text;

namespace SixLabors.Fonts.Tables.Cff
{
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
            StringBuilder stbuilder = new();
            int j = this.Operands.Length;
            for (int i = 0; i < j; ++i)
            {
                if (i > 0)
                {
                    stbuilder.Append(" ");
                }

                stbuilder.Append(this.Operands[i].ToString());
            }

            stbuilder.Append(" ");
            stbuilder.Append(this.Operator?.ToString() ?? string.Empty);
            return stbuilder.ToString();
        }
#endif
    }
}
