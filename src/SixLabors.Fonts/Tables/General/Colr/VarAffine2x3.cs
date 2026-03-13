// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents a variation-aware 2x3 affine transformation matrix used by COLR v1 PaintVarTransform operations.
/// Values are stored as Fixed 16.16 numbers with an associated variation index base for font variations.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-12-and-13-painttransform-paintvartransform"/>
/// </summary>
internal readonly struct VarAffine2x3
{
    /// <summary>
    /// The x-component of the x-basis vector.
    /// </summary>
    public readonly float Xx;

    /// <summary>
    /// The y-component of the x-basis vector.
    /// </summary>
    public readonly float Yx;

    /// <summary>
    /// The x-component of the y-basis vector.
    /// </summary>
    public readonly float Xy;

    /// <summary>
    /// The y-component of the y-basis vector.
    /// </summary>
    public readonly float Yy;

    /// <summary>
    /// The x-translation component.
    /// </summary>
    public readonly float Dx;

    /// <summary>
    /// The y-translation component.
    /// </summary>
    public readonly float Dy;

    /// <summary>
    /// The base index into the ItemVariationStore delta sets for this transform's variation data.
    /// </summary>
    public readonly uint VarIndexBase;

    /// <summary>
    /// Initializes a new instance of the <see cref="VarAffine2x3"/> struct.
    /// </summary>
    /// <param name="xx">The x-component of the x-basis vector.</param>
    /// <param name="yx">The y-component of the x-basis vector.</param>
    /// <param name="xy">The x-component of the y-basis vector.</param>
    /// <param name="yy">The y-component of the y-basis vector.</param>
    /// <param name="dx">The x-translation component.</param>
    /// <param name="dy">The y-translation component.</param>
    /// <param name="varIndexBase">The base index into the ItemVariationStore delta sets.</param>
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
