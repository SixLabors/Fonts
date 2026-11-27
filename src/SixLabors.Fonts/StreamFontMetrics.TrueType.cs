// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.General.Svg;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Tables.TrueType.Hinting;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <content>
/// Contains TrueType specific methods.
/// </content>
internal partial class StreamFontMetrics
{
    // Bounded pool of interpreters shared across threads.
    // Size tied to logical CPU count.
    private readonly ObjectPool<TrueTypeInterpreter>? interpreterPool;

    /// <summary>
    /// The font level alignment heights the geometric grid fitter anchors top edges to, in
    /// design units. Allocated once and shared by every glyph and size so per glyph
    /// fitting never allocates. The benign publication race always stores the same values.
    /// </summary>
    private float[]? gridFitTopAnchors;

    /// <summary>
    /// Gets the exception that aborted the most recent failed glyph program, or
    /// <see langword="null"/> when no program has failed. Diagnostic only: written on the
    /// hinting path without synchronization.
    /// </summary>
    public Exception? LastHintingFault { get; private set; }

    /// <summary>
    /// Gets the font level alignment heights the geometric grid fitter anchors top edges
    /// to, in design units.
    /// </summary>
    internal float[] GridFitTopAnchors => this.gridFitTopAnchors ??= [this.XHeight, this.CapHeight];

    /// <summary>
    /// Attempts to resolve the precomputed device advance width for the glyph at the given
    /// pixel size from the 'hdmx' table, avoiding any instruction execution.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="ppem">The pixels per em to look up.</param>
    /// <param name="advance">The advance width in whole device pixels.</param>
    /// <returns><see langword="true"/> if a device record covers the size; otherwise, <see langword="false"/>.</returns>
    internal bool TryGetDeviceAdvanceWidth(ushort glyphId, int ppem, out byte advance)
    {
        advance = 0;
        HdmxTable? hdmx = this.trueTypeFontTables?.Hdmx;
        return hdmx is not null && hdmx.TryGetAdvance(ppem, glyphId, out advance);
    }

    private TrueTypeInterpreter CreateInterpreter()
    {
        TrueTypeFontTables tables = this.trueTypeFontTables!;
        MaximumProfileTable maxp = tables.Maxp;

        TrueTypeInterpreter interpreter = new(
            maxp.MaxStackElements,
            maxp.MaxStorage,
            maxp.MaxFunctionDefs,
            maxp.MaxInstructionDefs,
            maxp.MaxTwilightPoints);

        FpgmTable? fpgm = tables.Fpgm;
        if (fpgm is not null)
        {
            interpreter.InitializeFunctionDefs(fpgm.Instructions);
        }

        return interpreter;
    }

    internal TrueTypeHintingResult ApplyTrueTypeHinting(HintingMode hintingMode, FontGlyphMetrics metrics, ref GlyphVector glyphVector, in GlyphVector unscaled, Vector2 scaleXY, float pixelSize)
    {
        if (hintingMode == HintingMode.None || this.outlineType != OutlineType.TrueType)
        {
            return TrueTypeHintingResult.None;
        }

        if (this.trueTypeFontTables is null || this.interpreterPool is null)
        {
            return TrueTypeHintingResult.None;
        }

        TrueTypeFontTables tables = this.trueTypeFontTables;
        TrueTypeInterpreter interpreter = this.interpreterPool.Get();

        try
        {
            CvtTable? cvt = tables.Cvt;
            PrepTable? prep = tables.Prep;
            float hintingScaleFactor = pixelSize / this.UnitsPerEm;

            // Sub and superscript metrics shrink the outline by adjusting the scale factor
            // while the requested pixel size still describes the base em. Control values and
            // delta triggers must match the scale the outline was actually transformed by,
            // so both derive from that scale when the two disagree.
            if (metrics.ScaleFactor.Y != this.UnitsPerEm * 72F)
            {
                hintingScaleFactor = scaleXY.Y;
                pixelSize = this.UnitsPerEm * scaleXY.Y;
            }

            // Apply cvar deltas to CVT values for variable fonts before hinting.
            short[]? cvtValues = cvt?.ControlValues;
            if (cvtValues is not null && this.GlyphVariationProcessor is not null)
            {
                cvtValues = this.GlyphVariationProcessor.ApplyCvtDeltas(cvtValues) ?? cvtValues;
            }

            // Provide normalized axis coordinates for the GETVARIATION opcode.
            interpreter.SetNormalizedAxisCoordinates(this.GlyphVariationProcessor?.NormalizedCoordinates);

            interpreter.SetControlValueTable(cvtValues, hintingScaleFactor, pixelSize, this.UnitsPerEm, prep?.Instructions, hintingMode);

            // Phantom points are passed unrounded: their original positions must keep the
            // true fractional metrics because instructions measure signed distances against
            // them. Only the current positions of the dependent phantoms are rounded, inside
            // the interpreter, before the glyph program runs.
            Bounds bounds = glyphVector.Bounds;

            Vector2 pp1 = new(bounds.Min.X - (metrics.LeftSideBearing * scaleXY.X), 0);
            Vector2 pp2 = new(pp1.X + (metrics.AdvanceWidth * scaleXY.X), 0);
            Vector2 pp3 = new(0, bounds.Max.Y + (metrics.TopSideBearing * scaleXY.Y));
            Vector2 pp4 = new(0, pp3.Y - (metrics.AdvanceHeight * scaleXY.Y));

            // The same four phantom points in font units, which IP interpolates from.
            Bounds unscaledBounds = unscaled.Bounds;
            Vector2 unscaledPp1 = new(unscaledBounds.Min.X - metrics.LeftSideBearing, 0);
            Vector2 unscaledPp2 = new(unscaledPp1.X + metrics.AdvanceWidth, 0);
            Vector2 unscaledPp3 = new(0, unscaledBounds.Max.Y + metrics.TopSideBearing);
            Vector2 unscaledPp4 = new(0, unscaledPp3.Y - metrics.AdvanceHeight);

            if (!GlyphVector.Hint(hintingMode, ref glyphVector, in unscaled, interpreter, pp1, pp2, pp3, pp4, unscaledPp1, unscaledPp2, unscaledPp3, unscaledPp4))
            {
                // The fault, inhibition and dropout state must be read before the interpreter
                // returns to the pool. A faulted program leaves the glyph unhinted by design;
                // the exception is retained so diagnostics can identify which instruction
                // stream failed. Inhibition is reported separately because it is the font's own
                // decision rather than an error, and callers must honor it. The dropout request
                // survives either outcome, since an unhinted glyph still carries sub pixel
                // features that the rasterizer must be told to preserve.
                this.LastHintingFault = interpreter.LastError;
                return interpreter.LastRunInhibited ? TrueTypeHintingResult.Inhibited : TrueTypeHintingResult.Failed;
            }

            // The touch flags must be read before the interpreter returns to the pool.
            return interpreter.LastRunTouchedX ? TrueTypeHintingResult.AppliedXY : TrueTypeHintingResult.AppliedY;
        }
        finally
        {
            this.interpreterPool.Return(interpreter);
        }
    }

    private static StreamFontMetrics LoadTrueTypeFont(FontReader reader, FontSource source)
    {
        // Load using recommended order for best performance.
        // https://learn.microsoft.com/en-gb/typography/opentype/spec/recom#optimized-table-ordering
        // 'head', 'hhea', 'maxp', OS/2, 'hmtx', LTSH, VDMX, 'hdmx', 'cmap', 'fpgm', 'prep', 'cvt ', 'loca', 'glyf', 'kern', 'name', 'post', 'gasp', PCLT, DSIG
        HeadTable head = reader.GetTable<HeadTable>();
        HorizontalHeadTable hhea = reader.GetTable<HorizontalHeadTable>();
        MaximumProfileTable maxp = reader.GetTable<MaximumProfileTable>();
        OS2Table os2 = reader.GetTable<OS2Table>();
        HorizontalMetricsTable htmx = reader.GetTable<HorizontalMetricsTable>();
        CMapTable cmap = reader.GetTable<CMapTable>();
        cmap.SetSymbolFontPage(os2.FontPage);
        FpgmTable? fpgm = reader.TryGetTable<FpgmTable>();
        PrepTable? prep = reader.TryGetTable<PrepTable>();
        CvtTable? cvt = reader.TryGetTable<CvtTable>();

        // hdmx depends on the glyph count from maxp, so it cannot be auto-loaded via TryGetTable.
        HdmxTable? hdmx = HdmxTable.Load(reader, maxp.GlyphCount);
        IndexLocationTable loca = reader.GetTable<IndexLocationTable>();
        GlyphTable glyf = reader.GetTable<GlyphTable>();
        KerningTable? kern = reader.TryGetTable<KerningTable>();
        NameTable name = reader.GetTable<NameTable>();
        PostTable post = reader.GetTable<PostTable>();

        VerticalHeadTable? vhea = reader.TryGetTable<VerticalHeadTable>();
        VerticalMetricsTable? vmtx = null;
        if (vhea is not null)
        {
            vmtx = reader.TryGetTable<VerticalMetricsTable>();
        }

        BaseTable? baseTable = reader.TryGetTable<BaseTable>();
        GlyphDefinitionTable? gdef = reader.TryGetTable<GlyphDefinitionTable>();
        GSubTable? gSub = reader.TryGetTable<GSubTable>();
        GPosTable? gPos = reader.TryGetTable<GPosTable>();

        FVarTable? fvar = reader.TryGetTable<FVarTable>();
        AVarTable? avar = reader.TryGetTable<AVarTable>();
        GVarTable? gvar = reader.TryGetTable<GVarTable>();
        HVarTable? hvar = reader.TryGetTable<HVarTable>();
        VVarTable? vvar = reader.TryGetTable<VVarTable>();
        MVarTable? mvar = reader.TryGetTable<MVarTable>();

        // cvar depends on axisCount from fvar, so it cannot be auto-loaded via TryGetTable.
        CVarTable? cvar = null;
        if (fvar is not null)
        {
            cvar = CVarTable.Load(reader, fvar.AxisCount);
        }

        ColrTable? colr = reader.TryGetTable<ColrTable>();
        CpalTable? cpal = reader.TryGetTable<CpalTable>();

        SvgTable? svg = reader.TryGetTable<SvgTable>();

        TrueTypeFontTables tables = new(cmap, head, hhea, htmx, maxp, name, os2, post, glyf, loca)
        {
            Fpgm = fpgm,
            Prep = prep,
            Cvt = cvt,
            Hdmx = hdmx,
            Kern = kern,
            Vhea = vhea,
            Vmtx = vmtx,
            Base = baseTable,
            Gdef = gdef,
            GSub = gSub,
            GPos = gPos,
            Colr = colr,
            Cpal = cpal,
            Fvar = fvar,
            Gvar = gvar,
            Hvar = hvar,
            Vvar = vvar,
            Mvar = mvar,
            Avar = avar,
            Svg = svg,
            Cvar = cvar
        };

        GlyphVariationProcessor? glyphVariationProcessor = null;
        if (fvar != null)
        {
            // Use the item variation store from HVAR or VVAR if available (for metrics variations).
            // A variable font may have gvar without HVAR/VVAR (using phantom points for metrics instead).
            ItemVariationStore? itemVariationStore = hvar?.ItemVariationStore ?? vvar?.ItemVariationStore;
            glyphVariationProcessor = new GlyphVariationProcessor(itemVariationStore, fvar, avar, gvar, hvar, vvar, mvar, cvar);
        }

        return new StreamFontMetrics(tables, source, glyphVariationProcessor);
    }

    private FontGlyphMetrics CreateTrueTypeGlyphMetrics(
        in CodePoint codePoint,
        ushort glyphId,
        GlyphType glyphType,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        ColorFontSupport colorSupport,
        bool isVerticalLayout,
        FontPalette? palette)
    {
        TrueTypeFontTables tables = this.trueTypeFontTables!;
        GlyphTable glyf = tables.Glyf;
        HorizontalMetricsTable htmx = tables.Htmx;
        VerticalMetricsTable? vtmx = tables.Vmtx;

        Vector2 glyphAdvanceAdjustment = Vector2.Zero;
        GlyphVariationProcessor? variationProcessor = this.GlyphVariationProcessor;
        GlyphVector vector = variationProcessor is not null && tables.Gvar is not null
            ? glyf.GetVariedGlyph(glyphId, variationProcessor, 0, out glyphAdvanceAdjustment)
            : glyf.GetGlyph(glyphId);

        Bounds bounds = vector.Bounds;

        ushort advanceWidth = htmx.GetAdvancedWidth(glyphId);
        short lsb = htmx.GetLeftSideBearing(glyphId);

        // A dedicated metrics table takes precedence over the adjustment carried
        // by the glyph's metric points.
        if (this.GlyphVariationProcessor is not null)
        {
            advanceWidth = (ushort)Math.Max(0, advanceWidth + this.GlyphVariationProcessor.AdvanceAdjustment(glyphId, glyphAdvanceAdjustment.X));
        }

        IMetricsHeader metrics = isVerticalLayout ? this.VerticalMetrics : this.HorizontalMetrics;
        ushort advancedHeight = (ushort)(metrics.Ascender - metrics.Descender);
        short tsb = (short)(metrics.Ascender - bounds.Max.Y);
        if (vtmx != null)
        {
            advancedHeight = vtmx.GetAdvancedHeight(glyphId);
            tsb = vtmx.GetTopSideBearing(glyphId);
        }

        // Vertical metrics use the same precedence rule as horizontal metrics.
        if (this.GlyphVariationProcessor is not null)
        {
            advancedHeight = (ushort)Math.Max(0, advancedHeight + this.GlyphVariationProcessor.VerticalAdvanceAdjustment(glyphId, glyphAdvanceAdjustment.Y));
        }

        ColrTable? colr = tables.Colr;
        if ((colorSupport & ColorFontSupport.ColrV1) == ColorFontSupport.ColrV1 && colr?.ContainsColorV1Glyph(glyphId) == true)
        {
            CpalTable? cpal = tables.Cpal;
            ColrV1GlyphSource glyphSource = new(colr, cpal, palette, i => glyf.GetGlyph(i), this.GlyphVariationProcessor);

            return new PaintedGlyphMetrics(
                this,
                glyphId,
                codePoint,
                glyphSource,
                palette,
                bounds,
                advanceWidth,
                advancedHeight,
                lsb,
                tsb,
                this.UnitsPerEm,
                textAttributes,
                textDecorations);
        }

        if ((colorSupport & ColorFontSupport.ColrV0) == ColorFontSupport.ColrV0 && colr?.ContainsColorV0Glyph(glyphId) == true)
        {
            CpalTable? cpal = tables.Cpal;
            ColrV0GlyphSource glyphSource = new(colr, cpal, palette, i => glyf.GetGlyph(i));

            return new PaintedGlyphMetrics(
                this,
                glyphId,
                codePoint,
                glyphSource,
                palette,
                bounds,
                advanceWidth,
                advancedHeight,
                lsb,
                tsb,
                this.UnitsPerEm,
                textAttributes,
                textDecorations);
        }

        SvgTable? svg = tables.Svg;
        if ((colorSupport & ColorFontSupport.Svg) == ColorFontSupport.Svg && svg?.ContainsGlyph(glyphId) == true)
        {
            return new PaintedGlyphMetrics(
                this,
                glyphId,
                codePoint,
                this.GetOrCreateSvgGlyphSource(svg),
                null,
                bounds,
                advanceWidth,
                advancedHeight,
                lsb,
                tsb,
                this.UnitsPerEm,
                textAttributes,
                textDecorations);
        }

        return new TrueTypeGlyphMetrics(
            this,
            glyphId,
            codePoint,
            vector,
            advanceWidth,
            advancedHeight,
            lsb,
            tsb,
            this.UnitsPerEm,
            textAttributes,
            textDecorations,
            glyphType);
    }

    private sealed class TrueTypeInterpreterPooledObjectPolicy
     : IPooledObjectPolicy<TrueTypeInterpreter>
    {
        private readonly StreamFontMetrics owner;

        public TrueTypeInterpreterPooledObjectPolicy(StreamFontMetrics owner)
            => this.owner = owner;

        public TrueTypeInterpreter Create()
            => this.owner.CreateInterpreter();

        public bool Return(TrueTypeInterpreter interpreter)
            => true; // Always accept returned instances.
    }
}
