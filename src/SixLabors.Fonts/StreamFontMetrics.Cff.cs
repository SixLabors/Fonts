// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.Cff;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <content>
    /// Contains CFF specific methods.
    /// </content>
    internal partial class StreamFontMetrics
    {
        private static StreamFontMetrics LoadCompactFont(FontReader reader)
        {
            // Load using recommended order for best performance.
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
            // 'head', 'hhea', 'maxp', OS/2, 'name', 'cmap', 'post', 'CFF '
            HeadTable head = reader.GetTable<HeadTable>();
            HorizontalHeadTable hhea = reader.GetTable<HorizontalHeadTable>();
            MaximumProfileTable maxp = reader.GetTable<MaximumProfileTable>();
            OS2Table os2 = reader.GetTable<OS2Table>();
            NameTable name = reader.GetTable<NameTable>();
            CMapTable cmap = reader.GetTable<CMapTable>();
            PostTable post = reader.GetTable<PostTable>();
            CffTable cff = reader.GetTable<CffTable>(); // TODO: CFF2, VORG

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

            CompactFontTables tables = new(cmap, head, hhea, htmx, maxp, name, os2, post, cff)
            {
                Kern = kern,
                Vhea = vhea,
                Vmtx = vmtx,
                Gdef = gdef,
                GSub = gSub,
                GPos = gPos,
                Colr = colr,
                Cpal = cpal,
            };

            return new StreamFontMetrics(tables);
        }

        private GlyphMetrics CreateCffGlyphMetrics(
            CodePoint codePoint,
            ushort glyphId,
            GlyphType glyphType,
            ushort palleteIndex = 0)
        {
            // TODO: Do we need this?
            if (this.outlineType != OutlineType.CFF)
            {
                throw new InvalidOperationException("Only CFF fonts can be used with this method.");
            }

            CompactFontTables tables = this.compactFontTables!;
            CffTable cff = tables.Cff;
            HorizontalMetricsTable htmx = tables.Htmx;
            VerticalMetricsTable? vtmx = tables.Vmtx;

            CffGlyphData vector = cff.GetGlyph(glyphId);
            Bounds bounds = vector.GetBounds();
            ushort advanceWidth = htmx.GetAdvancedWidth(glyphId);
            short lsb = htmx.GetLeftSideBearing(glyphId);

            // Provide a default for the advance height. This is overwritten for vertical fonts.
            ushort advancedHeight = (ushort)(this.Ascender - this.Descender);
            short tsb = (short)(this.Ascender - bounds.Max.Y);
            if (vtmx != null)
            {
                advancedHeight = vtmx.GetAdvancedHeight(glyphId);
                tsb = vtmx.GetTopSideBearing(glyphId);
            }

            GlyphColor? color = null;
            if (glyphType == GlyphType.ColrLayer)
            {
                // 0xFFFF is special index meaning use foreground color and thus leave unset
                if (palleteIndex != 0xFFFF)
                {
                    CpalTable? cpal = tables.Cpal;
                    color = cpal?.GetGlyphColor(0, palleteIndex);
                }
            }

            return new CffGlyphMetrics(
                this,
                codePoint,
                vector,
                bounds,
                advanceWidth,
                advancedHeight,
                lsb,
                tsb,
                this.UnitsPerEm,
                glyphId,
                glyphType,
                color);
        }
    }
}
