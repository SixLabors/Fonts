// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_334
    {
        [Fact]
        public void DoesNotSkewCompositeGlyph()
        {
            FontFamily family = new FontCollection().Add(TestFonts.SumanaRegularFile);
            family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

            Font font = family.CreateFont(metrics.UnitsPerEm);
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, "(", new TextOptions(font));

            Assert.Single(renderer.GlyphRects);

            FontRectangle expected = new(31, 190.5F, 264, 1047);
            Assert.Equal(expected, renderer.GlyphRects[0]);
        }
    }
}
