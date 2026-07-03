// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// A glyph renderer that computes the along-line intervals where glyph ink crosses a band,
/// without rasterizing anything. The band lies across the line axis: horizontal lines band on
/// y and collect x extents; vertical lines band on x and collect y extents. Ink is measured as
/// filled coverage, not just boundary geometry: the extents of outline segments inside the band
/// are unioned with the nonzero-winding inside spans along each band edge, so a wide filled
/// shape whose boundary only crosses the band at its sides still covers the span between the
/// crossings, matching Skia's <c>GetIntercepts</c> semantics. Glyphs whose reported bounds do
/// not touch the band are skipped before their outlines are decoded, so runs pay outline cost
/// only for the glyphs that can contribute. Instances are reusable: <see cref="Reset"/>
/// rearms the collector for a new band without allocating, so a render loop can share one
/// collector across every glyph it decorates.
/// </summary>
internal sealed class GlyphIntersectionCollector : IGlyphRenderer
{
    /// <summary>
    /// The number of line segments a Bezier curve is subdivided into when intersecting the
    /// band. Decoration skipping tolerates sub-pixel error, so a fixed subdivision avoids the
    /// cost of adaptive flattening.
    /// </summary>
    private const int CurveSegments = 24;

    private float bandStart;
    private float bandEnd;
    private bool vertical;
    private readonly List<(float Start, float End)> intervals = [];

    /// <summary>
    /// Signed crossings of the outline with the scan line along the band's start edge, used to
    /// reconstruct the filled spans at that edge via the nonzero winding rule.
    /// </summary>
    private readonly List<(float Position, int Direction)> bandStartCrossings = [];

    /// <summary>
    /// Signed crossings of the outline with the scan line along the band's end edge, used to
    /// reconstruct the filled spans at that edge via the nonzero winding rule.
    /// </summary>
    private readonly List<(float Position, int Direction)> bandEndCrossings = [];

    /// <summary>
    /// Reusable buffer holding the merged interval pairs produced by
    /// <see cref="BuildIntersectionSpan"/>; grown on demand and kept across resets.
    /// </summary>
    private float[] merged = [];

    private Vector2 currentPoint;
    private Vector2 figureStart;

    /// <summary>
    /// Whether a figure has been started and not yet closed. Winding reconstruction needs
    /// every contour closed, so an unterminated figure is closed implicitly.
    /// </summary>
    private bool figureOpen;

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
        this.bandStartCrossings.Clear();
        this.bandEndCrossings.Clear();
        this.figureOpen = false;
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
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
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
    public ReadOnlySpan<float> BuildIntersectionSpan()
    {
        this.CloseOpenFigure();

        // Convert the edge crossings into filled spans and union them with the segment
        // extents so interiors that dodge the band (boundary crossing only at the sides)
        // still count as ink.
        this.AppendWindingSpans(this.bandStartCrossings);
        this.AppendWindingSpans(this.bandEndCrossings);

        if (this.intervals.Count == 0)
        {
            return [];
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
        return new ReadOnlySpan<float>(this.merged, 0, count);
    }

    /// <summary>
    /// Intersects one outline segment with the band. Records the along-line extent of the
    /// portion inside the band, and any signed crossings of the band's edge scan lines for
    /// later winding reconstruction of the filled spans.
    /// </summary>
    /// <param name="from">The segment start point.</param>
    /// <param name="to">The segment end point.</param>
    private void AddSegment(Vector2 from, Vector2 to)
    {
        float fromBand = this.vertical ? from.X : from.Y;
        float toBand = this.vertical ? to.X : to.Y;
        float fromLine = this.vertical ? from.Y : from.X;
        float toLine = this.vertical ? to.Y : to.X;

        // Edge crossings are recorded regardless of the extent rejection below: a segment
        // passing straight through the band still bounds the filled span at each edge.
        AddCrossing(this.bandStartCrossings, this.bandStart, fromBand, toBand, fromLine, toLine);
        AddCrossing(this.bandEndCrossings, this.bandEnd, fromBand, toBand, fromLine, toLine);

        float minBand = Math.Min(fromBand, toBand);
        float maxBand = Math.Max(fromBand, toBand);
        if (maxBand < this.bandStart || minBand > this.bandEnd)
        {
            return;
        }

        float start = fromLine;
        float end = toLine;

        // Clip the segment parametrically to the band; the line coordinate varies linearly
        // with t, so the clipped endpoints bound the extent of the portion inside the band.
        float deltaBand = toBand - fromBand;
        if (deltaBand != 0F)
        {
            float deltaLine = toLine - fromLine;
            float t0 = Math.Clamp((this.bandStart - fromBand) / deltaBand, 0F, 1F);
            float t1 = Math.Clamp((this.bandEnd - fromBand) / deltaBand, 0F, 1F);
            start = fromLine + (deltaLine * t0);
            end = fromLine + (deltaLine * t1);
        }

        this.intervals.Add((Math.Min(start, end), Math.Max(start, end)));
    }

    /// <summary>
    /// Records the signed crossing of one outline segment with an edge scan line, if any. The
    /// half-open rule (start side inclusive, end side exclusive) counts each crossing exactly
    /// once across the shared endpoints of consecutive segments.
    /// </summary>
    /// <param name="crossings">The crossing list for the edge.</param>
    /// <param name="edge">The edge's cross-axis coordinate.</param>
    /// <param name="fromBand">The segment start on the cross axis.</param>
    /// <param name="toBand">The segment end on the cross axis.</param>
    /// <param name="fromLine">The segment start on the line axis.</param>
    /// <param name="toLine">The segment end on the line axis.</param>
    private static void AddCrossing(
        List<(float Position, int Direction)> crossings,
        float edge,
        float fromBand,
        float toBand,
        float fromLine,
        float toLine)
    {
        bool fromBelow = fromBand <= edge;
        bool toBelow = toBand <= edge;
        if (fromBelow == toBelow)
        {
            return;
        }

        float t = (edge - fromBand) / (toBand - fromBand);
        float position = fromLine + ((toLine - fromLine) * t);
        crossings.Add((position, fromBelow ? 1 : -1));
    }

    /// <summary>
    /// Reconstructs the filled spans along one edge scan line from its signed crossings using
    /// the nonzero winding rule, and appends them to the interval list. The crossings are
    /// consumed so a repeated build does not double count them.
    /// </summary>
    /// <param name="crossings">The crossing list for the edge.</param>
    private void AppendWindingSpans(List<(float Position, int Direction)> crossings)
    {
        if (crossings.Count > 1)
        {
            crossings.Sort(static (a, b) => a.Position.CompareTo(b.Position));

            int winding = 0;
            float spanStart = 0F;
            foreach ((float position, int direction) in crossings)
            {
                int previous = winding;
                winding += direction;
                if (previous == 0 && winding != 0)
                {
                    spanStart = position;
                }
                else if (previous != 0 && winding == 0)
                {
                    this.intervals.Add((spanStart, position));
                }
            }
        }

        crossings.Clear();
    }

    /// <summary>
    /// Closes the open figure, if any, by intersecting its implicit closing edge with the
    /// band. Winding reconstruction requires every contour to be closed.
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
        }
    }
}
