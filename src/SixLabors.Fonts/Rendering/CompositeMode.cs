// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Defines compositing and blending operations used when combining source and destination colors.
/// <para>
/// Values 0–12 correspond to standard Porter–Duff compositing modes. These determine how source and
/// destination alpha interact to produce transparency. The remaining values (13–27) correspond to
/// separable and non-separable blend modes used in modern graphics systems such as ImageSharp.
/// </para>
/// </summary>
public enum CompositeMode
{
    // --- Porter–Duff compositing modes ---

    /// <summary>
    /// Clears both the source and destination.
    /// The output is fully transparent regardless of the input colors.
    /// </summary>
    Clear = 0,

    /// <summary>
    /// Replaces the destination entirely with the source.
    /// The destination pixels are ignored.
    /// </summary>
    Src = 1,

    /// <summary>
    /// Keeps the destination as-is and ignores the source.
    /// Equivalent to no drawing operation.
    /// </summary>
    Dest = 2,

    /// <summary>
    /// Draws the source over the destination using standard alpha compositing.
    /// The source appears on top and the destination shows through transparent areas.
    /// </summary>
    SrcOver = 3,

    /// <summary>
    /// Draws the destination over the source.
    /// The destination appears on top and the source shows through transparent areas.
    /// </summary>
    DestOver = 4,

    /// <summary>
    /// Shows the source only where it overlaps the destination.
    /// The destination’s alpha acts as a mask for the source.
    /// </summary>
    SrcIn = 5,

    /// <summary>
    /// Shows the destination only where it overlaps the source.
    /// The source’s alpha acts as a mask for the destination.
    /// </summary>
    DestIn = 6,

    /// <summary>
    /// Shows the source only where it does not overlap the destination.
    /// Produces the inverse of <see cref="SrcIn"/>.
    /// </summary>
    SrcOut = 7,

    /// <summary>
    /// Shows the destination only where it does not overlap the source.
    /// Produces the inverse of <see cref="DestIn"/>.
    /// </summary>
    DestOut = 8,

    /// <summary>
    /// Draws the source over the destination but only within the destination’s alpha region.
    /// Outside that region, the destination remains unchanged.
    /// </summary>
    SrcAtop = 9,

    /// <summary>
    /// Draws the destination over the source but only within the source’s alpha region.
    /// Outside that region, the source is visible.
    /// </summary>
    DestAtop = 10,

    /// <summary>
    /// Exclusive OR.
    /// Shows the source and destination only where they do not overlap.
    /// Overlapping regions become transparent.
    /// </summary>
    Xor = 11,

    /// <summary>
    /// Adds the source and destination color values.
    /// Alpha is also added, producing a brightening effect.
    /// </summary>
    Plus = 12,

    // --- Separable and non-separable blend modes ---

    /// <summary>
    /// Combines colors using an inverse multiply.
    /// Formula: <c>1 − (1 − S) × (1 − D)</c>.
    /// Produces a lighter result similar to photographic screen exposure.
    /// </summary>
    Screen = 13,

    /// <summary>
    /// Multiplies or screens colors depending on destination lightness.
    /// Preserves highlights and shadows while mixing source and destination tones.
    /// </summary>
    Overlay = 14,

    /// <summary>
    /// Chooses the darker of source and destination values per color channel.
    /// </summary>
    Darken = 15,

    /// <summary>
    /// Chooses the lighter of source and destination values per color channel.
    /// </summary>
    Lighten = 16,

    /// <summary>
    /// Brightens the destination to reflect the source.
    /// Formula: <c>D / (1 − S)</c>.
    /// </summary>
    ColorDodge = 17,

    /// <summary>
    /// Darkens the destination to reflect the source.
    /// Formula: <c>1 − (1 − D) / S</c>.
    /// </summary>
    ColorBurn = 18,

    /// <summary>
    /// Applies overlay logic using the source’s lightness.
    /// Used for strong highlight and shadow effects.
    /// </summary>
    HardLight = 19,

    /// <summary>
    /// Similar to <see cref="HardLight"/>, but with reduced contrast.
    /// Produces a softer transition between tones.
    /// </summary>
    SoftLight = 20,

    /// <summary>
    /// Subtracts darker colors from lighter ones to highlight differences.
    /// Often used for comparison or edge detection effects.
    /// </summary>
    Difference = 21,

    /// <summary>
    /// Similar to <see cref="Difference"/>, but with reduced contrast.
    /// Midtones are preserved, producing a lower-contrast difference.
    /// </summary>
    Exclusion = 22,

    /// <summary>
    /// Multiplies source and destination colors.
    /// Always results in a darker composite.
    /// </summary>
    Multiply = 23,

    /// <summary>
    /// Combines the hue of the source with the saturation and luminosity of the destination.
    /// </summary>
    Hue = 24,

    /// <summary>
    /// Combines the saturation of the source with the hue and luminosity of the destination.
    /// </summary>
    Saturation = 25,

    /// <summary>
    /// Combines the hue and saturation of the source with the luminosity of the destination.
    /// </summary>
    Color = 26,

    /// <summary>
    /// Combines the luminosity of the source with the hue and saturation of the destination.
    /// </summary>
    Luminosity = 27
}
