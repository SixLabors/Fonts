// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// An <see cref="IGlyphRenderer"/> sink that captures a charstring evaluation into a
/// <see cref="CffOutline"/>. Instances are pooled and their buffers retained, so building
/// an outline allocates only the exact arrays the cached outline keeps.
/// </summary>
internal sealed class CffOutlineBuilder : IGlyphRenderer
{
    private static readonly ObjectPool<CffOutlineBuilder> Pool = new(new PooledObjectPolicy());

    private readonly List<CffOutlineVerb> verbs = [];
    private readonly List<Vector2> points = [];
    private readonly List<ushort> contourEnds = [];
    private Vector2 last;

    private CffOutlineBuilder()
    {
    }

    /// <summary>
    /// Gets the number of points captured so far. The engine tags hint mask regions with
    /// this count, so the fitter knows which stems were live for each run of points.
    /// </summary>
    public int PointCount => this.points.Count;

    /// <summary>
    /// Rents a cleared builder from the shared pool.
    /// </summary>
    /// <returns>The builder.</returns>
    public static CffOutlineBuilder Rent()
    {
        CffOutlineBuilder builder = Pool.Get();
        builder.verbs.Clear();
        builder.points.Clear();
        builder.contourEnds.Clear();
        builder.last = Vector2.Zero;
        return builder;
    }

    /// <summary>
    /// Returns this builder and its retained buffers to the shared pool.
    /// </summary>
    public void Release() => Pool.Return(this);

    /// <summary>
    /// Materializes the captured commands into an outline with exactly sized arrays.
    /// </summary>
    /// <param name="verticalStems">The declared vertical stem zones as X edge pairs in pixel space.</param>
    /// <param name="horizontalStems">The declared horizontal stem zones as Y edge pairs in pixel space.</param>
    /// <param name="initialStemCount">The number of stems active at the first movement operator.</param>
    /// <param name="lockFixMapOk">Whether GDI permits its post-lock overlap fixup for this charstring.</param>
    /// <param name="hintRegions">The hint mask regions, each naming the stems live for the run of points that starts at its index.</param>
    /// <param name="counterMasks">The cntrmask events in declaration order, empty when the glyph declares none.</param>
    /// <returns>The buffered outline.</returns>
    public CffOutline ToOutline(float[] verticalStems, float[] horizontalStems, int initialStemCount, bool lockFixMapOk, CffHintRegion[] hintRegions, CffCounterMask[] counterMasks)
    {
        if (this.points.Count > 0)
        {
            this.contourEnds.Add((ushort)(this.points.Count - 1));
        }

        return new CffOutline([.. this.verbs], [.. this.points], [.. this.contourEnds], verticalStems, horizontalStems, initialStemCount, lockFixMapOk, hintRegions, counterMasks);
    }

    /// <inheritdoc/>
    public void BeginText(in FontRectangle bounds)
    {
    }

    /// <inheritdoc/>
    public void EndText()
    {
    }

    /// <inheritdoc/>
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters) => true;

    /// <inheritdoc/>
    public void EndGlyph()
    {
    }

    /// <inheritdoc/>
    public void BeginLayer(Paint? paint, FillRule fillRule)
    {
    }

    /// <inheritdoc/>
    public void EndLayer()
    {
    }

    /// <inheritdoc/>
    public void BeginGroup(CompositeMode mode)
    {
    }

    /// <inheritdoc/>
    public void EndGroup()
    {
    }

    /// <inheritdoc/>
    public void BeginFigure()
    {
    }

    /// <inheritdoc/>
    public void MoveTo(Vector2 point)
    {
        if (this.points.Count > 0)
        {
            this.contourEnds.Add((ushort)(this.points.Count - 1));
        }

        this.verbs.Add(CffOutlineVerb.Move);
        this.points.Add(point);
        this.last = point;
    }

    /// <inheritdoc/>
    public void LineTo(Vector2 point)
    {
        this.verbs.Add(CffOutlineVerb.Line);
        this.points.Add(point);
        this.last = point;
    }

    /// <inheritdoc/>
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        // Charstrings emit cubics only, but a quadratic reaching the sink is captured
        // exactly through degree elevation rather than being distorted or dropped.
        Vector2 controlOne = this.last + ((secondControlPoint - this.last) * (2F / 3F));
        Vector2 controlTwo = point + ((secondControlPoint - point) * (2F / 3F));
        this.CubicBezierTo(controlOne, controlTwo, point);
    }

    /// <inheritdoc/>
    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        this.verbs.Add(CffOutlineVerb.Cubic);
        this.points.Add(secondControlPoint);
        this.points.Add(thirdControlPoint);
        this.points.Add(point);
        this.last = point;
    }

    /// <inheritdoc/>
    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
    {
        // Charstrings never emit arcs; a stray arc degrades to its chord, matching the
        // treatment in the emboldening renderer.
        this.LineTo(point);
    }

    /// <inheritdoc/>
    public void EndFigure()
    {
    }

    /// <inheritdoc/>
    public TextDecorations EnabledDecorations() => TextDecorations.None;

    /// <inheritdoc/>
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness, ReadOnlyMemory<float> intersections)
    {
    }

    private sealed class PooledObjectPolicy : IPooledObjectPolicy<CffOutlineBuilder>
    {
        public CffOutlineBuilder Create() => new();

        public bool Return(CffOutlineBuilder obj) => true;
    }
}
