// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if OS_WINDOWS
using System.Numerics;

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
        TextRenderer.RenderTextTo(new DummyGlyphRenderer(), "i", textOption);

        // OK
        TextRenderer.RenderTextTo(new DummyGlyphRenderer(), "v", textOption);

        // raise ArgumentOutOfRangeException
        TextRenderer.RenderTextTo(new DummyGlyphRenderer(), "a", textOption);

        textOption.WrappingLength = 9.0F;

        // OK
        TextRenderer.RenderTextTo(new DummyGlyphRenderer(), "i", textOption);

        // raise ArgumentOutOfRangeException
        TextRenderer.RenderTextTo(new DummyGlyphRenderer(), "v", textOption);

        // OK
        TextRenderer.RenderTextTo(new DummyGlyphRenderer(), "i\r\nv", textOption);

        // raise ArgumentOutOfRangeException
        TextRenderer.RenderTextTo(new DummyGlyphRenderer(), "v\r\ni", textOption);
    }
}

internal class DummyGlyphRenderer : IGlyphRenderer
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

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
    }

    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
    }
}
#endif
