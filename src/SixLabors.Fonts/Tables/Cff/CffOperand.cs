// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal readonly struct CffOperand
{
    public CffOperand(double number, OperandKind kind)
    {
        this.Kind = kind;
        this.RealNumValue = number;
    }

    public readonly OperandKind Kind { get; }

    public readonly double RealNumValue { get; }

#if DEBUG
    public override string ToString()
        => this.Kind switch
        {
            OperandKind.IntNumber => ((int)this.RealNumValue).ToString(),
            _ => this.RealNumValue.ToString(),
        };
#endif

}
