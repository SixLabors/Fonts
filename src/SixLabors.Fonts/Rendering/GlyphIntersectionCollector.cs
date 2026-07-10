// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A glyph renderer that computes, per outline contour, the along-line interval spanned by the
/// contour's ink where it crosses a band, without rasterizing anything. This measures the ink that
/// a decoration line would cross so the line can be interrupted around it, as CSS
/// <c>text-decoration-skip-ink</c> requires. The band lies across the line axis: horizontal lines
/// band on y and collect x extents; vertical lines band on x and collect y extents. Each contour
/// contributes a single <c>[min, max]</c> interval bounding every place its outline reaches into
/// the band: the extent is grown by each crossing of the band's two edges and by every flattened
/// outline point that lies inside the band. A single bounding interval per contour tracks the ink
/// tightly and leaves no hole over it, so the decoration line can never be drawn across a stroke
/// that crosses it. The counter of a filled contour is bridged rather than threaded, keeping the
/// line from fragmenting: the spec leaves the exact gaps UA-defined and allows widening or dropping
/// ones that would be too small to be useful. Overlapping contour intervals are merged when the
/// span is built, and the consumer widens each by a clearance and re-merges, which also absorbs the
/// sub-pixel slack of the fixed curve flattening. Instances are reusable: <see cref="Reset"/> rearms
/// the collector for a new band without allocating, so a render loop can share one collector across
/// every glyph it decorates.
/// </summary>
internal sealed class GlyphIntersectionCollector : IGlyphRenderer
{
    /// <summary>
    /// The number of line segments a Bezier curve is subdivided into when locating its crossings
    /// of the band edges and its points inside the band. Decoration skipping tolerates sub-pixel
    /// error, which the consumer's clearance absorbs, so a fixed subdivision avoids the cost of
    /// adaptive flattening.
    /// </summary>
    private const int CurveSegments = 24;

    private float bandStart;
    private float bandEnd;
    private bool vertical;
    private readonly List<(float Start, float End)> intervals = [];

    /// <summary>
    /// Reusable buffer holding the merged interval pairs produced by
    /// <see cref="BuildIntersectionSpan"/>; grown on demand and kept across resets.
    /// </summary>
    private float[] merged = [];

    private Vector2 currentPoint;
    private Vector2 figureStart;

    /// <summary>
    /// Whether a figure has been started and not yet closed. Its bounding interval is finalized
    /// when the figure closes, so an unterminated figure is closed implicitly.
    /// </summary>
    private bool figureOpen;

    /// <summary>
    /// The along-line extent of the current figure's ink inside the band, grown as the outline is
    /// decoded. Empty (<see cref="figureLeft"/> greater than <see cref="figureRight"/>) until the
    /// figure first reaches the band.
    /// </summary>
    private float figureLeft;
    private float figureRight;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphIntersectionCollector"/> class with
    /// an empty band. <see cref="Reset"/> must be called before collecting.
    /// </summary>
    public GlyphIntersectionCollector()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphIntersectionCollector"/> class.
    /// </summary>
    /// <param name="lowerLimit">One edge of the band on the cross axis.</param>
    /// <param name="upperLimit">The other edge of the band on the cross axis.</param>
    /// <param name="vertical">
    /// <see langword="true"/> to band on x and collect y extents for vertical lines;
    /// <see langword="false"/> to band on y and collect x extents for horizontal lines.
    /// </param>
    public GlyphIntersectionCollector(float lowerLimit, float upperLimit, bool vertical = false)
        => this.Reset(lowerLimit, upperLimit, vertical);

    /// <summary>
    /// Rearms the collector for a new band, discarding any previously collected state while
    /// retaining the internal buffers so reuse does not allocate.
    /// </summary>
    /// <param name="lowerLimit">One edge of the band on the cross axis.</param>
    /// <param name="upperLimit">The other edge of the band on the cross axis.</param>
    /// <param name="vertical">
    /// <see langword="true"/> to band on x and collect y extents for vertical lines;
    /// <see langword="false"/> to band on y and collect x extents for horizontal lines.
    /// </param>
    public void Reset(float lowerLimit, float upperLimit, bool vertical)
    {
        this.bandStart = Math.Min(lowerLimit, upperLimit);
        this.bandEnd = Math.Max(lowerLimit, upperLimit);
        this.vertical = vertical;
        this.intervals.Clear();
        this.figureOpen = false;
        this.figureLeft = float.MaxValue;
        this.figureRight = float.MinValue;
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
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)

        // Outlines are only worth decoding when the glyph's box can reach the band.
        => this.vertical
            ? bounds.Right >= this.bandStart && bounds.Left <= this.bandEnd
            : bounds.Bottom >= this.bandStart && bounds.Top <= this.bandEnd;

    /// <inheritdoc/>
    public void EndGlyph() => this.CloseOpenFigure();

    /// <inheritdoc/>
    public void BeginLayer(Paint? paint, FillRule fillRule, ClipQuad? clipBounds)
    {
    }

    /// <inheritdoc/>
    public void EndLayer()
    {
    }

    /// <inheritdoc/>
    public void BeginFigure()
    {
    }

    /// <inheritdoc/>
    public void MoveTo(Vector2 point)
    {
        this.CloseOpenFigure();
        this.currentPoint = point;
        this.figureStart = point;
        this.figureOpen = true;
        this.figureLeft = float.MaxValue;
        this.figureRight = float.MinValue;
    }

    /// <inheritdoc/>
    public void LineTo(Vector2 point)
    {
        this.AddSegment(this.currentPoint, point);
        this.currentPoint = point;
    }

    /// <inheritdoc/>
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
    {
        Vector2 previous = this.currentPoint;
        Vector2 last = previous;
        for (int i = 1; i <= CurveSegments; i++)
        {
            float t = i / (float)CurveSegments;
            float u = 1F - t;
            Vector2 sample = (u * u * previous) + (2F * u * t * secondControlPoint) + (t * t * point);
            this.AddSegment(last, sample);
            last = sample;
        }

        this.currentPoint = point;
    }

    /// <inheritdoc/>
    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
    {
        Vector2 previous = this.currentPoint;
        Vector2 last = previous;
        for (int i = 1; i <= CurveSegments; i++)
        {
            float t = i / (float)CurveSegments;
            float u = 1F - t;
            Vector2 sample = (u * u * u * previous)
                + (3F * u * u * t * secondControlPoint)
                + (3F * u * t * t * thirdControlPoint)
                + (t * t * t * point);
            this.AddSegment(last, sample);
            last = sample;
        }

        this.currentPoint = point;
    }

    /// <inheritdoc/>
    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
    {
        // Font outlines are emitted as lines and Bezier curves; arcs do not occur in practice.
        // Fall back to the chord so an unexpected arc still contributes conservative coverage.
        this.AddSegment(this.currentPoint, point);
        this.currentPoint = point;
    }

    /// <inheritdoc/>
    public void EndFigure() => this.CloseOpenFigure();

    /// <inheritdoc/>
    public TextDecorations EnabledDecorations() => TextDecorations.None;

    /// <inheritdoc/>
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness, ReadOnlyMemory<float> intersections)
    {
    }

    /// <summary>
    /// Builds the merged, sorted flattened interval pairs collected so far.
    /// </summary>
    /// <returns>The flattened interval pairs, or an empty array when nothing crossed the band.</returns>
    public float[] BuildIntersections()
        => this.BuildIntersectionSpan().ToArray();

    /// <summary>
    /// Builds the merged, sorted flattened interval pairs collected so far into a reusable
    /// internal buffer. The span is valid until the next build or <see cref="Reset"/>; callers
    /// that need the values to escape must copy them.
    /// </summary>
    /// <returns>The flattened interval pairs, or an empty span when nothing crossed the band.</returns>
    public ReadOnlyMemory<float> BuildIntersectionSpan()
    {
        this.CloseOpenFigure();

        if (this.intervals.Count == 0)
        {
            return ReadOnlyMemory<float>.Empty;
        }

        this.intervals.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        int required = this.intervals.Count * 2;
        if (this.merged.Length < required)
        {
            this.merged = new float[Math.Max(required, this.merged.Length * 2)];
        }

        int count = 0;
        (float start, float end) = this.intervals[0];
        for (int i = 1; i < this.intervals.Count; i++)
        {
            (float Start, float End) interval = this.intervals[i];
            if (interval.Start <= end)
            {
                end = Math.Max(end, interval.End);
                continue;
            }

            this.merged[count++] = start;
            this.merged[count++] = end;
            (start, end) = interval;
        }

        this.merged[count++] = start;
        this.merged[count++] = end;
        return new ReadOnlyMemory<float>(this.merged, 0, count);
    }

    /// <summary>
    /// Grows the current figure's along-line extent by one outline segment: by each point at which
    /// the segment crosses either band edge, and by either endpoint that lies inside the band.
    /// </summary>
    /// <param name="from">The segment start point.</param>
    /// <param name="to">The segment end point.</param>
    private void AddSegment(Vector2 from, Vector2 to)
    {
        float fromBand = this.vertical ? from.X : from.Y;
        float toBand = this.vertical ? to.X : to.Y;
        float fromLine = this.vertical ? from.Y : from.X;
        float toLine = this.vertical ? to.Y : to.X;

        this.ExpandCrossing(this.bandStart, fromBand, toBand, fromLine, toLine);
        this.ExpandCrossing(this.bandEnd, fromBand, toBand, fromLine, toLine);

        if (fromBand > this.bandStart && fromBand < this.bandEnd)
        {
            this.Expand(fromLine);
        }

        if (toBand > this.bandStart && toBand < this.bandEnd)
        {
            this.Expand(toLine);
        }
    }

    /// <summary>
    /// Grows the current figure's along-line extent by the point at which a segment crosses one
    /// band edge, if it crosses it at all.
    /// </summary>
    /// <param name="edge">The edge's cross-axis coordinate.</param>
    /// <param name="fromBand">The segment start on the cross axis.</param>
    /// <param name="toBand">The segment end on the cross axis.</param>
    /// <param name="fromLine">The segment start on the line axis.</param>
    /// <param name="toLine">The segment end on the line axis.</param>
    private void ExpandCrossing(float edge, float fromBand, float toBand, float fromLine, float toLine)
    {
        bool fromBelow = fromBand <= edge;
        bool toBelow = toBand <= edge;
        if (fromBelow == toBelow)
        {
            return;
        }

        float t = (edge - fromBand) / (toBand - fromBand);
        this.Expand(fromLine + ((toLine - fromLine) * t));
    }

    /// <summary>
    /// Grows the current figure's along-line extent to include one coordinate.
    /// </summary>
    /// <param name="line">The along-line coordinate to include.</param>
    private void Expand(float line)
    {
        if (line < this.figureLeft)
        {
            this.figureLeft = line;
        }

        if (line > this.figureRight)
        {
            this.figureRight = line;
        }
    }

    /// <summary>
    /// Closes the open figure, if any, by intersecting its implicit closing edge with the band and
    /// then committing its bounding extent as one interval when the figure reached the band.
    /// </summary>
    private void CloseOpenFigure()
    {
        if (this.figureOpen)
        {
            this.figureOpen = false;
            if (this.currentPoint != this.figureStart)
            {
                this.AddSegment(this.currentPoint, this.figureStart);
            }

            this.currentPoint = this.figureStart;

            if (this.figureLeft < this.figureRight)
            {
                this.intervals.Add((this.figureLeft, this.figureRight));
            }

            this.figureLeft = float.MaxValue;
            this.figureRight = float.MinValue;
        }
    }
}
