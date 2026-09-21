// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.TrueType;

/// <summary>
/// Represents a glyph metric from a particular TrueType font face.
/// </summary>
public partial class TrueTypeGlyphMetrics : FontGlyphMetrics
{
    private readonly GlyphVector vector;

    /// <summary>
    /// Scaled, hinted, upright outline copies keyed by ppem and resolved hinting mode.
    /// Offset translation, synthetic oblique, and layout rotation are applied per point at
    /// emit time so one cached copy serves every run, layout mode, and positioned offset.
    /// Allocated on first render: shaping and measurement clone metrics without ever
    /// rendering them, so an eager cache would cost a dictionary per glyph per shaping pass.
    /// </summary>
    private ConcurrentDictionary<ScaledVectorKey, GlyphVector>? scaledVectorCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrueTypeGlyphMetrics"/> class.
    /// </summary>
    /// <param name="font">The font metrics this glyph belongs to.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point for this glyph.</param>
    /// <param name="vector">The glyph outline vector.</param>
    /// <param name="advanceWidth">The advance width in font units.</param>
    /// <param name="advanceHeight">The advance height in font units.</param>
    /// <param name="leftSideBearing">The left side bearing in font units.</param>
    /// <param name="topSideBearing">The top side bearing in font units.</param>
    /// <param name="unitsPerEM">The units per em for the font.</param>
    /// <param name="textAttributes">The text attributes.</param>
    /// <param name="textDecorations">The text decorations.</param>
    /// <param name="glyphType">The glyph type.</param>
    internal TrueTypeGlyphMetrics(
        StreamFontMetrics font,
        ushort glyphId,
        CodePoint codePoint,
        GlyphVector vector,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        GlyphType glyphType)
        : base(
              font,
              glyphId,
              codePoint,
              vector.Bounds,
              advanceWidth,
              advanceHeight,
              leftSideBearing,
              topSideBearing,
              unitsPerEM,
              textAttributes,
              textDecorations,
              glyphType)
        => this.vector = vector;

    /// <summary>
    /// Gets the outline for the current glyph.
    /// </summary>
    /// <returns>The <see cref="GlyphVector"/>.</returns>
    internal GlyphVector GetOutline() => this.vector;

    /// <summary>
    /// Gets the scaled and hinted outline for the given size and mode, building and caching it
    /// on first use. Exposed for diagnostics and tests.
    /// </summary>
    /// <param name="scaledPPEM">The scaled size to build the outline for.</param>
    /// <param name="hintingMode">The requested hinting mode.</param>
    /// <returns>The scaled <see cref="GlyphVector"/>.</returns>
    internal GlyphVector GetScaledOutline(float scaledPPEM, HintingMode hintingMode)
    {
        ConcurrentDictionary<ScaledVectorKey, GlyphVector> cache =
            LazyInitializer.EnsureInitialized(ref this.scaledVectorCache, static () => new ConcurrentDictionary<ScaledVectorKey, GlyphVector>());
        return cache.GetOrAdd(new ScaledVectorKey(scaledPPEM, this.GetHintingMode(hintingMode)), static (key, self) => self.CreateScaledVector(key), this);
    }

    /// <inheritdoc/>
    internal override HintingMode ResolveHintingMode(HintingMode hintingMode) => this.GetHintingMode(hintingMode);

    /// <inheritdoc/>
    internal override bool TryGetFittedOutlinePlacement(
        float scaledPPEM,
        HintingMode resolvedHintingMode,
        bool includeInkExtent,
        out FittedOutlinePlacement placement)
    {
        GlyphVector scaledVector = this.GetScaledOutline(scaledPPEM, resolvedHintingMode);
        if (!scaledVector.IsHinted)
        {
            placement = default;
            return false;
        }

        Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
        float fittedMin = 0F;
        float fittedMax = 0F;
        bool hasFittedInk = false;
        if (includeInkExtent)
        {
            IList<ControlPoint> controlPoints = scaledVector.ControlPoints;
            hasFittedInk = controlPoints.Count > 0
                && GetFittedInkExtentX(controlPoints, scaledVector.EndPoints, out fittedMin, out fittedMax);
        }

        placement = new FittedOutlinePlacement(scale, hasFittedInk, fittedMin, fittedMax);
        return true;
    }

    /// <summary>
    /// Returns the size to render/measure the glyph at. Under full hinting the em square is
    /// constrained to whole pixels, matching the classic rasterizers whose instruction
    /// exceptions and control values assume integral pixel sizes.
    /// </summary>
    /// <param name="pointSize">The font size in pt units.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
    /// <param name="hintingMode">The hinting mode, which may constrain the size to whole pixels.</param>
    /// <returns>The <see cref="float"/>.</returns>
    internal override float GetScaledSize(float pointSize, float dpi, HintingMode hintingMode)
    {
        float scaledPPEM = base.GetScaledSize(pointSize, dpi, hintingMode);
        if (this.GetHintingMode(hintingMode) == HintingMode.Full)
        {
            scaledPPEM = MathF.Max(1F, MathF.Floor((scaledPPEM / 72F) + 0.5F)) * 72F;
        }

        return scaledPPEM;
    }

    /// <summary>
    /// Attempts to compute the whole pixel advance width for the glyph under full hinting.
    /// The value comes from the 'hdmx' device record when the font carries one, exactly as
    /// classic rasterizers resolve device advances, or from the design advance rounded to
    /// whole pixels otherwise: fonts without device records do not adjust advances in their
    /// instructions. Neither path executes hinting or touches the outline cache, so this is
    /// safe on the layout hot path.
    /// </summary>
    /// <param name="pointSize">The font size in pt units.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
    /// <param name="hintingMode">The requested hinting mode.</param>
    /// <param name="advancePx">The advance width in whole device pixels.</param>
    /// <returns><see langword="true"/> if a hinted advance applies; otherwise, <see langword="false"/>.</returns>
    public override bool TryGetHintedAdvanceWidth(float pointSize, float dpi, HintingMode hintingMode, out float advancePx)
    {
        advancePx = 0F;
        if (this.GetHintingMode(hintingMode) != HintingMode.Full)
        {
            return false;
        }

        // Sub and superscript metrics shrink the glyph by adjusting the scale factor while
        // the em square stays at the base size, so layout maps font units to pixels through
        // a different ratio than the base em. Device advances resolved against the base em
        // would be rescaled by that ratio on the way back into layout units, advancing the
        // pen by the wrong amount, so those runs keep their shaped fractional advances.
        if (this.ScaleFactor.X != this.UnitsPerEm * 72F)
        {
            return false;
        }

        float scaledPPEM = this.GetScaledSize(pointSize, dpi, hintingMode);
        int ppem = (int)(scaledPPEM / 72F);
        if (this.FontMetrics.TryGetDeviceAdvanceWidth(this.GlyphId, ppem, out byte deviceAdvance))
        {
            advancePx = deviceAdvance;
            return true;
        }

        // Without a device table entry the true advance is the hinted phantom advance, read
        // from the same scaled vector the renderer caches, so hinting runs once per size and
        // layout and raster agree exactly. The rounded design advance only covers glyphs the
        // interpreter could not hint.
        ConcurrentDictionary<ScaledVectorKey, GlyphVector> cache = LazyInitializer.EnsureInitialized(ref this.scaledVectorCache, static () => new ConcurrentDictionary<ScaledVectorKey, GlyphVector>());
        GlyphVector scaledVector = cache.GetOrAdd(new ScaledVectorKey(scaledPPEM, HintingMode.Full), static (key, self) => self.CreateScaledVector(key), this);
        if (scaledVector.IsHinted)
        {
            advancePx = scaledVector.HintedAdvance.X;
            return true;
        }

        advancePx = MathF.Floor((this.AdvanceWidth * scaledPPEM / (this.UnitsPerEm * 72F)) + 0.5F);
        return true;
    }

    /// <summary>
    /// Computes the implied start point used when both stored ends of a TrueType contour are
    /// off curve. GDI performs a signed arithmetic shift on the summed 26.6 coordinates.
    /// </summary>
    /// <param name="first">The contour's first stored point in device pixels.</param>
    /// <param name="last">The contour's last stored point in device pixels.</param>
    /// <returns>The implied contour start on the signed 26.6 grid.</returns>
    private static Vector2 GetImpliedContourStart(Vector2 first, Vector2 last)
    {
        int firstX = (int)(first.X * 64F);
        int firstY = (int)(first.Y * 64F);
        int lastX = (int)(last.X * 64F);
        int lastY = (int)(last.Y * 64F);

        // cjFillPolygon uses 32-bit ADD followed by SAR 1. This floors an odd sum for
        // either sign and is intentionally different from averaging to a half-grid value.
        int x = unchecked(firstX + lastX) >> 1;
        int y = unchecked(firstY + lastY) >> 1;
        return new Vector2(x / 64F, y / 64F);
    }

    /// <summary>
    /// Computes the fitted outline's true horizontal ink extent. Off-curve control points
    /// bound the curve's hull but overshoot the curve itself, so quadratic extrema are
    /// evaluated exactly wherever a control point lies beyond its segment's endpoints.
    /// </summary>
    /// <param name="controlPoints">The outline's control points in scaled device units.</param>
    /// <param name="endPoints">The outline's contour end point indices.</param>
    /// <param name="min">The minimum ink X coordinate.</param>
    /// <param name="max">The maximum ink X coordinate.</param>
    /// <returns><see langword="true"/> when the outline produced any extent.</returns>
    private static bool GetFittedInkExtentX(IList<ControlPoint> controlPoints, IReadOnlyList<ushort> endPoints, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;
        int endOfContour = -1;
        for (int contour = 0; contour < endPoints.Count; contour++)
        {
            int start = endOfContour + 1;
            endOfContour = endPoints[contour];
            int count = endOfContour - start + 1;
            if (count < 2)
            {
                continue;
            }

            for (int i = start; i <= endOfContour; i++)
            {
                ControlPoint current = controlPoints[i];
                if (current.OnCurve)
                {
                    min = Math.Min(min, current.Point.X);
                    max = Math.Max(max, current.Point.X);
                    continue;
                }

                // Resolve the segment's effective on-curve neighbours, synthesizing implied
                // midpoints for consecutive off-curve points exactly as emission does. Only
                // the contour's wraparound midpoint is forced onto the 26.6 grid by GDI.
                ControlPoint previous = controlPoints[i == start ? endOfContour : i - 1];
                ControlPoint next = controlPoints[i == endOfContour ? start : i + 1];
                float a = previous.OnCurve
                    ? previous.Point.X
                    : i == start
                        ? GetImpliedContourStart(previous.Point, current.Point).X
                        : (previous.Point.X + current.Point.X) * 0.5F;

                float b = next.OnCurve
                    ? next.Point.X
                    : i == endOfContour
                        ? GetImpliedContourStart(current.Point, next.Point).X
                        : (next.Point.X + current.Point.X) * 0.5F;

                min = Math.Min(min, Math.Min(a, b));
                max = Math.Max(max, Math.Max(a, b));

                float c = current.Point.X;
                if (c < Math.Min(a, b) || c > Math.Max(a, b))
                {
                    // The curve's horizontal extreme lies inside the segment: evaluate the
                    // quadratic at its stationary parameter.
                    float denominator = a - (2F * c) + b;
                    if (MathF.Abs(denominator) > 1e-6F)
                    {
                        float t = (a - c) / denominator;
                        if (t is > 0F and < 1F)
                        {
                            float omt = 1F - t;
                            float extreme = (omt * omt * a) + (2F * omt * t * c) + (t * t * b);
                            min = Math.Min(min, extreme);
                            max = Math.Max(max, extreme);
                        }
                    }
                }
            }
        }

        return max >= min;
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
        Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
        GlyphVector scaledVector = this.GetScaledOutline(scaledPPEM, hintingMode);

        IList<ControlPoint> controlPoints = scaledVector.ControlPoints;
        IReadOnlyList<ushort> endPoints = scaledVector.EndPoints;

        // Offset translation, synthetic oblique, and layout rotation are applied per point at
        // emit time so the cached outline stays shareable across runs, modes, and positioned
        // offsets. Placement lands after hinting, matching FreeType's treatment of GPOS
        // offsets, followed by the Y-flip into device space and the origin translation.
        Matrix3x2 outlineTransform = this.GetOutlineTransform(mode, textRun);
        Matrix3x2 emit = Matrix3x2.CreateTranslation((this.Offset + positionOffset) * scale);
        emit *= outlineTransform;
        emit *= Matrix3x2.CreateScale(1F, -1F);
        emit.Translation += glyphOrigin;

        float boldStrength = this.GetSyntheticBoldStrength(scaledPPEM, textRun);
        EmboldeningGlyphRenderer? emboldening = null;
        IGlyphRenderer target = renderer;
        if (boldStrength > 0F)
        {
            emboldening = EmboldeningGlyphRenderer.Rent(renderer, boldStrength);
            target = emboldening;
        }

        try
        {
            int endOfContour = -1;
            for (int i = 0; i < scaledVector.EndPoints.Count; i++)
            {
                int startOfContour = endOfContour + 1;
                endOfContour = endPoints[i];
                if (startOfContour == endOfContour)
                {
                    // cjFillPolygon omits contours that contain only one stored point.
                    continue;
                }

                target.BeginFigure();

                ControlPoint first = controlPoints[startOfContour];
                ControlPoint last = controlPoints[endOfContour];
                Vector2 contourStart;
                int currentIndex;
                if (first.OnCurve)
                {
                    // Native emission gives the first stored on-curve point priority even
                    // when the contour's final stored point is also on curve.
                    contourStart = Vector2.Transform(first.Point, emit);
                    currentIndex = startOfContour + 1;
                }
                else if (last.OnCurve)
                {
                    contourStart = Vector2.Transform(last.Point, emit);
                    currentIndex = startOfContour;
                }
                else
                {
                    // This is the only implied midpoint cjFillPolygon rounds to 26.6. The
                    // internal midpoints of a quadratic run remain exact half-grid values.
                    contourStart = Vector2.Transform(GetImpliedContourStart(first.Point, last.Point), emit);
                    currentIndex = startOfContour;
                }

                target.MoveTo(contourStart);

                while (currentIndex <= endOfContour)
                {
                    ControlPoint current = controlPoints[currentIndex];
                    if (current.OnCurve)
                    {
                        target.LineTo(Vector2.Transform(current.Point, emit));
                        currentIndex++;
                        continue;
                    }

                    Vector2 control = Vector2.Transform(current.Point, emit);
                    currentIndex++;
                    while (currentIndex <= endOfContour && !controlPoints[currentIndex].OnCurve)
                    {
                        Vector2 nextControl = Vector2.Transform(controlPoints[currentIndex].Point, emit);

                        // A native QSPLINE stores adjacent off-curve controls directly; its
                        // implied endpoint is their unrounded midpoint in 16.16 output space.
                        target.QuadraticBezierTo(control, (control + nextControl) * 0.5F);
                        control = nextControl;
                        currentIndex++;
                    }

                    Vector2 endpoint = contourStart;
                    if (currentIndex <= endOfContour)
                    {
                        // cjFillPolygon appends the next on-curve point to the QSPLINE and
                        // consumes it, so it is not emitted again as a line endpoint.
                        endpoint = Vector2.Transform(controlPoints[currentIndex].Point, emit);
                        currentIndex++;
                    }

                    target.QuadraticBezierTo(control, endpoint);
                }

                target.EndFigure();
            }

            // Emit the completed fill group before FontGlyphMetrics ends the glyph.
            emboldening?.CompleteOutline();
        }
        finally
        {
            emboldening?.Release();
        }
    }

    /// <summary>
    /// Builds the scaled, hinted outline copy cached per pixel size and hinting mode.
    /// A deep copy is scaled so that the globally cached design unit instance is never
    /// altered. The hinter always receives the upright, untranslated outline. Grid fitting
    /// is whatever the font's own instructions perform and nothing more: a font that hints
    /// one axis, or none, is rendered as it asks to be.
    /// </summary>
    /// <param name="key">The cache key carrying the pixel size and resolved hinting mode.</param>
    /// <returns>The scaled <see cref="GlyphVector"/>.</returns>
    private GlyphVector CreateScaledVector(ScaledVectorKey key)
    {
        Vector2 scale = new Vector2(key.ScaledPPEM) / this.ScaleFactor;
        GlyphVector clone = GlyphVector.DeepClone(this.vector);
        if (key.HintingMode == HintingMode.Full
            && this.FontMetrics.GlyphVariationProcessor is null
            && this.ScaleFactor == new Vector2(this.UnitsPerEm * 72F))
        {
            // Static TrueType outlines enter GDI's scaler as integral font units. Use its
            // reduced integer ratio directly so negative half-grid coordinates take the
            // scl_FRound direction selected by the native ComputeScaling routine.
            GlyphVector.ScaleTrueTypeInPlace(ref clone, (int)(key.ScaledPPEM / 72F), this.UnitsPerEm);
        }
        else
        {
            // Variable outlines and typographic sub/superscript transforms use separate
            // native fixed-font-unit paths; retain their existing transform until those
            // functions are ported from the disassembly.
            GlyphVector.TransformInPlace(ref clone, Matrix3x2.CreateScale(scale));
            GlyphVector.QuantizeInPlace(ref clone);
        }

        float pixelSize = key.ScaledPPEM / 72F;
        _ = this.FontMetrics.ApplyTrueTypeHinting(key.HintingMode, this, ref clone, in this.vector, scale, pixelSize);

        return clone;
    }

    /// <summary>
    /// Identifies a cached scaled outline by pixel size and the resolved hinting mode that
    /// shaped it. Both participate in identity so alternating modes at one size never
    /// return an outline fitted for another mode.
    /// </summary>
    private readonly struct ScaledVectorKey : IEquatable<ScaledVectorKey>
    {
        public ScaledVectorKey(float scaledPPEM, HintingMode hintingMode)
        {
            this.ScaledPPEM = scaledPPEM;
            this.HintingMode = hintingMode;
        }

        public float ScaledPPEM { get; }

        public HintingMode HintingMode { get; }

        public bool Equals(ScaledVectorKey other) => this.ScaledPPEM == other.ScaledPPEM && this.HintingMode == other.HintingMode;

        public override bool Equals(object? obj) => obj is ScaledVectorKey other && this.Equals(other);

        public override int GetHashCode() => HashCode.Combine(this.ScaledPPEM, this.HintingMode);
    }
}
