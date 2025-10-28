// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Rendering;

/// <summary>
/// Provides painted (layered) glyph rendering for color formats such as COLR v1 and OT-SVG.
/// Geometry and paints are supplied in document-space by an interpreter; all layout transforms
/// (UPEM mapping, DPI/point-size scaling, rotation, final placement) are applied here.
/// </summary>
public sealed class PaintedGlyphMetrics : GlyphMetrics
{
    private readonly IPaintedGlyphSource source;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaintedGlyphMetrics"/> class.
    /// </summary>
    /// <param name="font">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The code point.</param>
    /// <param name="source">The painted glyph source.</param>
    /// <param name="bounds">The design-space bounds for the glyph.</param>
    /// <param name="advanceWidth">The advance width.</param>
    /// <param name="advanceHeight">The advance height.</param>
    /// <param name="leftSideBearing">The left side bearing.</param>
    /// <param name="topSideBearing">The top side bearing.</param>
    /// <param name="unitsPerEM">Units per EM.</param>
    /// <param name="textAttributes">Text attributes.</param>
    /// <param name="textDecorations">Text decorations.</param>
    internal PaintedGlyphMetrics(
        StreamFontMetrics font,
        ushort glyphId,
        CodePoint codePoint,
        IPaintedGlyphSource source,
        Bounds bounds,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        TextAttributes textAttributes,
        TextDecorations textDecorations)
        : base(
              font,
              glyphId,
              codePoint,
              bounds,
              advanceWidth,
              advanceHeight,
              leftSideBearing,
              topSideBearing,
              unitsPerEM,
              textAttributes,
              textDecorations,
              GlyphType.Painted)
        => this.source = source;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaintedGlyphMetrics"/> class for rendering with overrides.
    /// </summary>
    internal PaintedGlyphMetrics(
        StreamFontMetrics font,
        ushort glyphId,
        CodePoint codePoint,
        IPaintedGlyphSource source,
        Bounds bounds,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        Vector2 offset,
        Vector2 scaleFactor,
        TextRun textRun)
        : base(
              font,
              glyphId,
              codePoint,
              bounds,
              advanceWidth,
              advanceHeight,
              leftSideBearing,
              topSideBearing,
              unitsPerEM,
              offset,
              scaleFactor,
              textRun,
              GlyphType.Painted)
        => this.source = source;

    /// <inheritdoc/>
    internal override GlyphMetrics CloneForRendering(TextRun textRun)
        => new PaintedGlyphMetrics(
            this.FontMetrics,
            this.GlyphId,
            this.CodePoint,
            this.source,
            this.Bounds,
            this.AdvanceWidth,
            this.AdvanceHeight,
            this.LeftSideBearing,
            this.TopSideBearing,
            this.UnitsPerEm,
            this.Offset,
            this.ScaleFactor,
            textRun);

    /// <inheritdoc/>
    internal override void RenderTo(
        IGlyphRenderer renderer,
        int graphemeIndex,
        Vector2 location,
        Vector2 offset,
        GlyphLayoutMode mode,
        TextOptions options)
    {
        if (ShouldSkipGlyphRendering(this.CodePoint))
        {
            return;
        }

        float pointSize = this.TextRun.Font?.Size ?? options.Font.Size;
        float dpi = options.Dpi;

        // Device-space placement.
        location *= dpi;
        offset *= dpi;
        Vector2 renderLocation = location + offset;

        float scaledPpem = this.GetScaledSize(pointSize, dpi);
        Vector2 scale = new Vector2(scaledPpem) / this.ScaleFactor; // uniform

        Matrix3x2 rotation = GetRotationMatrix(mode);

        // Layout similarity: uniform scale then rotation; translation added below.
        Matrix3x2 layout = Matrix3x2.CreateScale(scale);
        layout *= rotation;
        layout.Translation = (this.Offset * scale) + renderLocation;

        // Bounds in device space for BeginGlyph.
        FontRectangle box = this.GetBoundingBox(mode, renderLocation, scaledPpem);
        GlyphRendererParameters parameters = new(this, this.TextRun, pointSize, dpi, mode, graphemeIndex);

        if (renderer.BeginGlyph(in box, in parameters))
        {
            if (!UnicodeUtility.ShouldRenderWhiteSpaceOnly(this.CodePoint)
                && this.source.TryGetPaintedGlyph(this.GlyphId, out PaintedGlyph glyph, out PaintedCanvas canvas))
            {
                // Source-to-UPEM: viewBox mapping (uniform "meet"), optional y-flip, optional root transform.
                Matrix3x2 s2u = ComputeSourceToUpem(canvas, this.UnitsPerEm);

                // Full transform from source doc-space to device space.
                Matrix3x2 total = s2u * layout;

                // Stream layers and commands with correct transforms.
                StreamPaintedGlyph(glyph, in box, renderer, total);
            }

            renderer.EndGlyph();
            this.RenderDecorationsTo(renderer, location, mode, rotation, scaledPpem, options);
        }
    }

    /// <summary>
    /// Computes the mapping from the interpreter's document-space to UPEM font space.
    /// Enforces a uniform 'meet' scale from the root viewBox (if present) and flips Y
    /// only if the source is y-up.
    /// </summary>
    private static Matrix3x2 ComputeSourceToUpem(in PaintedCanvas canvas, ushort upem)
    {
        Matrix3x2 m = Matrix3x2.Identity;

        // Root transform (doc-space). Apply first if provided.
        if (!canvas.RootTransform.IsIdentity)
        {
            m *= canvas.RootTransform;
        }

        // Translate viewBox min to origin, then uniform scale to UPEM using "meet".
        if (canvas.HasViewBox)
        {
            Matrix3x2 t = Matrix3x2.CreateTranslation(-canvas.ViewBox.X, -canvas.ViewBox.Y);

            float sx = upem / Math.Max(canvas.ViewBox.Width, 1e-6f);
            float sy = upem / Math.Max(canvas.ViewBox.Height, 1e-6f);
            float s = MathF.Min(sx, sy);

            Matrix3x2 sUni = Matrix3x2.CreateScale(s);

            m = m * t * sUni;
        }

        // Coordinate system orientation.
        if (!canvas.IsYDown)
        {
            // Flip Y around the origin; placement happens in layout.
            m *= Matrix3x2.CreateScale(1f, -1f);
        }

        return m;
    }

    /// <summary>
    /// Streams the painted glyph to the renderer, transforming geometry and userSpaceOnUse paints.
    /// </summary>
    /// <param name="glyph">The painted glyph.</param>
    /// <param name="bounds">The device-space bounds of the glyph.</param>
    /// <param name="renderer">The glyph renderer.</param>
    /// <param name="xform">The full device-space transform to apply.</param>
    private static void StreamPaintedGlyph(
        in PaintedGlyph glyph,
        in FontRectangle bounds,
        IGlyphRenderer renderer,
        Matrix3x2 xform)
    {
        IReadOnlyList<PaintedLayer> layers = glyph.Layers;
        for (int i = 0; i < layers.Count; i++)
        {
            PaintedLayer layer = layers[i];

            // pre-applied transforms (element/group)
            Matrix3x2 layerXform = layer.Transform * xform;

            // Clip bounds in device space (if any).
            ClipQuad? clipBounds = layer.ClipBounds.HasValue
                ? ClipQuad.FromBounds(layer.ClipBounds.Value, layerXform)
                : null;

            // Similarity decomposition for arc radii/angle/sweep adjustment (from layer).
            Similarity sim = Similarity.FromMatrix(layerXform);

            // Transform userSpaceOnUse paints into device space; keep ObjectBoundingBox normalized.
            Paint? paint = TransformPaint(layer.Paint, in bounds, layerXform);

            renderer.BeginLayer(paint, layer.FillRule, clipBounds);

            bool open = false;
            IReadOnlyList<PathCommand> cmds = layer.Path;

            for (int j = 0; j < cmds.Count; j++)
            {
                PathCommand c = cmds[j];
                switch (c.Verb)
                {
                    case PathVerb.MoveTo:
                    {
                        if (!open)
                        {
                            renderer.BeginFigure();
                            open = true;
                        }

                        renderer.MoveTo(Vector2.Transform(c.EndPoint, layerXform));
                        break;
                    }

                    case PathVerb.LineTo:
                    {
                        renderer.LineTo(Vector2.Transform(c.EndPoint, layerXform));
                        break;
                    }

                    case PathVerb.QuadraticTo:
                    {
                        renderer.QuadraticBezierTo(
                            Vector2.Transform(c.ControlPoint1, layerXform),
                            Vector2.Transform(c.EndPoint, layerXform));
                        break;
                    }

                    case PathVerb.CubicTo:
                    {
                        renderer.CubicBezierTo(
                            Vector2.Transform(c.ControlPoint1, layerXform),
                            Vector2.Transform(c.ControlPoint2, layerXform),
                            Vector2.Transform(c.EndPoint, layerXform));
                        break;
                    }

                    case PathVerb.ArcTo:
                    {
                        // Adjust radii by the scale component of the transform;
                        // angle/sweep by the similarity component;
                        // endpoint is fully transformed.
                        float rx = c.RadiusX * layerXform.M11;
                        float ry = c.RadiusY * layerXform.M12;
                        float ang = c.RotationDegrees + sim.RotationDegrees;
                        bool sweep = sim.Reflection ? !c.Sweep : c.Sweep;

                        renderer.ArcTo(rx, ry, ang, c.LargeArc, sweep, Vector2.Transform(c.EndPoint, layerXform));
                        break;
                    }

                    case PathVerb.ClosePath:
                    {
                        if (open)
                        {
                            renderer.EndFigure();
                            open = false;
                        }

                        break;
                    }
                }
            }

            if (open)
            {
                renderer.EndFigure();
            }

            renderer.EndLayer();
        }
    }

    /// <summary>
    /// Converts a <see cref="Paint"/> into device-space geometry for the target layer,
    /// removing (baking in) any paint-local transforms. Geometry path commands have already
    /// been transformed elsewhere; this method only resolves paint geometry (start/end points,
    /// centers, radii, angles) into device space so the renderer can construct brushes directly.
    /// <para>
    /// Rules:
    /// <list type="bullet">
    ///   <item><description><b>UserSpaceOnUse</b>: Apply <see cref="Paint.Transform"/> in user space, then apply
    ///   <paramref name="layerXform"/> to obtain device-space positions. Emit device-space values.</description></item>
    ///   <item><description><b>ObjectBoundingBox</b>: Apply <see cref="Paint.Transform"/> in normalized [0..1] box space,
    ///   then denormalize to device space using <paramref name="layerBounds"/>. Emit device-space values.</description></item>
    ///   <item><description>Color stops (ratios) remain normalized in [0..1] and are passed through unchanged.</description></item>
    ///   <item><description>All returned paints have identity <see cref="Paint.Transform"/> and are suitable for direct
    ///   consumption by Drawing brushes (e.g. <c>LinearGradientBrush</c> expects device-space points).</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="paint">The source paint, or <see langword="null"/>.</param>
    /// <param name="layerBounds">The device-space axis-aligned bounding box of the current layer’s geometry.</param>
    /// <param name="layerXform">
    /// The full device-space transform applied to this layer’s geometry (e.g., layer * s2u * layout).
    /// Used to push UserSpaceOnUse paints into device space. ObjectBoundingBox paints are denormalized
    /// using <paramref name="layerBounds"/> instead.
    /// </param>
    /// <returns>
    /// A paint expressed in <b>device-space</b> with identity transform, or <see langword="null"/>
    /// if the input was <see langword="null"/>.
    /// </returns>
    private static Paint? TransformPaint(
        Paint? paint,
        in FontRectangle layerBounds,
        Matrix3x2 layerXform)
    {
        if (paint is null)
        {
            return null;
        }

        switch (paint)
        {
            case SolidPaint s:
            {
                return s;
            }

            case LinearGradientPaint lg:
            {
                Vector2 p0;
                Vector2 p1;
                Vector2? p2;

                if (lg.Units == GradientUnits.UserSpaceOnUse)
                {
                    // USOU: transform directly to device space.
                    Matrix3x2 paintXForm = lg.Transform * layerXform;
                    p0 = Vector2.Transform(lg.P0, paintXForm);
                    p1 = Vector2.Transform(lg.P1, paintXForm);
                    p2 = lg.P2.HasValue ? Vector2.Transform(lg.P2.Value, paintXForm) : null;
                }
                else
                {
                    // OBB: transform in normalized [0..1] space, then denormalize to device via layer bounds.
                    Vector2 n0 = Vector2.Transform(lg.P0, lg.Transform);
                    Vector2 n1 = Vector2.Transform(lg.P1, lg.Transform);
                    Vector2? n2 = lg.P2.HasValue ? Vector2.Transform(lg.P2.Value, lg.Transform) : null;

                    p0 = Vector2.Transform(DenormalizePoint(n0, layerBounds), layerXform);
                    p1 = Vector2.Transform(DenormalizePoint(n1, layerBounds), layerXform);
                    p2 = n2.HasValue ? Vector2.Transform(DenormalizePoint(n2.Value, layerBounds), layerXform) : null;
                }

                return new LinearGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    P0 = p0,
                    P1 = p1,
                    P2 = p2,
                    Spread = lg.Spread,
                    Stops = lg.Stops,
                    Opacity = lg.Opacity,
                    Transform = Matrix3x2.Identity
                };
            }

            case RadialGradientPaint rg:
            {
                Vector2 c0;
                Vector2 c1;
                float r0;
                float r1;

                if (rg.Units == GradientUnits.UserSpaceOnUse)
                {
                    // USOU: transform directly to device space.
                    Matrix3x2 paintXForm = rg.Transform * layerXform;

                    // Centers get full layer transform.
                    c0 = Vector2.Transform(rg.Center0, paintXForm);
                    c1 = Vector2.Transform(rg.Center1, paintXForm);

                    // Radii scale by uniform similarity only.
                    Similarity compSim = Similarity.FromMatrix(paintXForm);
                    r0 = rg.Radius0 * compSim.Scale;
                    r1 = rg.Radius1 * compSim.Scale;
                }
                else
                {
                    // OBB: transform in normalized [0..1] space, then denormalize to device via layer bounds.
                    Vector2 nc0 = Vector2.Transform(rg.Center0, rg.Transform);
                    Vector2 nc1 = Vector2.Transform(rg.Center1, rg.Transform);

                    c0 = Vector2.Transform(DenormalizePoint(nc0, layerBounds), layerXform);
                    c1 = Vector2.Transform(DenormalizePoint(nc1, layerBounds), layerXform);

                    // Radii scale by total similarity (paint * layer).
                    Matrix3x2 paintXForm = rg.Transform * layerXform;
                    Similarity compSim = Similarity.FromMatrix(paintXForm);
                    r0 = rg.Radius0 * compSim.Scale;
                    r1 = rg.Radius1 * compSim.Scale;
                }

                return new RadialGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center0 = c0,
                    Radius0 = r0,
                    Center1 = c1,
                    Radius1 = r1,
                    Spread = rg.Spread,
                    Stops = rg.Stops,
                    Opacity = rg.Opacity,
                    Transform = Matrix3x2.Identity
                };
            }

            case SweepGradientPaint sg:
            {
                Vector2 center;
                float start = sg.StartAngle;
                float end = sg.EndAngle;

                if (sg.Units == GradientUnits.UserSpaceOnUse)
                {
                    // USOU: transform directly to device space.
                    Matrix3x2 paintXForm = sg.Transform * layerXform;

                    // Center gets full layer transform.
                    center = Vector2.Transform(sg.Center, paintXForm);

                    // Angles adjust by similarity rotation and reflection only.
                    Similarity compSim = Similarity.FromMatrix(paintXForm);
                    start += compSim.RotationDegrees;
                    end += compSim.RotationDegrees;
                    if (compSim.Reflection)
                    {
                        (start, end) = (end, start);
                    }
                }
                else
                {
                    // OBB: transform in normalized [0..1] space, then denormalize to device via layer bounds.
                    Vector2 nc = Vector2.Transform(sg.Center, sg.Transform);
                    center = Vector2.Transform(DenormalizePoint(nc, layerBounds), layerXform);

                    // Angles adjust by total similarity (paint * layer).
                    Matrix3x2 paintXForm = sg.Transform * layerXform;
                    Similarity compSim = Similarity.FromMatrix(paintXForm);
                    start += compSim.RotationDegrees;
                    end += compSim.RotationDegrees;
                    if (compSim.Reflection)
                    {
                        (start, end) = (end, start);
                    }
                }

                return new SweepGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center = center,
                    StartAngle = start,
                    EndAngle = end,
                    Spread = sg.Spread,
                    Stops = sg.Stops,
                    Opacity = sg.Opacity,
                    Transform = Matrix3x2.Identity
                };
            }

            default:
            {
                return paint;
            }
        }

        static Vector2 DenormalizePoint(Vector2 p, in FontRectangle bounds)
            => new(bounds.X + (p.X * bounds.Width), bounds.Y + (p.Y * bounds.Height));
    }

    /// <summary>
    /// Represents the similarity component of a 2D affine transformation.
    /// </summary>
    /// <remarks>
    /// A similarity transformation is an affine transform that preserves an object's shape and angles,
    /// allowing only uniform scaling, rotation, and optional reflection. This structure isolates those
    /// properties from a general <see cref="Matrix3x2"/> so that dependent operations such as arc or
    /// gradient adjustment can apply proportional transformations correctly.
    /// </remarks>
    private readonly struct Similarity
    {
        private Similarity(float scale, float rotationDeg, bool reflection, bool isSimilarity)
        {
            this.Scale = scale;
            this.RotationDegrees = rotationDeg;
            this.Reflection = reflection;
            this.IsSimilarity = isSimilarity;
        }

        /// <summary>
        /// Gets the length of the first column.
        /// </summary>
        public float Scale { get; }

        /// <summary>
        /// Gets the rotation in degrees.
        /// </summary>
        public float RotationDegrees { get; }

        /// <summary>
        /// Gets a value indicating whether this matrix includes a reflection.</summary>
        public bool Reflection { get; }

        /// <summary>
        /// Gets a value indicating whether this matrix is a similarity transform.
        /// True if columns are orthogonal and equal length within tolerance.
        /// </summary>
        public bool IsSimilarity { get; }

        public static Similarity FromMatrix(in Matrix3x2 m)
        {
            float a = m.M11, b = m.M12, c = m.M21, d = m.M22;

            // scale = |X column|
            float sx = MathF.Sqrt((a * a) + (b * b));

            // rotation from X column
            float rotDeg = MathF.Atan2(b, a) * (180f / MathF.PI);

            // reflection from determinant
            bool refl = ((a * d) - (b * c)) < 0f;

            // similarity test: columns orthogonal and same length
            float dot = (a * c) + (b * d);
            float sy = MathF.Sqrt((c * c) + (d * d));
            const float eps = 1e-4f;
            bool ortho = MathF.Abs(dot) <= eps;
            bool equal = MathF.Abs(sx - sy) <= eps;

            return new Similarity(sx, rotDeg, refl, ortho && equal && sx > 0f);
        }
    }
}
