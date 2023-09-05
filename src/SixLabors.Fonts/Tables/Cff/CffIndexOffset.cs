// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

internal readonly struct CffIndexOffset
{
    /// <summary>
    /// The starting offset
    /// </summary>
    public readonly int Start;

    /// <summary>
    /// The length
    /// </summary>
    public readonly int Length;

    public CffIndexOffset(int start, int len)
    {
        this.Start = start;
        this.Length = len;
    }

#if DEBUG
    public override string ToString() => "Start:" + this.Start + ",Length:" + this.Length;
#endif
}
