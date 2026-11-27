// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts;

/// <summary>
/// Represents the shaped bounds of a glyph. A mutable struct embedded in
/// <see cref="GlyphShapingPosition"/> and accessed by reference through
/// <see cref="GlyphShapingPosition.Bounds"/>: positioning lookups accumulate deltas into
/// the fields in place, and re-seeding is plain value assignment with no allocation.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal struct GlyphShapingBounds
{
    private int width;
    private int height;

    public GlyphShapingBounds(int x, int y, int width, int height)
    {
        this.X = x;
        this.Y = y;
        this.width = width;
        this.height = height;
        this.IsDirtyWH = false;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width
    {
        get => this.width;

        set
        {
            this.width = value;
            this.IsDirtyWH = true;
        }
    }

    public int Height
    {
        get => this.height;

        set
        {
            this.height = value;
            this.IsDirtyWH = true;
        }
    }

    /// <summary>
    /// Gets a value indicating whether positioning has written either advance dimension.
    /// Consumers use this to choose between the positioned advances and the metrics
    /// advances.
    /// </summary>
    public bool IsDirtyWH { get; private set; }

    /// <summary>
    /// Gets a value indicating whether either advance is outside the unsigned 16-bit range.
    /// </summary>
    public readonly bool HasOutOfRangeAdvance => (uint)this.width > ushort.MaxValue || (uint)this.height > ushort.MaxValue;

    private string DebuggerDisplay
        => FormattableString.Invariant($"{this.X} : {this.Y} : {this.Width} : {this.Height} : {this.IsDirtyWH}");
}
