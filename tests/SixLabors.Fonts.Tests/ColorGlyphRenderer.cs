// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests;

// TODO: We don't actually need this type.We should refactor tests to remove it.
public class ColorGlyphRenderer : GlyphRenderer
{
    public List<GlyphColor> Colors { get; } = new List<GlyphColor>();

    public override void BeginLayer(Paint paint, FillRule fillRule)
    {
        if (paint is SolidPaint solidPaint)
        {
            this.Colors.Add(solidPaint.Color);
        }

        base.BeginLayer(paint, fillRule);
    }
}
