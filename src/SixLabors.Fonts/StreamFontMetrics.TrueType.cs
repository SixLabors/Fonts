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

    internal void ApplyTrueTypeHinting(HintingMode hintingMode, GlyphMetrics metrics, ref GlyphVector glyphVector, Vector2 scaleXY, float pixelSize)
    {
        if (hintingMode == HintingMode.None || this.outlineType != OutlineType.TrueType)
        {
            return;
        }

        if (this.trueTypeFontTables is null || this.interpreterPool is null)
        {
            return;
        }

        TrueTypeFontTables tables = this.trueTypeFontTables;
        TrueTypeInterpreter interpreter = this.interpreterPool.Get();

        try
        {
            CvtTable? cvt = tables.Cvt;
            PrepTable? prep = tables.Prep;
            float hintingScaleFactor = pixelSize / this.UnitsPerEm;
            interpreter.SetControlValueTable(cvt?.ControlValues, hintingScaleFactor, pixelSize, prep?.Instructions);

            Bounds bounds = glyphVector.Bounds;

            Vector2 pp1 = new(MathF.Round(bounds.Min.X - (metrics.LeftSideBearing * scaleXY.X)), 0);
            Vector2 pp2 = new(MathF.Round(pp1.X + (metrics.AdvanceWidth * scaleXY.X)), 0);
            Vector2 pp3 = new(0, MathF.Round(bounds.Max.Y + (metrics.TopSideBearing * scaleXY.Y)));
            Vector2 pp4 = new(0, MathF.Round(pp3.Y - (metrics.AdvanceHeight * scaleXY.Y)));

            GlyphVector.Hint(hintingMode, ref glyphVector, interpreter, pp1, pp2, pp3, pp4);
        }
        finally
        {
            this.interpreterPool.Return(interpreter);
        }
    }

    private static StreamFontMetrics LoadTrueTypeFont(FontReader reader)
    {
        // TODO: This doesn't seem correct. The glyf table should only load the raw data.
        // Transformations should probably be applied later when glyphs are requested.
        // Load glyph variations related tables first, because glyph table needs them.


        // Load using recommended order for best performance.
        // https://learn.microsoft.com/en-gb/typography/opentype/spec/recom#optimized-table-ordering
        // 'head', 'hhea', 'maxp', OS/2, 'hmtx', LTSH, VDMX, 'hdmx', 'cmap', 'fpgm', 'prep', 'cvt ', 'loca', 'glyf', 'kern', 'name', 'post', 'gasp', PCLT, DSIG
        HeadTable head = reader.GetTable<HeadTable>();
        HorizontalHeadTable hhea = reader.GetTable<HorizontalHeadTable>();
        MaximumProfileTable maxp = reader.GetTable<MaximumProfileTable>();
        OS2Table os2 = reader.GetTable<OS2Table>();
        HorizontalMetricsTable htmx = reader.GetTable<HorizontalMetricsTable>();
        CMapTable cmap = reader.GetTable<CMapTable>();
        FpgmTable? fpgm = reader.TryGetTable<FpgmTable>();
        PrepTable? prep = reader.TryGetTable<PrepTable>();
        CvtTable? cvt = reader.TryGetTable<CvtTable>();
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

        GlyphDefinitionTable? gdef = reader.TryGetTable<GlyphDefinitionTable>();
        GSubTable? gSub = reader.TryGetTable<GSubTable>();
        GPosTable? gPos = reader.TryGetTable<GPosTable>();

        FVarTable? fvar = reader.TryGetTable<FVarTable>();
        AVarTable? avar = reader.TryGetTable<AVarTable>();
        GVarTable? gvar = reader.TryGetTable<GVarTable>();
        HVarTable? hvar = reader.TryGetTable<HVarTable>();
        VVarTable? vvar = reader.TryGetTable<VVarTable>();

        ColrTable? colr = reader.TryGetTable<ColrTable>();
        CpalTable? cpal = reader.TryGetTable<CpalTable>();

        SvgTable? svg = reader.TryGetTable<SvgTable>();

        TrueTypeFontTables tables = new(cmap, head, hhea, htmx, maxp, name, os2, post, glyf, loca)
        {
            Fpgm = fpgm,
            Prep = prep,
            Cvt = cvt,
            Kern = kern,
            Vhea = vhea,
            Vmtx = vmtx,
            Gdef = gdef,
            GSub = gSub,
            GPos = gPos,
            Colr = colr,
            Cpal = cpal,
            Fvar = fvar,
            Gvar = gvar,
            Hvar = hvar,
            Vvar = vvar,
            Avar = avar,
            Svg = svg
        };

        GlyphVariationProcessor? glyphVariationProcessor = null;
        if (fvar != null && (hvar != null || vvar != null))
        {
            // vvar can only be present if hvar is absent and vice versa.
            ItemVariationStore itemVariationStore = hvar != null ? hvar.ItemVariationStore : vvar!.ItemVariationStore;
            glyphVariationProcessor = new GlyphVariationProcessor(itemVariationStore, fvar, avar, gvar, hvar, vvar);
        }

        return new StreamFontMetrics(tables);
    }

    private GlyphMetrics CreateTrueTypeGlyphMetrics(
        in CodePoint codePoint,
        ushort glyphId,
        GlyphType glyphType,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        ColorFontSupport colorSupport,
        bool isVerticalLayout,
        ushort paletteIndex = 0)
    {
        // TODO: When do we require and how do we use the palette index?
        TrueTypeFontTables tables = this.trueTypeFontTables!;
        GlyphTable glyf = tables.Glyf;
        HorizontalMetricsTable htmx = tables.Htmx;
        VerticalMetricsTable? vtmx = tables.Vmtx;

        GlyphVector vector = glyf.GetGlyph(glyphId);

        Bounds bounds = vector.Bounds;

        ushort advanceWidth = htmx.GetAdvancedWidth(glyphId);
        short lsb = htmx.GetLeftSideBearing(glyphId);

        IMetricsHeader metrics = isVerticalLayout ? this.VerticalMetrics : this.HorizontalMetrics;
        ushort advancedHeight = (ushort)(metrics.Ascender - metrics.Descender);
        short tsb = (short)(metrics.Ascender - bounds.Max.Y);
        if (vtmx != null)
        {
            advancedHeight = vtmx.GetAdvancedHeight(glyphId);
            tsb = vtmx.GetTopSideBearing(glyphId);
        }

        ColrTable? colr = tables.Colr;
        if ((colorSupport & ColorFontSupport.ColrV1) == ColorFontSupport.ColrV1 && colr?.ContainsColorV1Glyph(glyphId) == true)
        {
            CpalTable? cpal = tables.Cpal;
            ColrV1GlyphSource glyphSource = new(colr, cpal, i => glyf.GetGlyph(i));

            return new PaintedGlyphMetrics(
                this,
                glyphId,
                codePoint,
                glyphSource,
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
            ColrV0GlyphSource glyphSource = new(colr, cpal, i => glyf.GetGlyph(i));

            return new PaintedGlyphMetrics(
                this,
                glyphId,
                codePoint,
                glyphSource,
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
                new SvgGlyphSource(svg),
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
