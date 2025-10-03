// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tables.Cff;

internal class CffBoundsFinder : IGlyphRenderer
{
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;
    private Vector2 currentXY;
    private readonly int nsteps;
    private bool open;
    private bool firstEval;

    public CffBoundsFinder()
    {
        this.minX = float.MaxValue;
        this.maxX = float.MinValue;
        this.minY = float.MaxValue;
        this.maxY = float.MinValue;
        this.nsteps = 3;
        this.currentXY = Vector2.Zero;
        this.open = false;
        this.firstEval = true;
    }

    public void BeginFigure()
    {
        // Do nothing.
    }

    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        => true; // Do nothing.

    public void BeginText(in FontRectangle bounds)
    {
        // Do nothing.
    }

    public void EndFigure()
    {
        this.open = false;
        this.currentXY = Vector2.Zero;
    }

    public void EndGlyph()
    {
        if (this.open)
        {
            this.EndFigure();
        }
    }

    public void EndText()
    {
        if (this.open)
        {
            this.EndFigure();
        }
    }

    public void BeginLayer(Paint? paint, FillRule fillRule)
    {
        // Do nothing.
    }

    public void EndLayer()
    {
        // Do nothing.
    }

    public void LineTo(Vector2 point)
    {
        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
        this.open = true;
    }

    public void MoveTo(Vector2 point)
    {
        if (this.open)
        {
            this.EndFigure();
        }

        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
    }

    public void ArcTo(float radiusX, float radiusY, float xAxisRotation, bool largeArc, bool sweep, Vector2 point)
    {
        // TODO: check this. I feel like we should have to implement it.
        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
        this.open = true;
    }

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        float eachstep = 1F / this.nsteps;
        float t = eachstep; // Start

        for (int n = 1; n < this.nsteps; ++n)
        {
            float c = 1F - t;
            Vector2 xy = (this.currentXY * c * c * c) + (secondControlPoint * 3 * t * c * c) + (thirdControlPoint * 3 * t * t * c) + (point * t * t * t);
            this.UpdateMinMax(xy.X, xy.Y);

            t += eachstep;
        }

        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
        this.open = true;
    }

    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        float eachstep = 1F / this.nsteps;
        float t = eachstep; // Start

        for (int n = 1; n < this.nsteps; ++n)
        {
            float c = 1F - t;
            Vector2 xy = (this.currentXY * c * c) + (secondControlPoint * 2 * t * c) + (point * t * t);
            this.UpdateMinMax(xy.X, xy.Y);

            t += eachstep;
        }

        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
        this.open = true;
    }

    public TextDecorations EnabledDecorations()
        => TextDecorations.None;

    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
        // Do nothing.
    }

    private void UpdateMinMax(float x0, float y0)
    {
        if (this.firstEval)
        {
            // 4 times
            if (x0 < this.minX)
            {
                this.minX = x0;
            }

            if (x0 > this.maxX)
            {
                this.maxX = x0;
            }

            if (y0 < this.minY)
            {
                this.minY = y0;
            }

            if (y0 > this.maxY)
            {
                this.maxY = y0;
            }

            this.firstEval = false;
        }
        else
        {
            // 2 times
            if (x0 < this.minX)
            {
                this.minX = x0;
            }
            else if (x0 > this.maxX)
            {
                this.maxX = x0;
            }

            if (y0 < this.minY)
            {
                this.minY = y0;
            }
            else if (y0 > this.maxY)
            {
                this.maxY = y0;
            }
        }
    }

    public Bounds GetBounds()
        => new(
            (short)Math.Floor(this.minX),
            (short)Math.Floor(this.minY),
            (short)Math.Ceiling(this.maxX),
            (short)Math.Ceiling(this.maxY));
}
