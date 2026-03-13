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

    /// <summary>
    /// Initializes a new instance of the <see cref="CffIndexOffset"/> struct.
    /// </summary>
    /// <param name="start">The starting offset of the element.</param>
    /// <param name="len">The length in bytes of the element.</param>
    public CffIndexOffset(int start, int len)
    {
        this.Start = start;
        this.Length = len;
    }

#if DEBUG
    /// <inheritdoc/>
    public override string ToString() => "Start:" + this.Start + ",Length:" + this.Length;
#endif
}
