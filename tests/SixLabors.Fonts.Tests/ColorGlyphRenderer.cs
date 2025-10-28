// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests;

// TODO: We massively overuse this type.
// We should refactor tests to remove it where possible.
public class ColorGlyphRenderer : GlyphRenderer
{
    public List<GlyphColor> Colors { get; } = new List<GlyphColor>();

    public override void BeginLayer(Paint paint, FillRule fillRule, in ClipQuad? clipBounds)
    {
        if (paint is SolidPaint solidPaint)
        {
            this.Colors.Add(solidPaint.Color);
        }

        base.BeginLayer(paint, fillRule, clipBounds);
    }
}
