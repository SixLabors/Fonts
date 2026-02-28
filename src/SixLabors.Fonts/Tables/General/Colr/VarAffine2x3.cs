// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

internal readonly struct VarAffine2x3
{
    public readonly float Xx;
    public readonly float Yx;
    public readonly float Xy;
    public readonly float Yy;
    public readonly float Dx;
    public readonly float Dy;
    public readonly uint VarIndexBase;

    public VarAffine2x3(float xx, float yx, float xy, float yy, float dx, float dy, uint varIndexBase)
    {
        this.Xx = xx;
        this.Yx = yx;
        this.Xy = xy;
        this.Yy = yy;
        this.Dx = dx;
        this.Dy = dy;
        this.VarIndexBase = varIndexBase;
    }
}
