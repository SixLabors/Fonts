// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents the position and length of an element within a CFF INDEX structure.
/// </summary>
internal readonly struct CffIndexOffset
{
    /// <summary>
    /// The starting offset of the element within the INDEX data.
    /// </summary>
    public readonly int Start;

    /// <summary>
    /// The length in bytes of the element.
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
