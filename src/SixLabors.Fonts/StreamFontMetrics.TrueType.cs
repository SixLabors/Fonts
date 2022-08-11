// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.TrueType;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Tables.TrueType.Hinting;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <content>
    /// Contains TrueType specific methods.
    /// </content>
    internal partial class StreamFontMetrics
    {
        /// <summary>
        /// <para>
        /// TODO: See if something can be done about this. For potential workarounds <see href="http://rastertragedy.com/RTRCh4.htm#Sec1"/>
        /// These common fonts can be hinted by the engine only in XY mode and only if we do not scale the x-dimension.
        /// even then the output is worse than the non-hinted version due to thinning of certain strokes.
        /// <see href="http://agg.sourceforge.net/antigrain.com/research/font_rasterization/"/>.
        /// </para>
        /// <para>
        /// We'll sacrifice hinting for these fonts to allow the engine to work better for modern fonts and avoid potentially raised issues.
        /// </para>
        /// </summary>
        private static readonly string[] BadHintList = { "Arial", "Times New Roman", "Tahoma", "Palatino Linotype", "Segoe UI" };
        private bool? shouldHint;

        [ThreadStatic]
        private TrueTypeInterpreter? interpreter;

        internal void ApplyTrueTypeHinting(HintingMode hintingMode, GlyphMetrics metrics, ref GlyphVector glyphVector, Vector2 scaleXY, float pixelSize)
        {
            this.shouldHint ??= Array.IndexOf(BadHintList, this.Description.FontFamilyInvariantCulture) == -1;
            if (this.shouldHint != true)
            {
                return;
            }

            if (hintingMode == HintingMode.None || this.outlineType != OutlineType.TrueType)
            {
                return;
            }

            TrueTypeFontTables tables = this.trueTypeFontTables!;
            if (this.interpreter == null)
            {
                MaximumProfileTable maxp = tables.Maxp;
                this.interpreter = new TrueTypeInterpreter(
                    maxp.MaxStackElements,
                    maxp.MaxStorage,
                    maxp.MaxFunctionDefs,
                    maxp.MaxInstructionDefs,
                    maxp.MaxTwilightPoints);

                FpgmTable? fpgm = tables.Fpgm;
                if (fpgm is not null)
                {
                    this.interpreter.InitializeFunctionDefs(fpgm.Instructions);
                }
            }

            CvtTable? cvt = tables.Cvt;
            PrepTable? prep = tables.Prep;
            float scaleFactor = pixelSize / this.UnitsPerEm;
            this.interpreter.SetControlValueTable(cvt?.ControlValues, scaleFactor, pixelSize, prep?.Instructions);

            Bounds bounds = glyphVector.GetBounds();

            Vector2 pp1 = new(MathF.Round(bounds.Min.X - (metrics.LeftSideBearing * scaleXY.X)), 0);
            Vector2 pp2 = new(MathF.Round(pp1.X + (metrics.AdvanceWidth * scaleXY.X)), 0);
            Vector2 pp3 = new(0, MathF.Round(bounds.Max.Y + (metrics.TopSideBearing * scaleXY.Y)));
            Vector2 pp4 = new(0, MathF.Round(pp3.Y - (metrics.AdvanceHeight * scaleXY.Y)));

            GlyphVector.Hint(hintingMode, ref glyphVector, this.interpreter, pp1, pp2, pp3, pp4);
        }

        private static StreamFontMetrics LoadTrueTypeFont(FontReader reader)
        {
            // Load using recommended order for best performance.
            // https://www.microsoft.com/typography/otspec/recom.htm#TableOrdering
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

            ColrTable? colr = reader.TryGetTable<ColrTable>();
            CpalTable? cpal = reader.TryGetTable<CpalTable>();

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
            };

            return new StreamFontMetrics(tables);
        }

        private GlyphMetrics CreateTrueTypeGlyphMetrics(
            CodePoint codePoint,
            ushort glyphId,
            GlyphType glyphType,
            ushort palleteIndex = 0)
        {
            TrueTypeFontTables tables = this.trueTypeFontTables!;
            GlyphTable glyf = tables.Glyf;
            HorizontalMetricsTable htmx = tables.Htmx;
            VerticalMetricsTable? vtmx = tables.Vmtx;

            GlyphVector vector = glyf.GetGlyph(glyphId);
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

            return new TrueTypeGlyphMetrics(
                this,
                codePoint,
                vector,
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
