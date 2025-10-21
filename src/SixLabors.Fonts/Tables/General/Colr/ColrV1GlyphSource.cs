// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Supplies painted glyphs for COLR v1 fonts.
/// Flattens paint graphs into a linear <see cref="PaintedLayer"/> stream and emits a <see cref="PaintedCanvas"/>.
/// </summary>
internal sealed class ColrV1GlyphSource : IPaintedGlyphSource
{
    private readonly ColrTable colr;
    private readonly CpalTable? cpal;
    private readonly Func<ushort, GlyphVector?> glyphLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColrV1GlyphSource"/> class.
    /// </summary>
    /// <param name="colr">The COLR table.</param>
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <param name="glyphLoader">Delegate that loads a glyph outline for the given glyph id.</param>
    public ColrV1GlyphSource(ColrTable colr, CpalTable? cpal, Func<ushort, GlyphVector?> glyphLoader)
    {
        this.colr = colr;
        this.cpal = cpal;
        this.glyphLoader = glyphLoader;
    }

    /// <inheritdoc/>
    public bool TryGetPaintedGlyph(ushort glyphId, out PaintedGlyph glyph, out PaintedCanvas canvas)
    {
        glyph = default;
        canvas = default;

        if (!this.colr.TryGetResolvedLayers(glyphId, out List<ResolvedGlyphLayer>? resolved))
        {
            return false;
        }

        List<PaintedLayer> layers = new(resolved.Count);

        for (int i = 0; i < resolved.Count; i++)
        {
            ResolvedGlyphLayer rl = resolved[i];

            GlyphVector? gv = this.glyphLoader(rl.GlyphId);
            if (gv is null || !gv.Value.HasValue())
            {
                continue;
            }

            // Build geometry once for this layer.
            List<PathCommand> path = BuildPath(gv.Value);

            // Flatten paint graph: accumulate wrapper transforms; attach composite mode to leaves.
            // Start with identity matrix and no override blend.
            List<Rendering.Paint> leafPaints = [];
            FlattenPaint(rl.Paint, rl.Transform, rl.CompositeMode, this.cpal, leafPaints);

            // Emit one layer per leaf paint.
            Bounds? clip = rl.ClipBox;
            for (int p = 0; p < leafPaints.Count; p++)
            {
                Rendering.Paint leaf = leafPaints[p];
                layers.Add(new PaintedLayer(leaf, FillRule.NonZero, leaf.Transform, clip, path));
            }
        }

        if (layers.Count == 0)
        {
            return false;
        }

        // Canvas viewBox in Y-up; renderer downstream decides orientation via flag.
        glyph = new PaintedGlyph(layers);
        canvas = new PaintedCanvas(FontRectangle.Empty, isYDown: false, rootTransform: Matrix3x2.Identity);
        return true;
    }

    /// <summary>
    /// Recursively flattens a COLR paint graph:
    /// - Wrapper nodes pre-multiply their matrix into <paramref name="transform"/> and recurse to the child.
    /// - Composite emits backdrop subtree first (inherits <paramref name="mode"/>),
    ///   then source subtree with <c>currentCompositeMode = node.CompositeMode</c>.
    /// - Leaf nodes emit concrete Rendering.Paint with <c>Transform = accum</c> and <c>CompositeMode = currentBlend ?? default</c>.
    /// Colors and stop offsets are passed through; no Y-flip applied here.
    /// </summary>
    /// <param name="node">The COLR paint node.</param>
    /// <param name="transform">The affine matrix in document space.</param>
    /// <param name="mode">The active composite mode to apply to leaf paints, or null for default.</param>
    /// <param name="cpal">Optional CPAL palette for color resolution.</param>
    /// <param name="outLeaves">Collector for emitted leaf paints.</param>
    private static void FlattenPaint(
        Paint node,
        Matrix3x2 transform,
        CompositeMode mode,
        CpalTable? cpal,
        List<Rendering.Paint> outLeaves)
    {
        // The input not will only be a paintable leaf here, as upstream resolution
        // should have eliminated glyph/colr-glyph nodes and flattened composites.
        switch (node)
        {
            case PaintSolid ps:
            {
                if (ps.PaletteIndex == 0xFFFF)
                {
                    // "Use foreground" => represent as a SolidPaint with fully transparent color;
                    // renderer can substitute foreground if needed.
                    outLeaves.Add(new SolidPaint
                    {
                        Color = new GlyphColor(0, 0, 0, 0),
                        Opacity = 1F,
                        Transform = transform,
                        CompositeMode = mode
                    });
                    return;
                }

                GlyphColor color = ResolveColor(cpal, ps.PaletteIndex, ps.Alpha);
                outLeaves.Add(new SolidPaint
                {
                    Color = color,
                    Opacity = 1F,
                    Transform = transform,
                    CompositeMode = mode
                });
                return;
            }

            case PaintLinearGradient pl:
            {
                GradientStop[] stops = ResolveStops(pl.ColorLine, cpal);
                outLeaves.Add(new LinearGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    P0 = new Vector2(pl.X0, pl.Y0),
                    P1 = new Vector2(pl.X1, pl.Y1),
                    P2 = new Vector2(pl.X2, pl.Y2),
                    Spread = MapSpread(pl.ColorLine.Extend),
                    Stops = stops,
                    Opacity = 1F,
                    Transform = transform,
                    CompositeMode = mode
                });
                return;
            }

            case PaintVarLinearGradient vpl:
            {
                GradientStop[] stops = ResolveStops(vpl.ColorLine, cpal);
                outLeaves.Add(new LinearGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    P0 = new Vector2(vpl.X0, vpl.Y0),
                    P1 = new Vector2(vpl.X1, vpl.Y1),
                    P2 = new Vector2(vpl.X2, vpl.Y2),
                    Spread = MapSpread(vpl.ColorLine.Extend),
                    Stops = stops,
                    Opacity = 1F,
                    Transform = transform,
                    CompositeMode = mode
                });
                return;
            }

            case PaintRadialGradient pr:
            {
                GradientStop[] stops = ResolveStops(pr.ColorLine, cpal);
                outLeaves.Add(new RadialGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center0 = new Vector2(pr.X0, pr.Y0),
                    Radius0 = pr.Radius0,
                    Center1 = new Vector2(pr.X1, pr.Y1),
                    Radius1 = pr.Radius1,
                    Spread = MapSpread(pr.ColorLine.Extend),
                    Stops = stops,
                    Opacity = 1F,
                    Transform = transform,
                    CompositeMode = mode
                });
                return;
            }

            case PaintVarRadialGradient vpr:
            {
                GradientStop[] stops = ResolveStops(vpr.ColorLine, cpal);
                outLeaves.Add(new RadialGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center0 = new Vector2(vpr.X0, vpr.Y0),
                    Radius0 = vpr.Radius0,
                    Center1 = new Vector2(vpr.X1, vpr.Y1),
                    Radius1 = vpr.Radius1,
                    Spread = MapSpread(vpr.ColorLine.Extend),
                    Stops = stops,
                    Opacity = 1F,
                    Transform = transform,
                    CompositeMode = mode
                });
                return;
            }

            case PaintSweepGradient sw:
            {
                GradientStop[] stops = ResolveStops(sw.ColorLine, cpal);
                outLeaves.Add(new SweepGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center = new Vector2(sw.CenterX, sw.CenterY),
                    StartAngle = sw.StartAngle,
                    EndAngle = sw.EndAngle,
                    Spread = MapSpread(sw.ColorLine.Extend),
                    Stops = stops,
                    Opacity = 1F,
                    Transform = transform,
                    CompositeMode = mode
                });
                return;
            }

            case PaintVarSweepGradient vsw:
            {
                GradientStop[] stops = ResolveStops(vsw.ColorLine, cpal);
                outLeaves.Add(new SweepGradientPaint
                {
                    Units = GradientUnits.UserSpaceOnUse,
                    Center = new Vector2(vsw.CenterX, vsw.CenterY),
                    StartAngle = vsw.StartAngle,
                    EndAngle = vsw.EndAngle,
                    Spread = MapSpread(vsw.ColorLine.Extend),
                    Stops = stops,
                    Opacity = 1F,
                    Transform = transform,
                    CompositeMode = mode
                });
                return;
            }

            default:
                return;
        }
    }

    /// <summary>
    /// Maps COLR Extend to renderer SpreadMethod.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SpreadMethod MapSpread(Extend extend)
        => extend switch
        {
            Extend.Pad => SpreadMethod.Pad,
            Extend.Repeat => SpreadMethod.Repeat,
            Extend.Reflect => SpreadMethod.Reflect,
            _ => SpreadMethod.Pad
        };

    /// <summary>
    /// Resolves a color line into concrete gradient stops. Offsets are clamped to [0,1].
    /// 0xFFFF palette indices are treated as transparent here (foreground color handled by text color elsewhere).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GradientStop[] ResolveStops(ColorLine line, CpalTable? cpal)
    {
        ColorStop[] src = line.Stops;
        GradientStop[] stops = new GradientStop[src.Length];

        for (int i = 0; i < src.Length; i++)
        {
            ref readonly ColorStop s = ref src[i];

            GlyphColor c = s.PaletteIndex == 0xFFFF
                ? new GlyphColor(0, 0, 0, 0) // transparent placeholder; renderer can blend with foreground
                : ResolveColor(cpal, s.PaletteIndex, s.Alpha);

            float offset = Math.Clamp(s.StopOffset, 0F, 1F);

            stops[i] = new GradientStop(offset, c);
        }

        return stops;
    }

    /// <summary>
    /// Resolves a color line into concrete gradient stops. Offsets are clamped to [0,1].
    /// 0xFFFF palette indices are treated as transparent here (foreground color handled by text color elsewhere).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GradientStop[] ResolveStops(VarColorLine line, CpalTable? cpal)
    {
        VarColorStop[] src = line.Stops;
        GradientStop[] stops = new GradientStop[src.Length];

        for (int i = 0; i < src.Length; i++)
        {
            ref readonly VarColorStop s = ref src[i];

            GlyphColor c = s.PaletteIndex == 0xFFFF
                ? new GlyphColor(0, 0, 0, 0) // transparent placeholder; renderer can blend with foreground
                : ResolveColor(cpal, s.PaletteIndex, s.Alpha);

            float offset = Math.Clamp(s.StopOffset, 0F, 1F);

            stops[i] = new GradientStop(offset, c);
        }

        return stops;
    }

    /// <summary>
    /// Resolves a CPAL palette entry with an alpha multiplier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GlyphColor ResolveColor(CpalTable? cpal, int paletteEntryIndex, float alphaMul)
    {
        // Palette index 0 selection. If you later expose palette selection, thread it here.
        GlyphColor baseColor = cpal is null ? new GlyphColor(0, 0, 0, 0) : cpal.GetGlyphColor(0, paletteEntryIndex);

        byte a = (byte)Math.Clamp((int)MathF.Round(baseColor.Alpha * alphaMul), 0, 255);
        return new GlyphColor(baseColor.Red, baseColor.Green, baseColor.Blue, a);
    }

    /// <summary>
    /// Converts a glyph vector into a sequence of path commands.
    /// </summary>
    /// <param name="gv">The glyph vector.</param>
    private static List<PathCommand> BuildPath(GlyphVector gv)
    {
        IList<ControlPoint> points = gv.ControlPoints;
        IReadOnlyList<ushort> ends = gv.EndPoints;

        // Upper bound: roughly points + contours for Move/Close.
        List<PathCommand> cmds = new(points.Count + ends.Count);

        int endOfContour = -1;

        // Process each closed contour.
        for (int ci = 0; ci < ends.Count; ci++)
        {
            int startOfContour = endOfContour + 1;
            endOfContour = ends[ci];

            // Skip empty or malformed ranges.
            if (endOfContour < startOfContour)
            {
                continue;
            }

            int length = endOfContour - startOfContour + 1;
            if (length == 0)
            {
                continue;
            }

            // Determine initial move point:
            // - If last point is on-curve => move to last.
            // - Else if first is on-curve => move to first.
            // - Else move to midpoint(last, first).
            ControlPoint first = points[startOfContour];
            ControlPoint last = points[endOfContour];

            Vector2 moveTo;
            if (last.OnCurve)
            {
                moveTo = last.Point;
            }
            else if (first.OnCurve)
            {
                moveTo = first.Point;
            }
            else
            {
                moveTo = Mid(last.Point, first.Point);
            }

            cmds.Add(PathCommand.MoveTo(moveTo));

            // Initialize ring traversal variables to mirror GlyphMetrics:
            // prev = curr; curr = next; next = nextIndex point
            Vector2 curr = last.Point;                     // "curr" starts at last point
            Vector2 next = first.Point;                    // "next" starts at first point

            // Walk each point index in the closed ring.
            for (int p = 0; p < length; p++)
            {
                Vector2 prev = curr;                       // save previous emitted position
                curr = next;                               // advance to current
                int currentIndex = startOfContour + p;
                int nextIndex = startOfContour + ((p + 1) % length);
                int prevIndex = startOfContour + ((length + p - 1) % length);

                // Peek next input point for upcoming step.
                next = points[nextIndex].Point;

                bool currOn = points[currentIndex].OnCurve;
                bool prevOn = points[prevIndex].OnCurve;
                bool nextOn = points[nextIndex].OnCurve;

                if (currOn)
                {
                    // Metrics emits a straight line to the current on-curve point.
                    // Emit only if it advances (avoid zero-length segments).
                    if (!curr.Equals(prev))
                    {
                        cmds.Add(PathCommand.LineTo(curr));
                    }

                    continue;
                }

                // Off-curve logic matches GlyphMetrics:

                // prev2:
                // If the previous input point was off-curve, insert a midpoint between the
                // current emitted position and the previous input point, and line to it.
                // Otherwise prev2 equals the current emitted position.
                Vector2 prev2 = prevOn ? prev : Mid(curr, prev);

                if (!prevOn)
                {
                    // Conditional line (only when previous input point was off-curve).
                    cmds.Add(PathCommand.LineTo(prev2));
                }

                // next2:
                // If the next input point is off-curve, the quadratic endpoint is the
                // midpoint of current off-curve and next off-curve; else it is the next on-curve.
                Vector2 next2 = nextOn ? next : Mid(curr, next);

                // Metrics emits an unconditional LineTo(prev2) immediately before the quadratic,
                // even if the previous branch already lined to prev2. This can duplicate the
                // same vertex; downstream rasterizers coalesce it. We mirror that behavior.
                cmds.Add(PathCommand.LineTo(prev2));

                // Emit the quadratic segment: control = current off-curve ("curr" input point),
                // endpoint = next2 as defined above.
                cmds.Add(PathCommand.QuadraticTo(curr, next2));

                // Advance the emitted position to the quadratic endpoint.
                // The ring traversal will update "curr" at the next loop head via "prev = curr; curr = next".
                // Here we must synchronize "curr" with what we just emitted.
                curr = next2;
            }

            // Close the contour to match metrics' EndFigure semantics.
            cmds.Add(PathCommand.Close());
        }

        return cmds;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Mid(in Vector2 a, in Vector2 b)
        => (a + b) * .5F;
}
