// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a COLR v1 ClipBox format 2 subtable with variation-aware int16 bounding box edges.
/// Each edge value is adjusted by a delta resolved from the ItemVariationStore.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#clipbox"/>
/// </summary>
internal sealed class ClipBoxFormat2 : ClipBox
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
    /// The base index into the ItemVariationStore delta sets for the four edge values.
    /// </summary>
    private readonly uint varIndexBase;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipBoxFormat2"/> class.
    /// </summary>
    /// <param name="xMin">The minimum x-coordinate.</param>
    /// <param name="yMin">The minimum y-coordinate.</param>
    /// <param name="xMax">The maximum x-coordinate.</param>
    /// <param name="yMax">The maximum y-coordinate.</param>
    /// <param name="varIndexBase">The base index into the ItemVariationStore delta sets.</param>
    public ClipBoxFormat2(short xMin, short yMin, short xMax, short yMax, uint varIndexBase)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
        this.varIndexBase = varIndexBase;
    }

    /// <inheritdoc/>
    public override Bounds GetBounds(ColrTable colr, GlyphVariationProcessor? processor)
    {
        float dx0 = colr.ResolveDelta(processor, this.varIndexBase + 0u);
        float dy0 = colr.ResolveDelta(processor, this.varIndexBase + 1u);
        float dx1 = colr.ResolveDelta(processor, this.varIndexBase + 2u);
        float dy1 = colr.ResolveDelta(processor, this.varIndexBase + 3u);

        float xMin = this.xMin + dx0;
        float yMin = this.yMin + dy0;
        float xMax = this.xMax + dx1;
        float yMax = this.yMax + dy1;

        return new Bounds(xMin, yMin, xMax, yMax);
    }
}
