// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Abstract base class for COLR v1 ClipBox subtables, which define bounding boxes for clipping paint operations.
/// Format-dispatched into <see cref="ClipBoxFormat1"/> (static) and <see cref="ClipBoxFormat2"/> (variable).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr#clipbox"/>
/// </summary>
internal abstract class ClipBox
{
    /// <summary>
    /// Gets the bounds of the clip box, optionally applying variation deltas.
    /// </summary>
    /// <param name="colr">The COLR table used for resolving variation deltas.</param>
    /// <param name="processor">The glyph variation processor, or <see langword="null"/> for non-variable fonts.</param>
    /// <returns>The resolved bounding box.</returns>
    public abstract Bounds GetBounds(ColrTable colr, GlyphVariationProcessor? processor);
}
