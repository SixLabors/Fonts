// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if OS_WINDOWS
using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_383
{
    [Fact]
    public void CanBreakLinesWithShortWrappingLength()
    {
        FontFamily fontFamily = SystemFonts.Get("Yu Gothic");
        Font font = fontFamily.CreateFont(20.0F);

        TextOptions textOption = new(font)
        {
            WrappingLength = 10.0F,
            WordBreaking = WordBreaking.BreakAll
        };

        // OK
        TextRenderer.RenderTextTo(new NoOpGlyphRenderer(), "i", textOption);

        // OK
        TextRenderer.RenderTextTo(new NoOpGlyphRenderer(), "v", textOption);

        // raise ArgumentOutOfRangeException
        TextRenderer.RenderTextTo(new NoOpGlyphRenderer(), "a", textOption);

        textOption.WrappingLength = 9.0F;

        // OK
        TextRenderer.RenderTextTo(new NoOpGlyphRenderer(), "i", textOption);

        // raise ArgumentOutOfRangeException
        TextRenderer.RenderTextTo(new NoOpGlyphRenderer(), "v", textOption);

        // OK
        TextRenderer.RenderTextTo(new NoOpGlyphRenderer(), "i\r\nv", textOption);

        // raise ArgumentOutOfRangeException
        TextRenderer.RenderTextTo(new NoOpGlyphRenderer(), "v\r\ni", textOption);
    }
}

internal class NoOpGlyphRenderer : IGlyphRenderer
{
    public void BeginFigure()
    {
    }

    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters) => true;

    public void BeginText(in FontRectangle bounds)
    {
    }

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
    }

    public TextDecorations EnabledDecorations() => TextDecorations.None;

    public void EndFigure()
    {
    }

    public void EndGlyph()
    {
    }

    public void EndText()
    {
    }

    public void LineTo(Vector2 point)
    {
    }

    public void MoveTo(Vector2 point)
    {
    }

    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
    {
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
    }

    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
    }

    public void BeginLayer(Paint paint, FillRule fillRule)
    {
    }

    public void EndLayer()
    {
    }
}
#endif
