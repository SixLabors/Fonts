// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests;

public class GlyphRenderer : IGlyphRenderer
{
    private int figuresCount;
    private GlyphRendererParameters parameters;

    public List<Vector2> ControlPoints { get; } = new();

    public List<Vector2> ControlPointsOnCurve { get; } = new();

    public List<FontRectangle> GlyphRects { get; } = new();

    public List<GlyphRendererParameters> GlyphKeys { get; } = new();

    public bool BeginGlyph(in FontRectangle rect, in GlyphRendererParameters parameters)
    {
        this.GlyphRects.Add(rect);
        this.GlyphKeys.Add(this.parameters = parameters);
        return true;
    }

    public void BeginFigure() => this.figuresCount++;

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        this.ControlPoints.Add(secondControlPoint);
        this.ControlPoints.Add(thirdControlPoint);
        this.ControlPoints.Add(point);
        this.ControlPointsOnCurve.Add(point);
    }

    public void EndGlyph()
    {
    }

    public void EndFigure()
    {
    }

    public void LineTo(Vector2 point)
    {
        this.ControlPoints.Add(point);
        this.ControlPointsOnCurve.Add(point);
    }

    public void MoveTo(Vector2 point)
    {
        this.ControlPoints.Add(point);
        this.ControlPointsOnCurve.Add(point);
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        this.ControlPoints.Add(secondControlPoint);
        this.ControlPoints.Add(point);
        this.ControlPointsOnCurve.Add(point);
    }

    public void EndText()
    {
    }

    public void BeginText(in FontRectangle rect)
    {
    }

    public TextDecorations EnabledDecorations()
        => this.parameters.TextRun.TextDecorations;

    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
    }
}
