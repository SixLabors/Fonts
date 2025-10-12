// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#pragma warning disable SA1201 // Elements should appear in the correct order
namespace SixLabors.Fonts.Tables.General.Colr;

// Abstract ClipBox subtable (format-dispatched).
internal abstract class ClipBox
{
    public abstract Bounds GetBounds(IVariationResolver? varResolver);
}
