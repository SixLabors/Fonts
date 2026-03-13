// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Abstract base class for all COLR v1 paint table nodes in the paint DAG.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#paint-tables"/>
/// </summary>
internal abstract class Paint
{
    /// <summary>
    /// Gets the paint format number identifying the type of this paint node.
    /// </summary>
    public byte Format { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintColrLayers (format 1), which references a contiguous range of paint
/// layers in the LayerList to compose a multi-layer color glyph.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#format-1-paintcolrlayers"/>
/// </summary>
internal sealed class PaintColrLayers : Paint
{
    /// <summary>
    /// Gets the number of layers to compose.
    /// </summary>
    public byte NumLayers { get; init; }

    /// <summary>
    /// Gets the index of the first paint in the LayerList.
    /// </summary>
    public uint FirstLayerIndex { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintSolid (format 2), which fills with a solid color from the CPAL palette.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-2-and-3-paintsolid-paintvarsolid"/>
/// </summary>
internal sealed class PaintSolid : Paint
{
    /// <summary>
    /// Gets the CPAL palette entry index. A value of 0xFFFF indicates the foreground color.
    /// </summary>
    public ushort PaletteIndex { get; init; }

    /// <summary>
    /// Gets the alpha value as an F2DOT14 number.
    /// </summary>
    public float Alpha { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintVarSolid (format 3), a variation-aware solid color fill.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-2-and-3-paintsolid-paintvarsolid"/>
/// </summary>
internal sealed class PaintVarSolid : Paint
{
    /// <summary>
    /// Gets the CPAL palette entry index. A value of 0xFFFF indicates the foreground color.
    /// </summary>
    public ushort PaletteIndex { get; init; }

    /// <summary>
    /// Gets the alpha value as an F2DOT14 number (varied via VarIndexBase + 0).
    /// </summary>
    public float Alpha { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintLinearGradient (format 4), which fills with a linear gradient
/// defined by three points and a color line.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-4-and-5-paintlineargradient-paintvarlineargradient"/>
/// </summary>
internal sealed class PaintLinearGradient : Paint
{
    /// <summary>
    /// Gets the color line defining the gradient stops and extend mode.
    /// </summary>
    public required ColorLine ColorLine { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the first gradient point (FWORD).
    /// </summary>
    public short X0 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the first gradient point (FWORD).
    /// </summary>
    public short Y0 { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the second gradient point (FWORD).
    /// </summary>
    public short X1 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the second gradient point (FWORD).
    /// </summary>
    public short Y1 { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the rotation point (FWORD).
    /// </summary>
    public short X2 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the rotation point (FWORD).
    /// </summary>
    public short Y2 { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintVarLinearGradient (format 5), a variation-aware linear gradient.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-4-and-5-paintlineargradient-paintvarlineargradient"/>
/// </summary>
internal sealed class PaintVarLinearGradient : Paint
{
    /// <summary>
    /// Gets the variable color line defining the gradient stops and extend mode.
    /// </summary>
    public required VarColorLine ColorLine { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the first gradient point (FWORD, var +0).
    /// </summary>
    public short X0 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the first gradient point (FWORD, var +1).
    /// </summary>
    public short Y0 { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the second gradient point (FWORD, var +2).
    /// </summary>
    public short X1 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the second gradient point (FWORD, var +3).
    /// </summary>
    public short Y1 { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the rotation point (FWORD, var +4).
    /// </summary>
    public short X2 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the rotation point (FWORD, var +5).
    /// </summary>
    public short Y2 { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintRadialGradient (format 6), which fills with a radial gradient
/// defined by two circles and a color line.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-6-and-7-paintradialgradient-paintvarradialgradient"/>
/// </summary>
internal sealed class PaintRadialGradient : Paint
{
    /// <summary>
    /// Gets the color line defining the gradient stops and extend mode.
    /// </summary>
    public required ColorLine ColorLine { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the first circle center (FWORD).
    /// </summary>
    public short X0 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the first circle center (FWORD).
    /// </summary>
    public short Y0 { get; init; }

    /// <summary>
    /// Gets the radius of the first circle (UFWORD).
    /// </summary>
    public ushort Radius0 { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the second circle center (FWORD).
    /// </summary>
    public short X1 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the second circle center (FWORD).
    /// </summary>
    public short Y1 { get; init; }

    /// <summary>
    /// Gets the radius of the second circle (UFWORD).
    /// </summary>
    public ushort Radius1 { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintVarRadialGradient (format 7), a variation-aware radial gradient.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-6-and-7-paintradialgradient-paintvarradialgradient"/>
/// </summary>
internal sealed class PaintVarRadialGradient : Paint
{
    /// <summary>
    /// Gets the variable color line defining the gradient stops and extend mode.
    /// </summary>
    public required VarColorLine ColorLine { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the first circle center (FWORD, var +0).
    /// </summary>
    public short X0 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the first circle center (FWORD, var +1).
    /// </summary>
    public short Y0 { get; init; }

    /// <summary>
    /// Gets the radius of the first circle (UFWORD, var +2).
    /// </summary>
    public ushort Radius0 { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the second circle center (FWORD, var +3).
    /// </summary>
    public short X1 { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the second circle center (FWORD, var +4).
    /// </summary>
    public short Y1 { get; init; }

    /// <summary>
    /// Gets the radius of the second circle (UFWORD, var +5).
    /// </summary>
    public ushort Radius1 { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintSweepGradient (format 8), which fills with a sweep (conical) gradient
/// around a center point between start and end angles.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-8-and-9-paintsweepgradient-paintvarsweepgradient"/>
/// </summary>
internal sealed class PaintSweepGradient : Paint
{
    /// <summary>
    /// Gets the color line defining the gradient stops and extend mode.
    /// </summary>
    public required ColorLine ColorLine { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the sweep center (FWORD).
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the sweep center (FWORD).
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets the start angle as an F2DOT14 value with bias per spec.
    /// </summary>
    public float StartAngle { get; init; }

    /// <summary>
    /// Gets the end angle as an F2DOT14 value with bias per spec.
    /// </summary>
    public float EndAngle { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintVarSweepGradient (format 9), a variation-aware sweep gradient.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-8-and-9-paintsweepgradient-paintvarsweepgradient"/>
/// </summary>
internal sealed class PaintVarSweepGradient : Paint
{
    /// <summary>
    /// Gets the variable color line defining the gradient stops and extend mode.
    /// </summary>
    public required VarColorLine ColorLine { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the sweep center (FWORD, var +0).
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the sweep center (FWORD, var +1).
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets the start angle as an F2DOT14 value (var +2).
    /// </summary>
    public float StartAngle { get; init; }

    /// <summary>
    /// Gets the end angle as an F2DOT14 value (var +3).
    /// </summary>
    public float EndAngle { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintGlyph (format 10), which binds a glyph outline to a child paint node.
/// The glyph outline serves as a clip path for the child paint.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#format-10-paintglyph"/>
/// </summary>
internal sealed class PaintGlyph : Paint
{
    /// <summary>
    /// Gets the child paint node that fills the glyph outline.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the glyph ID whose outline is used as the clip path.
    /// </summary>
    public ushort GlyphId { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintColrGlyph (format 11), which references another glyph's root paint
/// from the BaseGlyphList, allowing reuse of color glyph definitions.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#format-11-paintcolrglyph"/>
/// </summary>
internal sealed class PaintColrGlyph : Paint
{
    /// <summary>
    /// Gets the glyph ID whose root paint in the BaseGlyphList is referenced.
    /// </summary>
    public ushort GlyphId { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintTransform (format 12), which applies an affine transformation to its child paint.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-12-and-13-painttransform-paintvartransform"/>
/// </summary>
internal sealed class PaintTransform : Paint
{
    /// <summary>
    /// Gets the child paint node to transform.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the 2x3 affine transformation matrix.
    /// </summary>
    public Affine2x3 Transform { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintVarTransform (format 13), a variation-aware affine transformation.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-12-and-13-painttransform-paintvartransform"/>
/// </summary>
internal sealed class PaintVarTransform : Paint
{
    /// <summary>
    /// Gets the child paint node to transform.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the variation-aware 2x3 affine transformation matrix.
    /// </summary>
    public VarAffine2x3 Transform { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintTranslate (format 14), which translates its child paint by a fixed offset.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-14-and-15-painttranslate-paintvartranslate"/>
/// </summary>
internal sealed class PaintTranslate : Paint
{
    /// <summary>
    /// Gets the child paint node to translate.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the x-axis translation (FWORD).
    /// </summary>
    public short Dx { get; init; }

    /// <summary>
    /// Gets the y-axis translation (FWORD).
    /// </summary>
    public short Dy { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintVarTranslate (format 15), a variation-aware translation.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-14-and-15-painttranslate-paintvartranslate"/>
/// </summary>
internal sealed class PaintVarTranslate : Paint
{
    /// <summary>
    /// Gets the child paint node to translate.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the x-axis translation (FWORD, var +0).
    /// </summary>
    public short Dx { get; init; }

    /// <summary>
    /// Gets the y-axis translation (FWORD, var +1).
    /// </summary>
    public short Dy { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents COLR v1 scale paint operations (formats 16, 18, 20, 22), which apply a scale
/// transformation to their child paint. Supports uniform/anisotropic and around-center variants.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-16-to-23-paintscale-and-variants"/>
/// </summary>
internal sealed class PaintScale : Paint
{
    /// <summary>
    /// Gets the child paint node to scale.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the x-axis scale factor (F2DOT14).
    /// </summary>
    public float ScaleX { get; init; }

    /// <summary>
    /// Gets the y-axis scale factor (F2DOT14). Equal to ScaleX for uniform scale formats.
    /// </summary>
    public float ScaleY { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the scale center (FWORD). Zero if not an "around center" format.
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the scale center (FWORD). Zero if not an "around center" format.
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an "around center" scale format.
    /// </summary>
    public bool AroundCenter { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a uniform (isotropic) scale.
    /// </summary>
    public bool Uniform { get; init; }
}

/// <summary>
/// Represents COLR v1 variation-aware scale paint operations (formats 17, 19, 21, 23).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-16-to-23-paintscale-and-variants"/>
/// </summary>
internal sealed class PaintVarScale : Paint
{
    /// <summary>
    /// Gets the child paint node to scale.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the x-axis scale factor (var +0).
    /// </summary>
    public float ScaleX { get; init; }

    /// <summary>
    /// Gets the y-axis scale factor (var +1). Equal to ScaleX for uniform scale formats.
    /// </summary>
    public float ScaleY { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the scale center (var +2 if around center).
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the scale center (var +3 if around center).
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an "around center" scale format.
    /// </summary>
    public bool AroundCenter { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a uniform (isotropic) scale.
    /// </summary>
    public bool Uniform { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents COLR v1 rotate paint operations (formats 24, 26), which apply a rotation
/// to their child paint, optionally around a specified center point.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-24-to-27-paintrotate-and-variants"/>
/// </summary>
internal sealed class PaintRotate : Paint
{
    /// <summary>
    /// Gets the child paint node to rotate.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the rotation angle as an F2DOT14 value (1.0 = 180 degrees).
    /// </summary>
    public float Angle { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the rotation center (FWORD). Zero if not "around center".
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the rotation center (FWORD). Zero if not "around center".
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an "around center" rotation format.
    /// </summary>
    public bool AroundCenter { get; init; }
}

/// <summary>
/// Represents COLR v1 variation-aware rotate paint operations (formats 25, 27).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-24-to-27-paintrotate-and-variants"/>
/// </summary>
internal sealed class PaintVarRotate : Paint
{
    /// <summary>
    /// Gets the child paint node to rotate.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the rotation angle (var +0).
    /// </summary>
    public float Angle { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the rotation center (var +1 if around center).
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the rotation center (var +2 if around center).
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an "around center" rotation format.
    /// </summary>
    public bool AroundCenter { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents COLR v1 skew paint operations (formats 28, 30), which apply a skew transformation
/// to their child paint, optionally around a specified center point.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-28-to-31-paintskew-and-variants"/>
/// </summary>
internal sealed class PaintSkew : Paint
{
    /// <summary>
    /// Gets the child paint node to skew.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the x-axis skew angle as an F2DOT14 value.
    /// </summary>
    public float XSkew { get; init; }

    /// <summary>
    /// Gets the y-axis skew angle as an F2DOT14 value.
    /// </summary>
    public float YSkew { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the skew center (FWORD). Zero if not "around center".
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the skew center (FWORD). Zero if not "around center".
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an "around center" skew format.
    /// </summary>
    public bool AroundCenter { get; init; }
}

/// <summary>
/// Represents COLR v1 variation-aware skew paint operations (formats 29, 31).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#formats-28-to-31-paintskew-and-variants"/>
/// </summary>
internal sealed class PaintVarSkew : Paint
{
    /// <summary>
    /// Gets the child paint node to skew.
    /// </summary>
    public required Paint Child { get; init; }

    /// <summary>
    /// Gets the x-axis skew angle (var +0).
    /// </summary>
    public float XSkew { get; init; }

    /// <summary>
    /// Gets the y-axis skew angle (var +1).
    /// </summary>
    public float YSkew { get; init; }

    /// <summary>
    /// Gets the x-coordinate of the skew center (var +2 if around center).
    /// </summary>
    public short CenterX { get; init; }

    /// <summary>
    /// Gets the y-coordinate of the skew center (var +3 if around center).
    /// </summary>
    public short CenterY { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is an "around center" skew format.
    /// </summary>
    public bool AroundCenter { get; init; }

    /// <summary>
    /// Gets the base index into the ItemVariationStore delta sets.
    /// </summary>
    public uint VarIndexBase { get; init; }
}

/// <summary>
/// Represents a COLR v1 PaintComposite (format 32), which composites a source paint over a
/// backdrop paint using a specified Porter-Duff or blend mode.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#format-32-paintcomposite"/>
/// </summary>
internal sealed class PaintComposite : Paint
{
    /// <summary>
    /// Gets the composite mode used to blend the source over the backdrop.
    /// </summary>
    public ColrCompositeMode CompositeMode { get; init; }

    /// <summary>
    /// Gets the source paint node.
    /// </summary>
    public required Paint Source { get; init; }

    /// <summary>
    /// Gets the backdrop paint node.
    /// </summary>
    public required Paint Backdrop { get; init; }
}
