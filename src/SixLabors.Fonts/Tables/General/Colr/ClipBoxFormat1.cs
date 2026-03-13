// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a COLR v1 ClipBox format 1 subtable with static (non-variable) int16 bounding box edges.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#clipbox"/>
/// </summary>
internal sealed class ClipBoxFormat1 : ClipBox
{
    /// <summary>
    /// The minimum x-coordinate of the clip box.
    /// </summary>
    private readonly short xMin;

    /// <summary>
    /// The minimum y-coordinate of the clip box.
    /// </summary>
    private readonly short yMin;

    /// <summary>
    /// The maximum x-coordinate of the clip box.
    /// </summary>
    private readonly short xMax;

    /// <summary>
    /// The maximum y-coordinate of the clip box.
    /// </summary>
    private readonly short yMax;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipBoxFormat1"/> class.
    /// </summary>
    /// <param name="xMin">The minimum x-coordinate.</param>
    /// <param name="yMin">The minimum y-coordinate.</param>
    /// <param name="xMax">The maximum x-coordinate.</param>
    /// <param name="yMax">The maximum y-coordinate.</param>
    public ClipBoxFormat1(short xMin, short yMin, short xMax, short yMax)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
    }

    /// <inheritdoc/>
    public override Bounds GetBounds(ColrTable colr, GlyphVariationProcessor? processor)
        => new(this.xMin, this.yMin, this.xMax, this.yMax);
}
