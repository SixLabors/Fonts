// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#pragma warning disable SA1201 // Elements should appear in the correct order
namespace SixLabors.Fonts.Tables.General.Colr;

// Format 2: int16 edges + varIndex per edge.
internal sealed class ClipBoxFormat2 : ClipBox
{
    private readonly short xMin;
    private readonly short yMin;
    private readonly short xMax;
    private readonly short yMax;
    private readonly uint varIndexBase;

    public ClipBoxFormat2(short xMin, short yMin, short xMax, short yMax, uint varIndexBase)
    {
        this.xMin = xMin;
        this.yMin = yMin;
        this.xMax = xMax;
        this.yMax = yMax;
        this.varIndexBase = varIndexBase;
    }

    public override Bounds GetBounds(IVariationResolver? varResolver)
    {
        float dx0 = varResolver?.ResolveDelta(this.varIndexBase + 0u) ?? 0f;
        float dy0 = varResolver?.ResolveDelta(this.varIndexBase + 1u) ?? 0f;
        float dx1 = varResolver?.ResolveDelta(this.varIndexBase + 2u) ?? 0f;
        float dy1 = varResolver?.ResolveDelta(this.varIndexBase + 3u) ?? 0f;

        float xMin = this.xMin + dx0;
        float yMin = this.yMin + dy0;
        float xMax = this.xMax + dx1;
        float yMax = this.yMax + dy1;

        return new Bounds(xMin, yMin, xMax, yMax);
    }
}
