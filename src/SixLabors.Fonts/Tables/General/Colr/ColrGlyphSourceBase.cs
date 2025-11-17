// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// A base class for COLR glyph sources.
/// </summary>
internal abstract class ColrGlyphSourceBase : IPaintedGlyphSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColrGlyphSourceBase"/> class.
    /// </summary>
    /// <param name="colr">The COLR table.</param>
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <param name="glyphLoader">Delegate that loads a glyph outline for the given glyph id.</param>
    public ColrGlyphSourceBase(ColrTable colr, CpalTable? cpal, Func<ushort, GlyphVector?> glyphLoader)
    {
        this.Colr = colr;
        this.Cpal = cpal;
        this.GlyphLoader = glyphLoader;
    }

    /// <summary>
    /// Gets the COLR table.
    /// </summary>
    protected ColrTable Colr { get; }

    /// <summary>
    /// Gets the CPAL table, or null if not present.
    /// </summary>
    protected CpalTable? Cpal { get; }

    /// <summary>
    /// Gets the glyph loader delegate.
    /// </summary>
    protected Func<ushort, GlyphVector?> GlyphLoader { get; }

    /// <inheritdoc/>
    public abstract bool TryGetPaintedGlyph(ushort glyphId, out PaintedGlyph glyph, out PaintedCanvasMetadata canvas);

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
    protected static void FlattenPaint(
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

                    // Spec says: add 1.0 and multiply by 180° to retrieve counter-clockwise degrees.
                    StartAngle = (sw.StartAngle + 1F) * 180F,
                    EndAngle = (sw.EndAngle + 1F) * 180F,
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
                    StartAngle = (vsw.StartAngle + 1F) * 180F,
                    EndAngle = (vsw.EndAngle + 1F) * 180F,
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
    /// Converts a glyph vector into a sequence of path commands.
    /// </summary>
    /// <param name="gv">The glyph vector.</param>
    protected static List<PathCommand> BuildPath(GlyphVector gv)
    {
        IList<ControlPoint> points = gv.ControlPoints;
        IReadOnlyList<ushort> ends = gv.EndPoints;

        List<PathCommand> cmds = new(points.Count + ends.Count);

        int endOfContour = -1;

        for (int ci = 0; ci < ends.Count; ci++)
        {
            int startOfContour = endOfContour + 1;
            endOfContour = ends[ci];

            if (endOfContour < startOfContour)
            {
                continue;
            }

            int length = endOfContour - startOfContour + 1;
            if (length == 0)
            {
                continue;
            }

            // Choose initial MoveTo: last on-curve, else first on-curve, else midpoint(last, first).
            ControlPoint first = points[startOfContour];
            ControlPoint last = points[endOfContour];

            Vector2 moveTo = last.OnCurve ? last.Point
                            : first.OnCurve ? first.Point
                            : Mid(last.Point, first.Point);

            cmds.Add(PathCommand.MoveTo(moveTo));

            // Ring traversal over input points.
            Vector2 curr = last.Point;
            Vector2 next = first.Point;

            for (int p = 0; p < length; p++)
            {
                Vector2 prev = curr;
                curr = next;

                int currentIndex = startOfContour + p;
                int nextIndex = startOfContour + ((p + 1) % length);
                int prevIndex = startOfContour + ((length + p - 1) % length);

                next = points[nextIndex].Point;

                bool currOn = points[currentIndex].OnCurve;
                bool prevOn = points[prevIndex].OnCurve;
                bool nextOn = points[nextIndex].OnCurve;

                if (currOn)
                {
                    // Emit line to the current on-curve point unconditionally.
                    cmds.Add(PathCommand.LineTo(curr));
                    continue;
                }

                // Off-curve: insert implicit on-curve midpoints.
                Vector2 prev2 = prevOn ? prev : Mid(curr, prev);
                Vector2 next2 = nextOn ? next : Mid(curr, next);

                if (!prevOn)
                {
                    // Conditional line when previous input point was off-curve.
                    cmds.Add(PathCommand.LineTo(prev2));
                }

                // Metrics emits a LineTo(prev2) immediately before the quadratic as well.
                cmds.Add(PathCommand.LineTo(prev2));

                // Quadratic segment with control at current off-curve and endpoint at next2.
                cmds.Add(PathCommand.QuadraticTo(curr, next2));
            }

            cmds.Add(PathCommand.Close());
        }

        return cmds;
    }

    /// <summary>
    /// Maps COLR Extend to renderer SpreadMethod.
    /// </summary>
    /// <param name="extend">The COLR extend mode.</param>
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
    /// <param name="line">The color line.</param>
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <returns>The resolved gradient stops.</returns>
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
    /// <param name="line">The color line.</param>
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <returns>The resolved gradient stops.</returns>
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
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <param name="paletteEntryIndex">The palette entry index.</param>
    /// <param name="alphaMul">The alpha multiplier.</param>
    /// <returns>The resolved color.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GlyphColor ResolveColor(CpalTable? cpal, int paletteEntryIndex, float alphaMul)
    {
        // Palette index 0 selection. If you later expose palette selection, thread it here.
        GlyphColor baseColor = cpal is null ? new GlyphColor(0, 0, 0, 0) : cpal.GetGlyphColor(0, paletteEntryIndex);

        byte a = (byte)Math.Clamp((int)MathF.Round(baseColor.A * alphaMul), 0, 255);
        return new GlyphColor(baseColor.R, baseColor.G, baseColor.B, a);
    }

    /// <summary>
    /// Calculates the midpoint between two vectors.
    /// </summary>
    /// <param name="a">The first vector to use in the midpoint calculation.</param>
    /// <param name="b">The second vector to use in the midpoint calculation.</param>
    /// <returns>A <see cref="Vector2"/> representing the point exactly halfway between the two input vectors.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Mid(Vector2 a, Vector2 b)
        => (a + b) * .5F;
}
