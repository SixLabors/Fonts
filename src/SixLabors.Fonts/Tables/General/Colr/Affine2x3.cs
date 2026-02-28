// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

// Affine matrices used by PaintTransform variants
internal readonly struct Affine2x3
{
    public readonly float Xx; // Fixed 16.16
    public readonly float Yx;
    public readonly float Xy;
    public readonly float Yy;
    public readonly float Dx;
    public readonly float Dy;

    public Affine2x3(float xx, float yx, float xy, float yy, float dx, float dy)
    {
        this.Xx = xx;
        this.Yx = yx;
        this.Xy = xy;
        this.Yy = yy;
        this.Dx = dx;
        this.Dy = dy;
    }
}
