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
    /// <param name="glyphType">The glyph type.</param>
    /// <param name="glyphColor">Optional solid color (used by outline/legacy paths).</param>
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
        TextDecorations textDecorations,
        GlyphType glyphType = GlyphType.Layer,
        GlyphColor? glyphColor = null)
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
              glyphType,
              glyphColor)
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
        TextRun textRun,
        GlyphType glyphType = GlyphType.Layer,
        GlyphColor? glyphColor = null)
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
              glyphType,
              glyphColor)
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
            textRun,
            this.GlyphType,
            this.GlyphColor);

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
    private static void StreamPaintedGlyph(in PaintedGlyph glyph, in FontRectangle bounds, IGlyphRenderer renderer, Matrix3x2 xform)
    {
        ReadOnlySpan<PaintedLayer> layers = glyph.Layers.Span;
        for (int i = 0; i < layers.Length; i++)
        {
            PaintedLayer layer = layers[i];

            // pre-applied transforms (element/group)
            Matrix3x2 layerXform = layer.Transform * xform;

            // Similarity decomposition for arc radii/angle/sweep adjustment (from layer).
            Similarity sim = Similarity.FromMatrix(layerXform);

            // Transform userSpaceOnUse paints into device space; keep ObjectBoundingBox normalized.
            Paint? paint = TransformPaint(layer.Paint, in bounds, layerXform, in sim);

            renderer.BeginLayer(paint, layer.FillRule);

            bool open = false;
            ReadOnlySpan<PathCommand> cmds = layer.Path.Span;

            for (int j = 0; j < cmds.Length; j++)
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
                        // Adjust radii/angle/sweep by the similarity component; endpoint is fully transformed.
                        float rx = c.RadiusX * sim.Scale;
                        float ry = c.RadiusY * sim.Scale;
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
    /// <param name="layerSim">
    /// Similarity component (uniform scale, rotation, reflection) of <paramref name="layerXform"/>.
    /// Not required by this implementation (angles/radii use the composite matrices directly),
    /// but kept for parity with geometry handling.
    /// </param>
    /// <returns>
    /// A paint expressed in <b>device-space</b> with identity transform, or <see langword="null"/>
    /// if the input was <see langword="null"/>.
    /// </returns>
    private static Paint? TransformPaint(
        Paint? paint,
        in FontRectangle layerBounds,
        Matrix3x2 layerXform,
        in Similarity layerSim)
    {
        if (paint is null)
        {
            return null;
        }

        _ = layerSim; // not used here

        float left = layerBounds.X;
        float top = layerBounds.Y;
        float width = MathF.Max(layerBounds.Width, 1e-6f);
        float height = MathF.Max(layerBounds.Height, 1e-6f);

        switch (paint)
        {
            case SolidPaint s:
            {
                // Solids have no geometry to resolve.
                return s;
            }

            case LinearGradientPaint lg:
            {
                Vector2 p0;
                Vector2 p1;

                if (lg.Units == GradientUnits.UserSpaceOnUse)
                {
                    // USO: user -> paint.Transform -> layerXform -> device
                    Vector2 u0 = Vector2.Transform(lg.P0, lg.Transform);
                    Vector2 u1 = Vector2.Transform(lg.P1, lg.Transform);

                    p0 = Vector2.Transform(u0, layerXform);
                    p1 = Vector2.Transform(u1, layerXform);
                }
                else
                {
                    // OBB: normalized -> paint.Transform (normalized) -> denormalize via layer bounds -> device
                    Vector2 n0 = Vector2.Transform(lg.P0, lg.Transform);
                    Vector2 n1 = Vector2.Transform(lg.P1, lg.Transform);

                    p0 = DenormalizePoint(n0, left, top, width, height);
                    p1 = DenormalizePoint(n1, left, top, width, height);
                }

                return new LinearGradientPaint
                {
                    // Emit device-space paint with identity transform.
                    Units = GradientUnits.UserSpaceOnUse,
                    P0 = p0,
                    P1 = p1,
                    Spread = lg.Spread,
                    Stops = lg.Stops,
                    Opacity = lg.Opacity,
                    Transform = Matrix3x2.Identity
                };
            }

            case RadialGradientPaint rg:
            {
                // Compute center/focal and radius by transforming two points and measuring distance.
                // This captures uniform/non-uniform scale and rotation without guessing.
                Vector2 center;
                Vector2 focal;
                float radius;

                if (rg.Units == GradientUnits.UserSpaceOnUse)
                {
                    // USO center/focal in user -> paint.Transform -> layerXform -> device
                    Vector2 uc = Vector2.Transform(rg.Center, rg.Transform);
                    Vector2 uf = Vector2.Transform(rg.Focal ?? rg.Center, rg.Transform);

                    center = Vector2.Transform(uc, layerXform);
                    focal = Vector2.Transform(uf, layerXform);

                    // Radius: transform a point offset by (radius, 0) from center through both matrices.
                    Vector2 uEdge = Vector2.Transform(rg.Center + new Vector2(rg.Radius, 0f), rg.Transform);
                    Vector2 dEdge = Vector2.Transform(uEdge, layerXform);
                    radius = Vector2.Distance(dEdge, center);
                }
                else
                {
                    // OBB center/focal in normalized -> paint.Transform (normalized) -> denormalize -> device
                    Vector2 nc = Vector2.Transform(rg.Center, rg.Transform);
                    Vector2 nf = Vector2.Transform(rg.Focal ?? rg.Center, rg.Transform);

                    center = DenormalizePoint(nc, left, top, width, height);
                    focal = DenormalizePoint(nf, left, top, width, height);

                    // Radius: same two-point trick in normalized space, then denormalize both and measure.
                    Vector2 nEdge = Vector2.Transform(rg.Center + new Vector2(rg.Radius, 0f), rg.Transform);
                    Vector2 dEdge = DenormalizePoint(nEdge, left, top, width, height);
                    radius = Vector2.Distance(dEdge, center);
                }

                return new RadialGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center = center,
                    Focal = focal,
                    Radius = radius,
                    Spread = rg.Spread,
                    Stops = rg.Stops,
                    Opacity = rg.Opacity,
                    Transform = Matrix3x2.Identity
                };
            }

            case SweepGradientPaint sg:
            {
                // Center to device; angles adjusted by rotation/reflection from appropriate composite.
                Vector2 center;
                float start = sg.StartAngle;
                float end = sg.EndAngle;

                if (sg.Units == GradientUnits.UserSpaceOnUse)
                {
                    // Composite: paint.Transform then layerXform.
                    Matrix3x2 comp = sg.Transform * layerXform;
                    Similarity compSim = Similarity.FromMatrix(comp);

                    Vector2 uc = Vector2.Transform(sg.Center, sg.Transform);
                    center = Vector2.Transform(uc, layerXform);

                    start += compSim.RotationDegrees;
                    end += compSim.RotationDegrees;
                    if (compSim.Reflection)
                    {
                        (start, end) = (end, start);
                    }
                }
                else
                {
                    // OBB: center in normalized -> paint.Transform (normalized) -> denormalize; angles by paint.Transform only.
                    Vector2 nc = Vector2.Transform(sg.Center, sg.Transform);
                    center = DenormalizePoint(nc, left, top, width, height);

                    Similarity paintSim = Similarity.FromMatrix(sg.Transform);
                    start += paintSim.RotationDegrees;
                    end += paintSim.RotationDegrees;
                    if (paintSim.Reflection)
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
                // Unknown paint: return as-is (defensive).
                return paint;
            }
        }

        static Vector2 DenormalizePoint(in Vector2 p, float left, float top, float width, float height)
        {
            float x = left + (p.X * width);
            float y = top + (p.Y * height);
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// Captures the similarity component of a 2D affine transform (uniform scale, rotation, reflection).
    /// </summary>
    private readonly struct Similarity
    {
        public Similarity(bool isSimilarity, float scale, float rotationDeg, bool reflection)
        {
            this.IsSimilarity = isSimilarity;
            this.Scale = scale;
            this.RotationDegrees = rotationDeg;
            this.Reflection = reflection;
        }

        public bool IsSimilarity { get; }

        public float Scale { get; }

        public float RotationDegrees { get; }

        public bool Reflection { get; }

        public static Similarity FromMatrix(in Matrix3x2 m)
        {
            // Columns: [a b; c d]
            float a = m.M11;
            float b = m.M12;
            float c = m.M21;
            float d = m.M22;

            float dot = (a * c) + (b * d);
            float len0 = MathF.Sqrt((a * a) + (b * b));
            float len1 = MathF.Sqrt((c * c) + (d * d));

            bool ortho = MathF.Abs(dot) < 1e-4f;
            bool equal = MathF.Abs(len0 - len1) < 1e-4f;

            if (!ortho || !equal || len0 == 0f)
            {
                // Fallback: treat as no-op for arc param adjustment; endpoints are still fully transformed.
                return new Similarity(false, 1f, 0f, false);
            }

            float rotDeg = MathF.Atan2(b, a) * (180f / (float)Math.PI);
            bool refl = ((a * d) - (b * c)) < 0f;

            return new Similarity(true, len0, rotDeg, refl);
        }
    }
}
