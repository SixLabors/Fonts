// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Text;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class CffDataDicEntry
    {
        public CffOperand[] Operands;
        public CFFOperator Operator;

#if DEBUG
        public override string ToString()
        {

            StringBuilder stbuilder = new StringBuilder();
            int j = Operands.Length;
            for (int i = 0; i < j; ++i)
            {
                if (i > 0)
                {
                    stbuilder.Append(" ");
                }
                stbuilder.Append(Operands[i].ToString());
            }

            stbuilder.Append(" ");
            stbuilder.Append(Operator.ToString());
            return stbuilder.ToString();
        }
#endif
    }
}
