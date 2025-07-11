// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests;

public class ColorGlyphRenderer : GlyphRenderer, IColorGlyphRenderer
{
    public List<GlyphColor> Colors { get; } = new();

    public void SetColor(GlyphColor color) => this.Colors.Add(color);
}
