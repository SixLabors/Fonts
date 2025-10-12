// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#pragma warning disable SA1201 // Elements should appear in the correct order
namespace SixLabors.Fonts.Tables.General.Colr;

// Format 1: int16 edges.
internal sealed class ClipBoxFormat1 : ClipBox
{
    private readonly short xMin;
    private readonly short yMin;
    private readonly short xMax;
    private readonly short yMax;

    public ClipBoxFormat1(short xMin, short yMin, short xMax, short yMax)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
    }

    public override Bounds GetBounds(IVariationResolver? varResolver)
        => new(this.xMin, this.yMin, this.xMax, this.yMax);
}
