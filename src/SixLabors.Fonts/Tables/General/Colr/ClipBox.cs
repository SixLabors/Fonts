// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Colr;

// Abstract ClipBox subtable (format-dispatched).
internal abstract class ClipBox
{
    public abstract Bounds GetBounds(IVariationResolver? varResolver);
}
