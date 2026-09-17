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
public sealed class PaintedGlyphMetrics : FontGlyphMetrics
{
    private readonly IPaintedGlyphSource source;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaintedGlyphMetrics"/> class.
    /// </summary>
    /// <param name="font">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The code point.</param>
    /// <param name="source">The painted glyph source.</param>
    /// <param name="palette">The color palette selection the source resolves colors with, or null for the font's default palette.</param>
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
        FontPalette? palette,
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
    {
        this.source = source;
        this.FontPalette = palette;
    }

    /// <summary>
    /// Gets the color palette selection the glyph's colors were resolved with, or
    /// <see langword="null"/> for the font's default palette. Participates in renderer
    /// cache identity through <see cref="GlyphRendererParameters"/>.
    /// </summary>
    internal override FontPalette? FontPalette { get; }

    /// <inheritdoc/>
    internal override Bounds GetDesignBounds()
    {
        if (!this.source.TryGetPaintedGlyph(this.GlyphId, out PaintedGlyph glyph, out PaintedCanvasMetadata canvas))
        {
            return base.GetDesignBounds();
        }

        Matrix3x2 sourceToUpem = ComputeSourceToUpem(canvas, this.UnitsPerEm);
        return Bounds.Transform(glyph.Bounds, sourceToUpem);
    }

    /// <inheritdoc/>
    internal override void RenderOutlineTo(
        IGlyphRenderer renderer,
        Vector2 glyphOrigin,
        GlyphLayoutMode mode,
        TextRun? textRun,
        Vector2 positionOffset,
        Vector2 positionedAdvance,
        float scaledPPEM,
        HintingMode hintingMode)
    {
        if (!this.source.TryGetPaintedGlyph(this.GlyphId, out PaintedGlyph glyph, out PaintedCanvasMetadata canvas))
        {
            return;
        }

        FontRectangle box = this.GetBoundingBox(mode, glyphOrigin, scaledPPEM, textRun, positionOffset, positionedAdvance);

        // Full transform from source doc-space to device space.
        Matrix3x2 total = this.ComputeTotalTransform(in canvas, glyphOrigin, mode, textRun, positionOffset, scaledPPEM);

        // Stream layers and commands with correct transforms.
        StreamPaintedGlyph(glyph, in box, renderer, total);
    }

    /// <inheritdoc/>
    internal override ClipBounds? GetClipBounds(
        Vector2 glyphOrigin,
        GlyphLayoutMode mode,
        TextRun? textRun,
        Vector2 positionOffset,
        float scaledPPEM)
    {
        if (!this.source.TryGetPaintedGlyph(this.GlyphId, out PaintedGlyph glyph, out PaintedCanvasMetadata canvas)
            || !glyph.ClipBounds.HasValue)
        {
            return null;
        }

        // Clip bounds live in the design grid, outside the paint graph, so only the root
        // design-to-device transform applies to them. The renderer receives them untransformed
        // and restricts each operation with one rectangle intersect.
        Bounds clip = glyph.ClipBounds.Value;
        Matrix3x2 total = this.ComputeTotalTransform(in canvas, glyphOrigin, mode, textRun, positionOffset, scaledPPEM);
        return new ClipBounds(FontRectangle.FromLTRB(clip.Min.X, clip.Min.Y, clip.Max.X, clip.Max.Y), total);
    }

    /// <summary>
    /// Computes the full transform from the interpreter's document space to device space:
    /// the source-to-UPEM mapping followed by the scale, offset, oblique, rotation, Y
    /// inversion, and final placement sequence shared with TrueType and CFF outlines.
    /// </summary>
    /// <param name="canvas">The painted canvas metadata.</param>
    /// <param name="glyphOrigin">The origin used to render the glyph outline, in device pixels.</param>
    /// <param name="mode">The glyph layout mode to render using.</param>
    /// <param name="textRun">The text run providing the styling information for this glyph.</param>
    /// <param name="positionOffset">The positioned placement offset in font design units.</param>
    /// <param name="scaledPPEM">The scaled pixels-per-em value used to scale the outline.</param>
    /// <returns>The document-space to device-space transform.</returns>
    private Matrix3x2 ComputeTotalTransform(
        in PaintedCanvasMetadata canvas,
        Vector2 glyphOrigin,
        GlyphLayoutMode mode,
        TextRun? textRun,
        Vector2 positionOffset,
        float scaledPPEM)
    {
        Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor; // uniform
        Matrix3x2 outlineTransform = this.GetOutlineTransform(mode, textRun);

        // Keep painted geometry in Y-up font space through the same scale, offset, oblique, and
        // rotation sequence used by TrueType and CFF, then perform the device-space Y inversion.
        Matrix3x2 layout = Matrix3x2.CreateScale(scale);
        layout.Translation = (this.Offset + positionOffset) * scale;
        layout *= outlineTransform;
        layout *= Matrix3x2.CreateScale(1F, -1F);
        layout.Translation += glyphOrigin;

        // Source-to-UPEM: viewBox mapping (uniform "meet"), optional y-flip, optional root transform.
        return ComputeSourceToUpem(canvas, this.UnitsPerEm) * layout;
    }

    /// <summary>
    /// Computes the mapping from the interpreter's document-space to UPEM font space.
    /// Enforces a uniform 'meet' scale from the root viewBox (if present) and normalizes
    /// Y-down document coordinates to the Y-up glyph-metrics coordinate system.
    /// </summary>
    private static Matrix3x2 ComputeSourceToUpem(in PaintedCanvasMetadata canvas, ushort upem)
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

        // Normalize every painted source to the Y-up font-space contract used by glyph metrics.
        if (canvas.IsYDown)
        {
            // SVG document coordinates are Y-down; COLR outlines are already Y-up.
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
        IReadOnlyList<PaintedCompositeCommand>? compositeCommands = glyph.CompositeCommands;
        int compositeCommandIndex = 0;

        for (int i = 0; i < layers.Count; i++)
        {
            StreamCompositeCommands(
                compositeCommands,
                i,
                renderer,
                ref compositeCommandIndex);

            PaintedLayer layer = layers[i];

            // pre-applied transforms (element/group)
            Matrix3x2 layerXform = layer.Transform * xform;

            // Similarity decomposition for arc radii/angle/sweep adjustment (from layer).
            Similarity sim = Similarity.FromMatrix(layerXform);

            // Transform userSpaceOnUse paints into device space; keep ObjectBoundingBox normalized.
            Paint? paint = TransformPaint(layer.Paint, in bounds, layerXform);

            if (layer.Path.Count == 0)
            {
                // The paint owns no outline, so its figure is the clip bounds, or the glyph
                // bounds when the font defines none. Both sit outside the paint graph: the
                // clip bounds take only the root design-to-device transform, and the glyph
                // bounds are already in device space. The paint above keeps its own transforms.
                Vector2 c0;
                Vector2 c1;
                Vector2 c2;
                Vector2 c3;
                if (glyph.ClipBounds.HasValue)
                {
                    Bounds clip = glyph.ClipBounds.Value;
                    c0 = Vector2.Transform(clip.Min, xform);
                    c1 = Vector2.Transform(new Vector2(clip.Max.X, clip.Min.Y), xform);
                    c2 = Vector2.Transform(clip.Max, xform);
                    c3 = Vector2.Transform(new Vector2(clip.Min.X, clip.Max.Y), xform);
                }
                else
                {
                    c0 = new Vector2(bounds.Left, bounds.Top);
                    c1 = new Vector2(bounds.Right, bounds.Top);
                    c2 = new Vector2(bounds.Right, bounds.Bottom);
                    c3 = new Vector2(bounds.Left, bounds.Bottom);
                }

                renderer.BeginLayer(paint, layer.FillRule);
                renderer.BeginFigure();
                renderer.MoveTo(c0);
                renderer.LineTo(c1);
                renderer.LineTo(c2);
                renderer.LineTo(c3);
                renderer.EndFigure();
                renderer.EndLayer();
                continue;
            }

            renderer.BeginLayer(paint, layer.FillRule);

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

        StreamCompositeCommands(
            compositeCommands,
            layers.Count,
            renderer,
            ref compositeCommandIndex);
    }

    /// <summary>
    /// Streams group transitions that occur before a painted layer.
    /// </summary>
    /// <param name="commands">The optional group command stream.</param>
    /// <param name="layerIndex">The index of the next painted layer.</param>
    /// <param name="renderer">The glyph renderer.</param>
    /// <param name="commandIndex">The index of the next group command.</param>
    private static void StreamCompositeCommands(
        IReadOnlyList<PaintedCompositeCommand>? commands,
        int layerIndex,
        IGlyphRenderer renderer,
        ref int commandIndex)
    {
        if (commands is null)
        {
            return;
        }

        // Several nested group transitions can share a layer boundary. Retaining list order
        // preserves the exact depth-first traversal without allocating a render-time stack here.
        while (commandIndex < commands.Count && commands[commandIndex].LayerIndex == layerIndex)
        {
            PaintedCompositeCommand command = commands[commandIndex++];
            if (command.Kind == PaintedCompositeCommandKind.Begin)
            {
                renderer.BeginGroup(command.Mode);
            }
            else
            {
                renderer.EndGroup();
            }
        }
    }

    /// <summary>
    /// Resolves a <see cref="Paint"/> for the target layer. Geometry path commands have already
    /// been transformed elsewhere; this method only resolves the paint geometry so the renderer
    /// can construct brushes directly.
    /// <para>
    /// Rules:
    /// <list type="bullet">
    ///   <item><description>Linear gradients: the points are transformed into device space, applying
    ///   <see cref="Paint.Transform"/> in user space or in normalized [0..1] box space first, then
    ///   <paramref name="layerXform"/>, with <paramref name="layerBounds"/> denormalizing box space.
    ///   The returned paint has an identity <see cref="Paint.Transform"/>.</description></item>
    ///   <item><description>Radial and sweep gradients: the centers, radii and angles stay in the
    ///   paint's own space, flipped to y-down, and the returned <see cref="Paint.Transform"/> maps
    ///   that space to device space. A skew or a non-uniform scale in the paint's transform
    ///   therefore reaches the renderer instead of being reduced to a circle.</description></item>
    ///   <item><description>Color stops (ratios) remain normalized in [0..1] and are passed through unchanged.</description></item>
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
    /// A paint with <see cref="GradientUnits.UserSpaceOnUse"/> geometry, or <see langword="null"/>
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
                    CompositeMode = lg.CompositeMode,
                    Transform = Matrix3x2.Identity
                };
            }

            case RadialGradientPaint rg:
            {
                return new RadialGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center0 = FlipY(rg.Center0),
                    Radius0 = rg.Radius0,
                    Center1 = FlipY(rg.Center1),
                    Radius1 = rg.Radius1,
                    Spread = rg.Spread,
                    Stops = rg.Stops,
                    Opacity = rg.Opacity,
                    CompositeMode = rg.CompositeMode,
                    Transform = GetPaintTransform(rg.Transform, rg.Units, in layerBounds, layerXform)
                };
            }

            case SweepGradientPaint sg:
            {
                // The paint space is y-down like the renderer's, so the counter-clockwise angles
                // keep their direction and the transform carries any rotation or reflection.
                return new SweepGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center = FlipY(sg.Center),
                    StartAngle = sg.StartAngle,
                    EndAngle = sg.EndAngle,
                    Spread = sg.Spread,
                    Stops = sg.Stops,
                    Opacity = sg.Opacity,
                    CompositeMode = sg.CompositeMode,
                    Transform = GetPaintTransform(sg.Transform, sg.Units, in layerBounds, layerXform)
                };
            }

            default:
            {
                return paint;
            }
        }

        static Vector2 DenormalizePoint(Vector2 p, in FontRectangle bounds)
            => new(bounds.X + (p.X * bounds.Width), bounds.Y + (p.Y * bounds.Height));

        static Vector2 FlipY(Vector2 p) => new(p.X, -p.Y);
    }

    /// <summary>
    /// Builds the transform from a gradient paint's y-down space to device space.
    /// </summary>
    /// <param name="paintTransform">The paint's own transform in its y-up space.</param>
    /// <param name="units">The coordinate system of the paint geometry.</param>
    /// <param name="layerBounds">The layer bounds that normalized geometry maps through.</param>
    /// <param name="layerXform">The transform from the layer to device space.</param>
    /// <returns>The transform from the paint's y-down space to device space.</returns>
    private static Matrix3x2 GetPaintTransform(
        Matrix3x2 paintTransform,
        GradientUnits units,
        in FontRectangle layerBounds,
        Matrix3x2 layerXform)
    {
        // The flip is its own inverse: it takes the y-down paint geometry back to the y-up
        // space the paint's own transform is defined in.
        Matrix3x2 transform = Matrix3x2.CreateScale(1F, -1F) * paintTransform;
        if (units == GradientUnits.ObjectBoundingBox)
        {
            transform *= Matrix3x2.CreateScale(layerBounds.Width, layerBounds.Height) * Matrix3x2.CreateTranslation(layerBounds.X, layerBounds.Y);
        }

        return transform * layerXform;
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
