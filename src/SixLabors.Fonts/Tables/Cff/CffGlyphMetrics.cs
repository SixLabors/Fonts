// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a glyph metric from a particular Compact Font Face.
/// </summary>
internal class CffGlyphMetrics : FontGlyphMetrics
{
    private CffGlyphData glyphData;

    /// <summary>
    /// Buffered upright outline copies keyed by pixel size and hinting mode, mirroring the
    /// TrueType scaled outline cache: placement, synthetic oblique, layout rotation and
    /// origin apply per point at replay time so one buffered copy serves every run and
    /// positioned offset. Allocated on first render because shaping and measurement clone
    /// metrics without ever rendering them.
    /// </summary>
    private ConcurrentDictionary<ScaledOutlineKey, CffOutline>? scaledOutlineCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CffGlyphMetrics"/> class with text attribute parameters.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="glyphData">The CFF glyph data containing the charstring program.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <param name="advanceWidth">The advance width.</param>
    /// <param name="advanceHeight">The advance height.</param>
    /// <param name="leftSideBearing">The left side bearing.</param>
    /// <param name="topSideBearing">The top side bearing.</param>
    /// <param name="unitsPerEM">The units per em.</param>
    /// <param name="textAttributes">The text attributes.</param>
    /// <param name="textDecorations">The text decorations.</param>
    /// <param name="glyphType">The glyph type.</param>
    public CffGlyphMetrics(
        StreamFontMetrics fontMetrics,
        ushort glyphId,
        CodePoint codePoint,
        CffGlyphData glyphData,
        Bounds bounds,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        GlyphType glyphType)
        : base(
              fontMetrics,
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
              glyphType)
        => this.glyphData = glyphData;

    /// <inheritdoc/>
    internal override bool TryGetFittedOutlinePlacement(
        float scaledPPEM,
        HintingMode resolvedHintingMode,
        bool includeInkExtent,
        out FittedOutlinePlacement placement)
    {
        CffOutline outline = this.GetScaledOutline(scaledPPEM, resolvedHintingMode);
        if (!outline.IsFitted)
        {
            placement = default;
            return false;
        }

        Vector2 scale = this.GetOutlineScale(scaledPPEM);
        float fittedMin = 0F;
        float fittedMax = 0F;
        bool hasFittedInk = includeInkExtent && outline.TryGetFittedInkExtentX(out fittedMin, out fittedMax);

        placement = new FittedOutlinePlacement(scale, hasFittedInk, fittedMin, fittedMax);
        return true;
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
        Matrix3x2 transform = this.GetOutlineTransform(mode, textRun);
        Vector2 scale = this.GetOutlineScale(scaledPPEM);

        // Charstrings evaluate once per size into a buffered outline; every render replays
        // it through a unit scale transforming renderer, which reproduces the streaming
        // arithmetic exactly. Each mode shapes the outline differently: unhinted geometry,
        // vertical only fitting, or full grid fitting.
        CffOutline outline = this.GetScaledOutline(scaledPPEM, hintingMode);

        Vector2 scaledOffset = (this.Offset + positionOffset) * scale;

        float boldStrength = this.GetSyntheticBoldStrength(scaledPPEM, textRun);
        if (boldStrength > 0F)
        {
            // Flush through the supplied renderer before glyph completion so skip-ink observes
            // the same synthesized outline as the drawing renderer.
            EmboldeningGlyphRenderer target = EmboldeningGlyphRenderer.Rent(renderer, boldStrength);
            try
            {
                TransformingGlyphRenderer transforming = new(target, glyphOrigin, Vector2.One, scaledOffset, transform);
                outline.ReplayTo(ref transforming);
                target.CompleteOutline();
            }
            finally
            {
                target.Release();
            }
        }
        else
        {
            TransformingGlyphRenderer transforming = new(renderer, glyphOrigin, Vector2.One, scaledOffset, transform);
            outline.ReplayTo(ref transforming);
        }
    }

    /// <summary>
    /// Computes the pixels per design unit scale for the glyph, folding in the CFF font
    /// matrix. The normalized font matrix is identity for the default
    /// [0.001, 0, 0, 0.001, 0, 0] with one thousand units per em.
    /// </summary>
    /// <param name="scaledPPEM">The scaled size to render/measure the glyph at.</param>
    /// <returns>The per axis scale.</returns>
    private Vector2 GetOutlineScale(float scaledPPEM)
    {
        Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
        if (this.glyphData.FontMatrix is double[] fm)
        {
            float upm = this.UnitsPerEm;
            scale *= new Vector2((float)(fm[0] * upm), (float)(fm[3] * upm));
        }

        return scale;
    }

    /// <summary>
    /// Gets the buffered CFF outline shaped for the specified size and hinting mode.
    /// </summary>
    /// <param name="scaledPPEM">The scaled pixels-per-em value used to build the outline.</param>
    /// <param name="hintingMode">The resolved hinting mode shaping the outline.</param>
    /// <returns>The cached outline.</returns>
    private CffOutline GetScaledOutline(float scaledPPEM, HintingMode hintingMode)
    {
        ConcurrentDictionary<ScaledOutlineKey, CffOutline> cache =
            LazyInitializer.EnsureInitialized(ref this.scaledOutlineCache, static () => new ConcurrentDictionary<ScaledOutlineKey, CffOutline>());

        return cache.GetOrAdd(new ScaledOutlineKey(scaledPPEM, hintingMode), static (key, self) => self.CreateScaledOutline(key), this);
    }

    /// <summary>
    /// Returns the size to render/measure the glyph at. Under full hinting the em square is
    /// constrained to whole pixels, matching the classic rasterizers the declared hinting
    /// values were authored for.
    /// </summary>
    /// <param name="pointSize">The font size in pt units.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
    /// <param name="hintingMode">The hinting mode, which may constrain the size to whole pixels.</param>
    /// <returns>The <see cref="float"/>.</returns>
    internal override float GetScaledSize(float pointSize, float dpi, HintingMode hintingMode)
    {
        float scaledPPEM = base.GetScaledSize(pointSize, dpi, hintingMode);
        if (hintingMode == HintingMode.Full)
        {
            scaledPPEM = MathF.Max(1F, MathF.Floor((scaledPPEM / 72F) + 0.5F)) * 72F;
        }

        return scaledPPEM;
    }

    /// <summary>
    /// Attempts to compute the whole pixel advance width for the glyph under full hinting.
    /// CFF fonts carry no device advance records, so the design advance rounds linearly to
    /// whole pixels at the integer em size, matching the grid fitted outlines. No outline
    /// is built and no cache is touched, so this is safe on the layout hot path.
    /// </summary>
    /// <param name="pointSize">The font size in pt units.</param>
    /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
    /// <param name="hintingMode">The requested hinting mode.</param>
    /// <param name="advancePx">The advance width in whole device pixels.</param>
    /// <returns><see langword="true"/> if a hinted advance applies; otherwise, <see langword="false"/>.</returns>
    public override bool TryGetHintedAdvanceWidth(float pointSize, float dpi, HintingMode hintingMode, out float advancePx)
    {
        advancePx = 0F;
        if (hintingMode != HintingMode.Full)
        {
            return false;
        }

        // Sub and superscript metrics shrink the glyph by adjusting the scale factor while
        // the em square stays at the base size, so layout maps font units to pixels through
        // a different ratio than the base em. Whole pixel advances resolved against the
        // base em would be rescaled by that ratio on the way back into layout units,
        // advancing the pen by the wrong amount, so those runs keep their shaped
        // fractional advances.
        if (this.ScaleFactor.X != this.UnitsPerEm * 72F)
        {
            return false;
        }

        float scaledPPEM = this.GetScaledSize(pointSize, dpi, hintingMode);
        advancePx = MathF.Floor((this.AdvanceWidth * scaledPPEM / (this.UnitsPerEm * 72F)) + 0.5F);
        return true;
    }

    /// <summary>
    /// Builds the buffered outline cached per pixel size and hinting mode, aligning the
    /// mode semantics with the TrueType interpreter: unhinted geometry stays untouched,
    /// standard hinting fits the vertical axis only from the declared horizontal stem
    /// zones and blue zone flats, and full hinting fits both axes. The anchor array is
    /// font level state precomputed at parse time, so fitting allocates nothing here.
    /// </summary>
    /// <param name="key">The cache key carrying the pixel size and hinting mode.</param>
    /// <returns>The buffered <see cref="CffOutline"/>.</returns>
    private CffOutline CreateScaledOutline(ScaledOutlineKey key)
    {
        Vector2 scale = this.GetOutlineScale(key.ScaledPPEM);
        CffOutline outline = this.glyphData.BuildOutline(scale);

        if (key.HintingMode != HintingMode.None)
        {
            // Standard hinting fits the vertical axis only, matching the instruction driven
            // formats where a font may hint one axis; full hinting fits both.
            CffHintingValues hintingValues = this.glyphData.HintingValues ?? CffHintingValues.Empty;

            // The native fitter stores the font transform in signed 16.16 before it maps
            // stems or outline points. Quantizing once here keeps every downstream map
            // operation on the same device scale.
            float fixedHorizontalScale = CffFixedPoint.ToSingle(CffFixedPoint.FromSingle(scale.X));
            float fixedVerticalScale = CffFixedPoint.ToSingle(CffFixedPoint.FromSingle(scale.Y));

            HintMapOptions options = new(
                key.HintingMode == HintingMode.Full,
                true,
                hintingValues.Zones,
                hintingValues.FamilyZones,
                hintingValues.BlueFuzz,
                hintingValues.AdjustedBlueScale,
                hintingValues.BlueShift,
                hintingValues.ExpansionFactorFixed,
                hintingValues.VerticalStemWidths,
                hintingValues.HorizontalStemWidths,
                CffFixedPoint.FromSingle(key.ScaledPPEM / 72F),
                outline.LockFixMapOk,
                fixedHorizontalScale,
                fixedVerticalScale);

            if (HintMap.FitInPlace(outline.Points, outline.Verbs, outline.ContourEnds, outline.VerticalStems, outline.HorizontalStems, outline.InitialStemCount, outline.HintRegions, outline.CounterMasks, in options))
            {
                outline.MarkFitted(key.HintingMode == HintingMode.Full);
                return outline;
            }
        }

        // Nothing fitted, so the character space points still need the plain scale.
        Vector2[] points = outline.Points;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] *= scale;
        }

        return outline;
    }

    /// <summary>
    /// Identifies a cached buffered outline by pixel size and the hinting mode that shaped
    /// it. Modes producing identical geometry are normalized onto one key before lookup.
    /// </summary>
    private readonly struct ScaledOutlineKey : IEquatable<ScaledOutlineKey>
    {
        public ScaledOutlineKey(float scaledPPEM, HintingMode hintingMode)
        {
            this.ScaledPPEM = scaledPPEM;
            this.HintingMode = hintingMode;
        }

        public float ScaledPPEM { get; }

        public HintingMode HintingMode { get; }

        public bool Equals(ScaledOutlineKey other) => this.ScaledPPEM == other.ScaledPPEM && this.HintingMode == other.HintingMode;

        public override bool Equals(object? obj) => obj is ScaledOutlineKey other && this.Equals(other);

        public override int GetHashCode() => HashCode.Combine(this.ScaledPPEM, this.HintingMode);
    }
}
