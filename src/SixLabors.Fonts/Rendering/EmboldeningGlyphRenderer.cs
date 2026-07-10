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
/// Outline contours are buffered per fill group (a color layer, or the whole glyph when no
/// layers are present) so that the group's overall winding can be determined. Every point,
/// including off-curve control points, is then shifted along the local outline normal using a
/// mitred offset, which grows the filled area while shrinking any counters, just as a real bold
/// weight would. The dilated contours are replayed to the wrapped renderer, preserving the
/// original segment types.
/// </remarks>
internal sealed class EmboldeningGlyphRenderer : IGlyphRenderer
{
    private readonly IGlyphRenderer inner;
    private readonly float strength;
    private readonly List<Contour> group = new();
    private Contour? current;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmboldeningGlyphRenderer"/> class.
    /// </summary>
    /// <param name="inner">The renderer that receives the dilated outline.</param>
    /// <param name="strength">The outward offset applied to each outline edge, in pixels.</param>
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
    public void BeginFigure() => this.current = new Contour();

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

    private static Vector2[] Offset(List<Vector2> points, float strength)
    {
        int n = points.Count;
        Vector2[] result = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 p = points[i];
            Vector2 e1 = Normalize(p - points[(i - 1 + n) % n]);
            Vector2 e2 = Normalize(points[(i + 1) % n] - p);

            // Edge normals (rotate each edge direction by -90 degrees).
            Vector2 n1 = new(e1.Y, -e1.X);
            Vector2 n2 = new(e2.Y, -e2.X);
            Vector2 sum = n1 + n2;

            Vector2 direction;
            if (sum == Vector2.Zero)
            {
                direction = n1 == Vector2.Zero ? n2 : n1;
            }
            else
            {
                // Mitre so that each adjacent edge moves out by exactly the strength.
                // The denominator is clamped to tame the mitre spike at sharp corners.
                float d = 1F + Vector2.Dot(n1, n2);
                direction = sum / MathF.Max(d, 0.25F);
            }

            result[i] = p + (strength * direction);
        }

        return result;
    }

    private static Vector2 Normalize(Vector2 v)
    {
        float length = v.Length();
        return length < 1e-6F ? Vector2.Zero : v / length;
    }

    private static float SignedArea(List<Vector2> points)
    {
        float area = 0F;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            area += (points[j].X * points[i].Y) - (points[i].X * points[j].Y);
        }

        return area * 0.5F;
    }

    private void Add(SegmentType type, Vector2 p0)
    {
        Contour contour = this.current ??= new Contour();
        contour.Segments.Add((type, 1));
        contour.Points.Add(p0);
    }

    private void Add(SegmentType type, Vector2 p0, Vector2 p1)
    {
        Contour contour = this.current ??= new Contour();
        contour.Segments.Add((type, 2));
        contour.Points.Add(p0);
        contour.Points.Add(p1);
    }

    private void Add(SegmentType type, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        Contour contour = this.current ??= new Contour();
        contour.Segments.Add((type, 3));
        contour.Points.Add(p0);
        contour.Points.Add(p1);
        contour.Points.Add(p2);
    }

    private void Flush()
    {
        if (this.group.Count == 0)
        {
            return;
        }

        // Grow the fill outward regardless of the source outline's winding convention:
        // a positive total area means the group is wound counter-clockwise, for which the
        // edge normals already point outward, otherwise the offset direction is reversed.
        // Only on-curve anchor points are used so that extreme cubic control points cannot
        // distort the winding calculation.
        float area = 0F;
        foreach (Contour contour in this.group)
        {
            area += SignedArea(contour.Anchors());
        }

        float signedStrength = (area >= 0F ? 1F : -1F) * this.strength;

        foreach (Contour contour in this.group)
        {
            Vector2[] offset = Offset(contour.Points, signedStrength);
            this.inner.BeginFigure();

            int index = 0;
            foreach ((SegmentType type, int count) in contour.Segments)
            {
                switch (type)
                {
                    case SegmentType.Move:
                        this.inner.MoveTo(offset[index]);
                        break;
                    case SegmentType.Line:
                        this.inner.LineTo(offset[index]);
                        break;
                    case SegmentType.Quadratic:
                        this.inner.QuadraticBezierTo(offset[index], offset[index + 1]);
                        break;
                    case SegmentType.Cubic:
                        this.inner.CubicBezierTo(offset[index], offset[index + 1], offset[index + 2]);
                        break;
                }

                index += count;
            }

            this.inner.EndFigure();
        }

        this.group.Clear();
    }

    private sealed class Contour
    {
        public List<Vector2> Points { get; } = new();

        public List<(SegmentType Type, int Count)> Segments { get; } = new();

        /// <summary>
        /// Gets the on-curve anchor points of the contour, i.e. the endpoint of each segment,
        /// which describe the contour's winding independently of any off-curve control points.
        /// </summary>
        /// <returns>The anchor points.</returns>
        public List<Vector2> Anchors()
        {
            List<Vector2> anchors = new(this.Segments.Count);
            int index = 0;
            foreach ((SegmentType _, int count) in this.Segments)
            {
                index += count;
                anchors.Add(this.Points[index - 1]);
            }

            return anchors;
        }
    }
}
