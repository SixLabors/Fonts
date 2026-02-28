// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

// Base node
internal abstract class Paint
{
    public byte Format { get; init; }
}

// Format 1: PaintColrLayers
internal sealed class PaintColrLayers : Paint
{
    public byte NumLayers { get; init; } // uint8

    public uint FirstLayerIndex { get; init; } // uint32 into LayerList
}

// Formats 2/3: PaintSolid / PaintVarSolid
internal sealed class PaintSolid : Paint
{
    public ushort PaletteIndex { get; init; } // uint16

    public float Alpha { get; init; } // F2DOT14
}

internal sealed class PaintVarSolid : Paint
{
    public ushort PaletteIndex { get; init; } // uint16

    public float Alpha { get; init; } // F2DOT14 (var via varIndexBase + 0)

    public uint VarIndexBase { get; init; } // uint32
}

// Formats 4/5: PaintLinearGradient / PaintVarLinearGradient
internal sealed class PaintLinearGradient : Paint
{
    public required ColorLine ColorLine { get; init; }

    public short X0 { get; init; } // FWORD

    public short Y0 { get; init; } // FWORD

    public short X1 { get; init; } // FWORD

    public short Y1 { get; init; } // FWORD

    public short X2 { get; init; } // FWORD (rotation point)

    public short Y2 { get; init; } // FWORD
}

internal sealed class PaintVarLinearGradient : Paint
{
    public required VarColorLine ColorLine { get; init; }

    public short X0 { get; init; } // FWORD (var +0)

    public short Y0 { get; init; } // FWORD (var +1)

    public short X1 { get; init; } // FWORD (var +2)

    public short Y1 { get; init; } // FWORD (var +3)

    public short X2 { get; init; } // FWORD (var +4)

    public short Y2 { get; init; } // FWORD (var +5)

    public uint VarIndexBase { get; init; } // uint32
}

// Formats 6/7: PaintRadialGradient / PaintVarRadialGradient
internal sealed class PaintRadialGradient : Paint
{
    public required ColorLine ColorLine { get; init; }

    public short X0 { get; init; } // FWORD

    public short Y0 { get; init; } // FWORD

    public ushort Radius0 { get; init; } // UFWORD

    public short X1 { get; init; } // FWORD

    public short Y1 { get; init; } // FWORD

    public ushort Radius1 { get; init; } // UFWORD
}

internal sealed class PaintVarRadialGradient : Paint
{
    public required VarColorLine ColorLine { get; init; }

    public short X0 { get; init; } // FWORD (var +0)

    public short Y0 { get; init; } // FWORD (var +1)

    public ushort Radius0 { get; init; } // UFWORD (var +2)

    public short X1 { get; init; } // FWORD (var +3)

    public short Y1 { get; init; } // FWORD (var +4)

    public ushort Radius1 { get; init; } // UFWORD (var +5)

    public uint VarIndexBase { get; init; } // uint32
}

// Formats 8/9: PaintSweepGradient / PaintVarSweepGradient
internal sealed class PaintSweepGradient : Paint
{
    public required ColorLine ColorLine { get; init; }

    public short CenterX { get; init; } // FWORD

    public short CenterY { get; init; } // FWORD

    public float StartAngle { get; init; } // F2DOT14 (bias per spec)

    public float EndAngle { get; init; } // F2DOT14 (bias per spec)
}

internal sealed class PaintVarSweepGradient : Paint
{
    public required VarColorLine ColorLine { get; init; }

    public short CenterX { get; init; } // FWORD (var +0)

    public short CenterY { get; init; } // FWORD (var +1)

    public float StartAngle { get; init; } // F2DOT14 (var +2)

    public float EndAngle { get; init; } // F2DOT14 (var +3)

    public uint VarIndexBase { get; init; } // uint32
}

// Format 10: PaintGlyph
internal sealed class PaintGlyph : Paint
{
    public required Paint Child { get; init; } // paintOffset -> child

    public ushort GlyphId { get; init; } // uint16
}

// Format 11: PaintColrGlyph
internal sealed class PaintColrGlyph : Paint
{
    public ushort GlyphId { get; init; } // uint16 (into BaseGlyphList)
}

// Formats 12/13: PaintTransform / PaintVarTransform
internal sealed class PaintTransform : Paint
{
    public required Paint Child { get; init; }

    public Affine2x3 Transform { get; init; }
}

internal sealed class PaintVarTransform : Paint
{
    public required Paint Child { get; init; }

    public VarAffine2x3 Transform { get; init; }
}

// Formats 14/15: PaintTranslate / PaintVarTranslate
internal sealed class PaintTranslate : Paint
{
    public required Paint Child { get; init; }

    public short Dx { get; init; } // FWORD

    public short Dy { get; init; } // FWORD
}

internal sealed class PaintVarTranslate : Paint
{
    public required Paint Child { get; init; }

    public short Dx { get; init; } // FWORD (var +0)

    public short Dy { get; init; } // FWORD (var +1)

    public uint VarIndexBase { get; init; } // uint32
}

// Formats 16/17: PaintScaleAroundCenter / PaintVarScaleAroundCenter
// Formats 18/19: PaintScale / PaintVarScale
// Formats 20/21: PaintScaleUniformAroundCenter / PaintVarScaleUniformAroundCenter
// Formats 22/23: PaintScaleUniform / PaintVarScaleUniform
internal sealed class PaintScale : Paint
{
    public required Paint Child { get; init; }

    public float ScaleX { get; init; } // F2DOT14

    public float ScaleY { get; init; } // F2DOT14

    public short CenterX { get; init; } // FWORD (0 if not a "around center" format)

    public short CenterY { get; init; } // FWORD (0 if not a "around center" format)

    public bool AroundCenter { get; init; } // indicates which scale format family

    public bool Uniform { get; init; } // indicates uniform vs anisotropic
}

internal sealed class PaintVarScale : Paint
{
    public required Paint Child { get; init; }

    public float ScaleX { get; init; } // (var +0)

    public float ScaleY { get; init; } // (var +1)

    public short CenterX { get; init; } // (var +2 if around center)

    public short CenterY { get; init; } // (var +3 if around center)

    public bool AroundCenter { get; init; }

    public bool Uniform { get; init; }

    public uint VarIndexBase { get; init; }
}

// Formats 24/25/26/27: Rotate variants
internal sealed class PaintRotate : Paint
{
    public required Paint Child { get; init; }

    public float Angle { get; init; } // F2DOT14

    public short CenterX { get; init; } // FWORD (0 if not "around center")

    public short CenterY { get; init; } // FWORD

    public bool AroundCenter { get; init; }
}

internal sealed class PaintVarRotate : Paint
{
    public required Paint Child { get; init; }

    public float Angle { get; init; } // (var +0)

    public short CenterX { get; init; } // (var +1 if around center)

    public short CenterY { get; init; } // (var +2 if around center)

    public bool AroundCenter { get; init; }

    public uint VarIndexBase { get; init; }
}

// Formats 28/29/30/31: Skew variants
internal sealed class PaintSkew : Paint
{
    public required Paint Child { get; init; }

    public float XSkew { get; init; } // F2DOT14

    public float YSkew { get; init; } // F2DOT14

    public short CenterX { get; init; } // FWORD (0 if not "around center")

    public short CenterY { get; init; } // FWORD

    public bool AroundCenter { get; init; }
}

internal sealed class PaintVarSkew : Paint
{
    public required Paint Child { get; init; }

    public float XSkew { get; init; } // (var +0)

    public float YSkew { get; init; } // (var +1)

    public short CenterX { get; init; } // (var +2 if around center)

    public short CenterY { get; init; } // (var +3 if around center)

    public bool AroundCenter { get; init; }

    public uint VarIndexBase { get; init; }
}

internal sealed class PaintComposite : Paint
{
    public ColrCompositeMode CompositeMode { get; init; } // uint16

    public required Paint Source { get; init; } // paintOffset

    public required Paint Backdrop { get; init; } // paintOffset
}
