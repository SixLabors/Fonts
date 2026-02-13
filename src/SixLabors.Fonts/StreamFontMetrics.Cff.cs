// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.Cff;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.General.Svg;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <content>
/// Contains CFF specific methods.
/// </content>
internal partial class StreamFontMetrics
{
    private static StreamFontMetrics LoadCompactFont(FontReader reader)
    {
        // Load using recommended order for best performance.
        // https://learn.microsoft.com/en-gb/typography/opentype/spec/recom#optimized-table-ordering
        // 'head', 'hhea', 'maxp', OS/2, 'name', 'cmap', 'post', 'CFF ' / 'CFF2'
        HeadTable head = reader.GetTable<HeadTable>();
        HorizontalHeadTable hhea = reader.GetTable<HorizontalHeadTable>();
        MaximumProfileTable maxp = reader.GetTable<MaximumProfileTable>();
        OS2Table os2 = reader.GetTable<OS2Table>();
        NameTable name = reader.GetTable<NameTable>();
        CMapTable cmap = reader.GetTable<CMapTable>();
        PostTable post = reader.GetTable<PostTable>();
        ICffTable? cff = reader.TryGetTable<Cff1Table>() ?? (ICffTable?)reader.TryGetTable<Cff2Table>();

        // TODO: VORG
        HorizontalMetricsTable htmx = reader.GetTable<HorizontalMetricsTable>();
        VerticalHeadTable? vhea = reader.TryGetTable<VerticalHeadTable>();
        VerticalMetricsTable? vmtx = null;
        if (vhea is not null)
        {
            vmtx = reader.TryGetTable<VerticalMetricsTable>();
        }

        KerningTable? kern = reader.TryGetTable<KerningTable>();

        GlyphDefinitionTable? gdef = reader.TryGetTable<GlyphDefinitionTable>();
        GSubTable? gSub = reader.TryGetTable<GSubTable>();
        GPosTable? gPos = reader.TryGetTable<GPosTable>();

        ColrTable? colr = reader.TryGetTable<ColrTable>();
        CpalTable? cpal = reader.TryGetTable<CpalTable>();
        SvgTable? svg = reader.TryGetTable<SvgTable>();

        // Variations related tables.
        FVarTable? fVar = reader.TryGetTable<FVarTable>();
        AVarTable? aVar = reader.TryGetTable<AVarTable>();
        GVarTable? gVar = reader.TryGetTable<GVarTable>();
        HVarTable? hVar = reader.TryGetTable<HVarTable>();
        VVarTable? vVar = reader.TryGetTable<VVarTable>();

        GlyphVariationProcessor? glyphVariationProcessor = null;
        if (cff?.ItemVariationStore != null)
        {
            if (fVar is null)
            {
                throw new InvalidFontFileException("missing fvar table required for glyph variations processing");
            }

            // TODO: The docs say that hvar and vvar can be used for CFF fonts so how do we determine when to use them?
            glyphVariationProcessor = new GlyphVariationProcessor(cff.ItemVariationStore, fVar, aVar, gVar, hVar, vVar);
        }

        CompactFontTables tables = new(cmap, head, hhea, htmx, maxp, name, os2, post, cff!)
        {
            Kern = kern,
            Vhea = vhea,
            Vmtx = vmtx,
            Gdef = gdef,
            GSub = gSub,
            GPos = gPos,
            Colr = colr,
            Cpal = cpal,
            FVar = fVar,
            AVar = aVar,
            GVar = gVar,
            HVar = hVar,
            VVar = vVar,
            Svg = svg
        };

        return new StreamFontMetrics(tables, glyphVariationProcessor);
    }

    private GlyphMetrics CreateCffGlyphMetrics(
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
        CompactFontTables tables = this.compactFontTables!;
        ICffTable cff = tables.Cff;
        HorizontalMetricsTable htmx = tables.Htmx;
        VerticalMetricsTable? vtmx = tables.Vmtx;
        FVarTable? fVar = tables.FVar;
        AVarTable? aVar = tables.AVar;
        GVarTable? gVar = tables.GVar;

        CffGlyphData vector = cff.GetGlyph(glyphId);
        vector.FVar = fVar;
        vector.AVar = aVar;
        vector.GVar = gVar;
        Bounds bounds = vector.GetBounds();
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

        // TODO: Support CFF based COLR glyphs.
        // This requires parsing the CFF charstrings to extract the glyph vectors.
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

        return new CffGlyphMetrics(
            this,
            glyphId,
            codePoint,
            vector,
            bounds,
            advanceWidth,
            advancedHeight,
            lsb,
            tsb,
            this.UnitsPerEm,
            textAttributes,
            textDecorations,
            glyphType);
    }
}
