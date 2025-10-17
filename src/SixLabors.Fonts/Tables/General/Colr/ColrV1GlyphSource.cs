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
        Bounds union = Bounds.Empty;
        bool haveBounds = false;

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

            // Bounds for canvas viewBox union (Y-up; do not flip here).
            Bounds b = rl.ClipBox ?? gv.Value.Bounds;
            union = haveBounds ? Bounds.Union(union, b) : b;
            haveBounds = true;

            // Flatten paint graph: accumulate wrapper transforms; attach composite mode to leaves.
            // Start with identity matrix and no override blend.
            List<Rendering.Paint> leafPaints = [];
            FlattenPaint(rl.Paint, Matrix3x2.Identity, null, this.cpal, leafPaints);

            // Emit one layer per leaf paint. Layer transform is identity (geometry is Y-up; orientation reported by canvas).
            for (int p = 0; p < leafPaints.Count; p++)
            {
                layers.Add(new PaintedLayer(leafPaints[p], FillRule.NonZero, Matrix3x2.Identity, path));
            }
        }

        if (layers.Count == 0 || !haveBounds)
        {
            return false;
        }

        // Canvas viewBox in Y-up; renderer downstream decides orientation via flag.
        float minX = union.Min.X;
        float maxX = union.Max.X;
        float minY = union.Min.Y;
        float maxY = union.Max.Y;

        FontRectangle viewBox = new(minX, minY, maxX - minX, maxY - minY);
        glyph = new PaintedGlyph(layers.ToArray());
        canvas = new PaintedCanvas(viewBox, isYDown: false, rootTransform: Matrix3x2.Identity);
        return true;
    }

    /// <summary>
    /// Recursively flattens a COLR paint graph:
    /// - Wrapper nodes pre-multiply their matrix into <paramref name="accum"/> and recurse to the child.
    /// - Composite emits backdrop subtree first (inherits <paramref name="currentBlend"/>),
    ///   then source subtree with <c>currentCompositeMode = node.CompositeMode</c>.
    /// - Leaf nodes emit concrete Rendering.Paint with <c>Transform = accum</c> and <c>CompositeMode = currentBlend ?? default</c>.
    /// Colors and stop offsets are passed through; no Y-flip applied here.
    /// </summary>
    /// <param name="node">The COLR paint node.</param>
    /// <param name="accum">The accumulated affine matrix in document space.</param>
    /// <param name="currentBlend">The active composite mode to apply to leaf paints, or null for default.</param>
    /// <param name="cpal">Optional CPAL palette for color resolution.</param>
    /// <param name="outLeaves">Collector for emitted leaf paints.</param>
    private static void FlattenPaint(
        Paint node,
        Matrix3x2 accum,
        ColrCompositeMode? currentBlend,
        CpalTable? cpal,
        List<Rendering.Paint> outLeaves)
    {
        switch (node)
        {
            // ----------------
            // Wrapper: matrix
            // ----------------
            case PaintTransform pt:
            {
                Matrix3x2 m = ToMatrix(pt.Transform, null);
                FlattenPaint(pt.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            case PaintVarTransform pvt:
            {
                Matrix3x2 m = ToMatrix(null, pvt.Transform);
                FlattenPaint(pvt.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            // ----------------
            // Wrapper: translate
            // ----------------
            case PaintTranslate t:
            {
                Matrix3x2 m = Matrix3x2.CreateTranslation(t.Dx, t.Dy);
                FlattenPaint(t.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            case PaintVarTranslate vt:
            {
                Matrix3x2 m = Matrix3x2.CreateTranslation(vt.Dx, vt.Dy);
                FlattenPaint(vt.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            // ----------------
            // Wrapper: scale families
            // ----------------
            case PaintScale s:
            {
                Matrix3x2 m = BuildScale(s.ScaleX, s.Uniform ? s.ScaleX : s.ScaleY, s.AroundCenter, s.CenterX, s.CenterY);
                FlattenPaint(s.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            case PaintVarScale vs:
            {
                Matrix3x2 m = BuildScale(vs.ScaleX, vs.Uniform ? vs.ScaleX : vs.ScaleY, vs.AroundCenter, vs.CenterX, vs.CenterY);
                FlattenPaint(vs.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            // ----------------
            // Wrapper: rotate families
            // ----------------
            case PaintRotate r:
            {
                Matrix3x2 m = BuildRotate(r.Angle, r.AroundCenter, r.CenterX, r.CenterY);
                FlattenPaint(r.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            case PaintVarRotate vr:
            {
                Matrix3x2 m = BuildRotate(vr.Angle, vr.AroundCenter, vr.CenterX, vr.CenterY);
                FlattenPaint(vr.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            // ----------------
            // Wrapper: skew families
            // ----------------
            case PaintSkew k:
            {
                Matrix3x2 m = BuildSkew(k.XSkew, k.YSkew, k.AroundCenter, k.CenterX, k.CenterY);
                FlattenPaint(k.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            case PaintVarSkew vk:
            {
                Matrix3x2 m = BuildSkew(vk.XSkew, vk.YSkew, vk.AroundCenter, vk.CenterX, vk.CenterY);
                FlattenPaint(vk.Child, m * accum, currentBlend, cpal, outLeaves);
                return;
            }

            // ----------------
            // Composite: set blend for Source branch
            // ----------------
            case PaintComposite comp:
            {
                // Backdrop first, inherits currentBlend
                FlattenPaint(comp.Backdrop, accum, currentBlend, cpal, outLeaves);

                // Source next, overrides blend for its emitted leaves
                FlattenPaint(comp.Source, accum, comp.CompositeMode, cpal, outLeaves);
                return;
            }

            // ----------------
            // Leaves
            // ----------------
            case PaintSolid ps:
            {
                if (ps.PaletteIndex == 0xFFFF)
                {
                    // "Use foreground" → represent as a SolidPaint with fully transparent color;
                    // renderer can substitute foreground if needed.
                    outLeaves.Add(new SolidPaint
                    {
                        Color = new GlyphColor(0, 0, 0, 0),
                        Opacity = 1F,
                        Transform = accum,
                        CompositeMode = MapCompositeMode(currentBlend)
                    });
                    return;
                }

                GlyphColor color = ResolveColor(cpal, ps.PaletteIndex, ps.Alpha);
                outLeaves.Add(new SolidPaint
                {
                    Color = color,
                    Opacity = 1F,
                    Transform = accum,
                    CompositeMode = MapCompositeMode(currentBlend)
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
                    Transform = accum,
                    CompositeMode = MapCompositeMode(currentBlend)
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
                    Transform = accum,
                    CompositeMode = MapCompositeMode(currentBlend)
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
                    Transform = accum,
                    CompositeMode = MapCompositeMode(currentBlend)
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
                    Transform = accum,
                    CompositeMode = MapCompositeMode(currentBlend)
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
                    Transform = accum,
                    CompositeMode = MapCompositeMode(currentBlend)
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
                    Transform = accum,
                    CompositeMode = MapCompositeMode(currentBlend)
                });
                return;
            }

            // If resolver still passes glyph/colr-glyph nodes here, either handle them by
            // switching glyph outlines before building layers, or rely on upstream resolution.
            default:
                return;
        }
    }

    /// <summary>
    /// Maps an optional fixed or variable 2×3 affine to <see cref="Matrix3x2"/>.
    /// Layout:
    ///   [ xx  xy  dx ]
    ///   [ yx  yy  dy ]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 ToMatrix(Affine2x3? affine, VarAffine2x3? varAffine)
    {
        if (affine.HasValue)
        {
            Affine2x3 a = affine.Value;
            return new Matrix3x2(a.Xx, a.Yx, a.Xy, a.Yy, a.Dx, a.Dy); // (M11, M12, M21, M22, M31, M32)
        }

        if (varAffine.HasValue)
        {
            VarAffine2x3 v = varAffine.Value;
            return new Matrix3x2(v.Xx, v.Yx, v.Xy, v.Yy, v.Dx, v.Dy);
        }

        return Matrix3x2.Identity;
    }

    /// <summary>Builds a scale matrix, optionally around a center.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildScale(float sx, float sy, bool aroundCenter, float cx, float cy)
    {
        if (!aroundCenter)
        {
            return Matrix3x2.CreateScale(sx, sy);
        }

        // T(c) * S * T(-c)
        Matrix3x2 t0 = Matrix3x2.CreateTranslation(-cx, -cy);
        Matrix3x2 s = Matrix3x2.CreateScale(sx, sy);
        Matrix3x2 t1 = Matrix3x2.CreateTranslation(cx, cy);
        return t0 * s * t1;
    }

    /// <summary>Builds a rotation matrix in radians-degrees as provided, optionally around a center.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildRotate(float angleRadiansOrDegrees, bool aroundCenter, float cx, float cy)
    {
        // Input is already bias-adjusted per your loader; assume radians if that’s how you parsed it.
        Matrix3x2 r = Matrix3x2.CreateRotation(angleRadiansOrDegrees);
        if (!aroundCenter)
        {
            return r;
        }

        Matrix3x2 t0 = Matrix3x2.CreateTranslation(-cx, -cy);
        Matrix3x2 t1 = Matrix3x2.CreateTranslation(cx, cy);
        return t0 * r * t1;
    }

    /// <summary>Builds a skew matrix, optionally around a center. Angles are in radians.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildSkew(float xSkew, float ySkew, bool aroundCenter, float cx, float cy)
    {
        float tx = MathF.Tan(xSkew);
        float ty = MathF.Tan(ySkew);

        // Skew X then Y: Ky * Kx
        Matrix3x2 kx = new(1, 0, tx, 1, 0, 0); // x' = x + y*tx
        Matrix3x2 ky = new(1, ty, 0, 1, 0, 0); // y' = y + x*ty
        Matrix3x2 k = kx * ky;

        if (!aroundCenter)
        {
            return k;
        }

        Matrix3x2 t0 = Matrix3x2.CreateTranslation(-cx, -cy);
        Matrix3x2 t1 = Matrix3x2.CreateTranslation(cx, cy);
        return t0 * k * t1;
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
    /// Maps a COLR composite mode to the internal <see cref="CompositeMode"/>.
    /// <para>
    /// Returns <see cref="CompositeMode.SrcOver"/> when <paramref name="mode"/> is null
    /// or when the value is not recognized.
    /// </para>
    /// </summary>
    /// <param name="mode">The optional COLR composite mode.</param>
    /// <returns>The mapped <see cref="CompositeMode"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CompositeMode MapCompositeMode(ColrCompositeMode? mode)
        => mode switch
        {
            // Porter–Duff
            ColrCompositeMode.Clear => CompositeMode.Clear,
            ColrCompositeMode.Src => CompositeMode.Src,
            ColrCompositeMode.Dst => CompositeMode.Dst,
            ColrCompositeMode.SrcOver => CompositeMode.SrcOver,
            ColrCompositeMode.DstOver => CompositeMode.DstOver,
            ColrCompositeMode.SrcIn => CompositeMode.SrcIn,
            ColrCompositeMode.DstIn => CompositeMode.DstIn,
            ColrCompositeMode.SrcOut => CompositeMode.SrcOut,
            ColrCompositeMode.DstOut => CompositeMode.DstOut,
            ColrCompositeMode.SrcAtop => CompositeMode.SrcAtop,
            ColrCompositeMode.DstAtop => CompositeMode.DstAtop,
            ColrCompositeMode.Xor => CompositeMode.Xor,
            ColrCompositeMode.Plus => CompositeMode.Plus,

            // Blend modes
            ColrCompositeMode.Screen => CompositeMode.Screen,
            ColrCompositeMode.Overlay => CompositeMode.Overlay,
            ColrCompositeMode.Darken => CompositeMode.Darken,
            ColrCompositeMode.Lighten => CompositeMode.Lighten,
            ColrCompositeMode.ColorDodge => CompositeMode.ColorDodge,
            ColrCompositeMode.ColorBurn => CompositeMode.ColorBurn,
            ColrCompositeMode.HardLight => CompositeMode.HardLight,
            ColrCompositeMode.SoftLight => CompositeMode.SoftLight,
            ColrCompositeMode.Difference => CompositeMode.Difference,
            ColrCompositeMode.Exclusion => CompositeMode.Exclusion,
            ColrCompositeMode.Multiply => CompositeMode.Multiply,
            ColrCompositeMode.Hue => CompositeMode.Hue,
            ColrCompositeMode.Saturation => CompositeMode.Saturation,
            ColrCompositeMode.Color => CompositeMode.Color,
            ColrCompositeMode.Luminosity => CompositeMode.Luminosity,
            _ => CompositeMode.SrcOver,
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

    // --------------------------
    // Outline conversion (glyf)
    // --------------------------

    /// <summary>
    /// Converts a glyph vector (Y-up) into a sequence of path commands.
    /// </summary>
    private static List<PathCommand> BuildPath(GlyphVector gv)
    {
        IList<ControlPoint> points = gv.ControlPoints;
        IReadOnlyList<ushort> ends = gv.EndPoints;
        List<PathCommand> cmds = new(points.Count + ends.Count);

        int start = 0;
        for (int ci = 0; ci < ends.Count; ci++)
        {
            int end = ends[ci];
            if (end < start)
            {
                start = end + 1;
                continue;
            }

            int count = end - start + 1;
            if (count == 0)
            {
                start = end + 1;
                continue;
            }

            ControlPoint p0 = points[start];
            ControlPoint pPrev = points[end];
            Vector2 curr;

            // Choose the initial MoveTo:
            // - If first point on-curve → move to it.
            // - Else if last point on-curve → move to last.
            // - Else → move to midpoint between first two off-curve points.
            if (p0.OnCurve)
            {
                curr = p0.Point;
                cmds.Add(PathCommand.MoveTo(curr));
            }
            else if (pPrev.OnCurve)
            {
                curr = pPrev.Point;
                cmds.Add(PathCommand.MoveTo(curr));
            }
            else
            {
                curr = Mid(pPrev.Point, p0.Point);
                cmds.Add(PathCommand.MoveTo(curr));
            }

            int i = start;
            while (i <= end)
            {
                ControlPoint p = points[i];
                if (p.OnCurve)
                {
                    EmitLineOrQuadratic(cmds, ref curr, p.Point, null);
                    i++;
                }
                else
                {
                    int ni = i + 1 <= end ? i + 1 : start;
                    ControlPoint pn = points[ni];

                    if (pn.OnCurve)
                    {
                        // Quadratic segment curr -q-> pn.Point
                        EmitLineOrQuadratic(cmds, ref curr, pn.Point, p.Point);
                        i += 2;
                    }
                    else
                    {
                        // Two consecutive off-curve points → implicit on-curve at midpoint.
                        Vector2 mid = Mid(p.Point, pn.Point);
                        EmitLineOrQuadratic(cmds, ref curr, mid, p.Point);
                        i++;
                    }
                }
            }

            cmds.Add(PathCommand.Close());
            start = end + 1;
        }

        return cmds;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Mid(in Vector2 a, in Vector2 b)
        => (a + b) * .5F;

    /// <summary>
    /// Emits either a line or a cubic Bézier that approximates a quadratic,
    /// depending on whether the control point is present and the three points are nearly collinear.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitLineOrQuadratic(List<PathCommand> cmds, ref Vector2 curr, Vector2 end, Vector2? q)
    {
        if (!q.HasValue || NearlyCollinear(curr, q.Value, end))
        {
            if (!curr.Equals(end))
            {
                cmds.Add(PathCommand.LineTo(end));
                curr = end;
            }

            return;
        }

        // Quadratic-to-cubic conversion:
        // C1 = P0 + 2/3 (Q - P0)
        // C2 = P2 + 2/3 (Q - P2)
        Vector2 c1 = curr + ((2F / 3F) * (q.Value - curr));
        Vector2 c2 = end + ((2F / 3F) * (q.Value - end));

        cmds.Add(PathCommand.CubicTo(c1, c2, end));
        curr = end;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool NearlyCollinear(in Vector2 a, in Vector2 b, in Vector2 c)
    {
        // Twice the triangle area test.
        float area2 = MathF.Abs(((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X)));
        return area2 <= 0.5F;
    }
}
