// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Defines the composite (blending) modes used by COLR v1 PaintComposite (format 32) and PaintVarComposite (format 33).
/// Includes Porter-Duff compositing operators and separable color blend modes.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#compositemode"/>
/// </summary>
internal enum ColrCompositeMode : byte
{
    /// <summary>
    /// Porter-Duff Clear: no output.
    /// </summary>
    Clear = 0,

    /// <summary>
    /// Porter-Duff Src: source only.
    /// </summary>
    Src = 1,

    /// <summary>
    /// Porter-Duff Dest: destination only.
    /// </summary>
    Dst = 2,

    /// <summary>
    /// Porter-Duff Src Over: source over destination.
    /// </summary>
    SrcOver = 3,

    /// <summary>
    /// Porter-Duff Dest Over: destination over source.
    /// </summary>
    DstOver = 4,

    /// <summary>
    /// Porter-Duff Src In: source where destination exists.
    /// </summary>
    SrcIn = 5,

    /// <summary>
    /// Porter-Duff Dest In: destination where source exists.
    /// </summary>
    DstIn = 6,

    /// <summary>
    /// Porter-Duff Src Out: source where destination does not exist.
    /// </summary>
    SrcOut = 7,

    /// <summary>
    /// Porter-Duff Dest Out: destination where source does not exist.
    /// </summary>
    DstOut = 8,

    /// <summary>
    /// Porter-Duff Src Atop: source atop destination.
    /// </summary>
    SrcAtop = 9,

    /// <summary>
    /// Porter-Duff Dest Atop: destination atop source.
    /// </summary>
    DstAtop = 10,

    /// <summary>
    /// Porter-Duff Xor: exclusive OR of source and destination.
    /// </summary>
    Xor = 11,

    /// <summary>
    /// Porter-Duff Plus: additive blending.
    /// </summary>
    Plus = 12,

    /// <summary>
    /// Screen blend mode.
    /// </summary>
    Screen = 13,

    /// <summary>
    /// Overlay blend mode.
    /// </summary>
    Overlay = 14,

    /// <summary>
    /// Darken blend mode: selects the darker of source and destination.
    /// </summary>
    Darken = 15,

    /// <summary>
    /// Lighten blend mode: selects the lighter of source and destination.
    /// </summary>
    Lighten = 16,

    /// <summary>
    /// Color dodge blend mode.
    /// </summary>
    ColorDodge = 17,

    /// <summary>
    /// Color burn blend mode.
    /// </summary>
    ColorBurn = 18,

    /// <summary>
    /// Hard light blend mode.
    /// </summary>
    HardLight = 19,

    /// <summary>
    /// Soft light blend mode.
    /// </summary>
    SoftLight = 20,

    /// <summary>
    /// Difference blend mode.
    /// </summary>
    Difference = 21,

    /// <summary>
    /// Exclusion blend mode.
    /// </summary>
    Exclusion = 22,

    /// <summary>
    /// Multiply blend mode.
    /// </summary>
    Multiply = 23,

    /// <summary>
    /// Hue blend mode: applies the hue of the source to the destination.
    /// </summary>
    Hue = 24,

    /// <summary>
    /// Saturation blend mode: applies the saturation of the source to the destination.
    /// </summary>
    Saturation = 25,

    /// <summary>
    /// Color blend mode: applies the hue and saturation of the source to the destination.
    /// </summary>
    Color = 26,

    /// <summary>
    /// Luminosity blend mode: applies the luminosity of the source to the destination.
    /// </summary>
    Luminosity = 27
}
