// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// An <see cref="IGlyphRenderer"/> decorator that synthesizes a bold (faux bold) weight by
/// dilating each glyph outline outward. It is used when a bold style is requested for a font
/// family that provides no bold face, mirroring the CSS <c>font-synthesis: weight</c> behavior
/// used by web browsers.
/// </summary>
/// <remarks>
/// Outline contours are buffered per fill group so their overall winding can be determined.
/// The points are then moved using the same bounded lateral-bisector algorithm as FreeType's
/// <c>FT_Outline_EmboldenXY</c>, and replayed with their original segment types.
/// </remarks>
internal sealed class EmboldeningGlyphRenderer : IGlyphRenderer
{
    private static readonly ObjectPool<EmboldeningGlyphRenderer> Pool = new(new PooledObjectPolicy());

    private readonly List<Contour> group = [];
    private readonly List<Contour> availableContours = [];
    private IGlyphRenderer inner = null!;
    private float strength;
    private Contour? current;

    private EmboldeningGlyphRenderer()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmboldeningGlyphRenderer"/> class.
    /// </summary>
    /// <param name="inner">The renderer that receives the dilated outline.</param>
    /// <param name="strength">The total x/y outline strength, in pixels.</param>
    public EmboldeningGlyphRenderer(IGlyphRenderer inner, float strength)
    {
        this.inner = inner;
        this.strength = strength;
    }

    private enum SegmentType : byte
    {
        Move,
        Line,
        Quadratic,
        Cubic,
    }

    /// <summary>
    /// Rents a renderer configured to forward the emboldened outline to the given renderer.
    /// </summary>
    /// <param name="inner">The renderer that receives the dilated outline.</param>
    /// <param name="strength">The total x/y outline strength, in pixels.</param>
    /// <returns>The configured renderer.</returns>
    public static EmboldeningGlyphRenderer Rent(IGlyphRenderer inner, float strength)
    {
        EmboldeningGlyphRenderer renderer = Pool.Get();
        renderer.inner = inner;
        renderer.strength = strength;
        return renderer;
    }

    /// <summary>
    /// Returns this renderer and its retained contour buffers to the shared pool.
    /// </summary>
    public void Release() => Pool.Return(this);

    /// <inheritdoc/>
    public void BeginText(in FontRectangle bounds) => this.inner.BeginText(in bounds);

    /// <inheritdoc/>
    public void EndText() => this.inner.EndText();

    /// <inheritdoc/>
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        => this.inner.BeginGlyph(in bounds, in parameters);

    /// <inheritdoc/>
    public void EndGlyph()
    {
        this.Flush();
        this.inner.EndGlyph();
    }

    /// <inheritdoc/>
    public void BeginLayer(Paint? paint, FillRule fillRule, ClipQuad? clipBounds)
        => this.inner.BeginLayer(paint, fillRule, clipBounds);

    /// <inheritdoc/>
    public void EndLayer()
    {
        this.Flush();
        this.inner.EndLayer();
    }

    /// <inheritdoc/>
    public void BeginFigure() => this.current = this.GetContour();

    /// <inheritdoc/>
    public void MoveTo(Vector2 point) => this.Add(SegmentType.Move, point);

    /// <inheritdoc/>
    public void LineTo(Vector2 point) => this.Add(SegmentType.Line, point);

    /// <inheritdoc/>
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        => this.Add(SegmentType.Quadratic, secondControlPoint, point);

    /// <inheritdoc/>
    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        => this.Add(SegmentType.Cubic, secondControlPoint, thirdControlPoint, point);

    /// <inheritdoc/>
    public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
        => this.Add(SegmentType.Line, point);

    /// <inheritdoc/>
    public void EndFigure()
    {
        if (this.current is { Points.Count: > 0 })
        {
            this.group.Add(this.current);
        }

        this.current = null;
    }

    /// <inheritdoc/>
    public TextDecorations EnabledDecorations() => this.inner.EnabledDecorations();

    /// <inheritdoc/>
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness, ReadOnlyMemory<float> intersections)
        => this.inner.SetDecoration(textDecorations, start, end, thickness, intersections);

    /// <summary>
    /// Emits any buffered contours after the owning glyph outline has been fully decoded.
    /// </summary>
    public void CompleteOutline() => this.Flush();

    /// <summary>
    /// Moves one contour's points using FreeType's bounded lateral-bisector dilation.
    /// </summary>
    /// <param name="points">The contour points, including off-curve control points.</param>
    /// <param name="strength">The total x/y outline strength.</param>
    /// <param name="trueTypeOrientation">
    /// <see langword="true"/> when the complete outline uses FreeType's TrueType orientation;
    /// otherwise <see langword="false"/> for PostScript orientation.
    /// </param>
    private static void Embolden(List<Vector2> points, float strength, bool trueTypeOrientation)
    {
        // FreeType treats the requested value as the total increase and moves points using half
        // that amount in each axis. The remaining movement comes from the local edge bisector.
        strength *= .5F;

        int first = 0;
        int last = points.Count - 1;
        int i = last;
        int j = first;
        int anchorIndex = -1;
        Vector2 incoming = default;
        Vector2 anchor = default;
        float incomingLength = 0F;
        float anchorLength = 0F;

        // This is a direct float translation of FT_Outline_EmboldenXY. j examines successive
        // points, i advances only when points are moved, and anchorIndex closes the contour after
        // coincident points have been skipped.
        while (j != i && i != anchorIndex)
        {
            Vector2 outgoing;
            float outgoingLength;
            if (j != anchorIndex)
            {
                outgoing = points[j] - points[i];
                outgoingLength = outgoing.Length();
                if (outgoingLength == 0F)
                {
                    j = j < last ? j + 1 : first;
                    continue;
                }

                outgoing /= outgoingLength;
            }
            else
            {
                outgoing = anchor;
                outgoingLength = anchorLength;
            }

            if (incomingLength != 0F)
            {
                if (anchorIndex < 0)
                {
                    anchorIndex = i;
                    anchor = incoming;
                    anchorLength = incomingLength;
                }

                float dot = Vector2.Dot(incoming, outgoing);
                Vector2 shift = default;

                // FreeType leaves turns of roughly 160 degrees or more untouched. This avoids
                // unstable bisectors where two edges almost reverse direction.
                if (dot > -0.9375F)
                {
                    float denominator = dot + 1F;
                    shift = new Vector2(incoming.Y + outgoing.Y, incoming.X + outgoing.X);

                    if (trueTypeOrientation)
                    {
                        shift.X = -shift.X;
                    }
                    else
                    {
                        shift.Y = -shift.Y;
                    }

                    float cross = (outgoing.X * incoming.Y) - (outgoing.Y * incoming.X);
                    if (trueTypeOrientation)
                    {
                        cross = -cross;
                    }

                    // Limit each component by the shorter adjacent edge. FreeType uses this
                    // branch to stop collapsing segments from producing unbounded corner spikes.
                    float length = MathF.Min(incomingLength, outgoingLength);
                    shift.X = (strength * cross) <= (length * denominator)
                        ? shift.X * strength / denominator
                        : shift.X * length / cross;

                    shift.Y = (strength * cross) <= (length * denominator)
                        ? shift.Y * strength / denominator
                        : shift.Y * length / cross;
                }

                Vector2 delta = new(strength + shift.X, strength + shift.Y);
                while (i != j)
                {
                    points[i] += delta;
                    i = i < last ? i + 1 : first;
                }
            }
            else
            {
                i = j;
            }

            incoming = outgoing;
            incomingLength = outgoingLength;
            j = j < last ? j + 1 : first;
        }
    }

    private void Add(SegmentType type, Vector2 p0)
    {
        Contour contour = this.current ??= this.GetContour();
        contour.Segments.Add((type, 1));
        contour.Points.Add(p0);
    }

    private void Add(SegmentType type, Vector2 p0, Vector2 p1)
    {
        Contour contour = this.current ??= this.GetContour();
        contour.Segments.Add((type, 2));
        contour.Points.Add(p0);
        contour.Points.Add(p1);
    }

    private void Add(SegmentType type, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        Contour contour = this.current ??= this.GetContour();
        contour.Segments.Add((type, 3));
        contour.Points.Add(p0);
        contour.Points.Add(p1);
        contour.Points.Add(p2);
    }

    private Contour GetContour()
    {
        int index = this.availableContours.Count - 1;
        if (index >= 0)
        {
            Contour contour = this.availableContours[index];
            this.availableContours.RemoveAt(index);
            return contour;
        }

        return new Contour();
    }

    private void Flush()
    {
        if (this.group.Count == 0)
        {
            return;
        }

        // FreeType determines one orientation for the complete outline using the non-zero winding
        // rule and the polygon formed by every point, including off-curve control points. Using the
        // group orientation makes outer contours grow while oppositely wound counters shrink.
        double area = 0D;
        foreach (Contour contour in this.group)
        {
            List<Vector2> points = contour.Points;
            Vector2 previous = points[^1];
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 current = points[i];
                area += ((double)current.Y - previous.Y) * (current.X + previous.X);
                previous = current;
            }
        }

        bool hasOrientation = area != 0D;
        bool trueTypeOrientation = area < 0D;

        foreach (Contour contour in this.group)
        {
            List<Vector2> points = contour.Points;
            if (hasOrientation)
            {
                Embolden(points, this.strength, trueTypeOrientation);
            }

            this.inner.BeginFigure();

            int index = 0;
            foreach ((SegmentType type, int count) in contour.Segments)
            {
                switch (type)
                {
                    case SegmentType.Move:
                        this.inner.MoveTo(points[index]);
                        break;
                    case SegmentType.Line:
                        this.inner.LineTo(points[index]);
                        break;
                    case SegmentType.Quadratic:
                        this.inner.QuadraticBezierTo(points[index], points[index + 1]);
                        break;
                    case SegmentType.Cubic:
                        this.inner.CubicBezierTo(points[index], points[index + 1], points[index + 2]);
                        break;
                }

                index += count;
            }

            this.inner.EndFigure();
        }

        this.RecycleGroup();
    }

    private void Reset()
    {
        if (this.current is not null)
        {
            this.current.Clear();
            this.availableContours.Add(this.current);
            this.current = null;
        }

        this.RecycleGroup();
        this.inner = null!;
        this.strength = 0F;
    }

    private void RecycleGroup()
    {
        // Keep each contour's backing arrays with the pooled renderer so subsequent glyphs
        // only overwrite retained storage instead of allocating per contour.
        foreach (Contour contour in this.group)
        {
            contour.Clear();
            this.availableContours.Add(contour);
        }

        this.group.Clear();
    }

    private sealed class Contour
    {
        public List<Vector2> Points { get; } = [];

        public List<(SegmentType Type, int Count)> Segments { get; } = [];

        public void Clear()
        {
            this.Points.Clear();
            this.Segments.Clear();
        }
    }

    private sealed class PooledObjectPolicy : IPooledObjectPolicy<EmboldeningGlyphRenderer>
    {
        /// <inheritdoc/>
        public EmboldeningGlyphRenderer Create() => new();

        /// <inheritdoc/>
        public bool Return(EmboldeningGlyphRenderer obj)
        {
            obj.Reset();
            return true;
        }
    }
}
