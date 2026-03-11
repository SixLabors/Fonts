// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.General.Colr;

// Abstract ClipBox subtable (format-dispatched).
internal abstract class ClipBox
{
    public abstract Bounds GetBounds(ColrTable colr, GlyphVariationProcessor? processor);
}
