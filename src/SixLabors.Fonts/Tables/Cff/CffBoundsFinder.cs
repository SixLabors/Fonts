// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Calculates the bounding box of a CFF glyph by implementing <see cref="IGlyphRenderer"/>
/// and tracking the minimum and maximum coordinates of all path operations.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CffBoundsFinder"/> class.
    /// </summary>
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

    /// <inheritdoc/>
    public void BeginFigure()
    {
        // Do nothing.
    }

    /// <inheritdoc/>
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        => true; // Do nothing.

    /// <inheritdoc/>
    public void BeginText(in FontRectangle bounds)
    {
        // Do nothing.
    }

    /// <inheritdoc/>
    public void EndFigure()
    {
        this.open = false;
        this.currentXY = Vector2.Zero;
    }

    /// <inheritdoc/>
    public void EndGlyph()
    {
        if (this.open)
        {
            this.EndFigure();
        }
    }

    /// <inheritdoc/>
    public void EndText()
    {
        if (this.open)
        {
            this.EndFigure();
        }
    }

    /// <inheritdoc/>
    public void BeginLayer(Paint? paint, FillRule fillRule, ClipQuad? clipBounds)
    {
        // Do nothing.
    }

    /// <inheritdoc/>
    public void EndLayer()
    {
        // Do nothing.
    }

    /// <inheritdoc/>
    public void LineTo(Vector2 point)
    {
        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
        this.open = true;
    }

    /// <inheritdoc/>
    public void MoveTo(Vector2 point)
    {
        if (this.open)
        {
            this.EndFigure();
        }

        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
    }

    /// <inheritdoc/>
    public void ArcTo(float radiusX, float radiusY, float xAxisRotation, bool largeArc, bool sweep, Vector2 point)
    {
        // TODO: check this. I feel like we should have to implement it.
        this.currentXY = point;
        this.UpdateMinMax(point.X, point.Y);
        this.open = true;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public TextDecorations EnabledDecorations()
        => TextDecorations.None;

    /// <inheritdoc/>
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
    {
        // Do nothing.
    }

    /// <summary>
    /// Updates the tracked minimum and maximum coordinates with the given point.
    /// </summary>
    /// <param name="x0">The x-coordinate to evaluate.</param>
    /// <param name="y0">The y-coordinate to evaluate.</param>
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

    /// <summary>
    /// Gets the computed bounding box from all tracked path coordinates.
    /// </summary>
    /// <returns>The <see cref="Bounds"/> representing the glyph bounding box.</returns>
    public Bounds GetBounds()
        => new(
            (short)Math.Floor(this.minX),
            (short)Math.Floor(this.minY),
            (short)Math.Ceiling(this.maxX),
            (short)Math.Ceiling(this.maxY));
}
