// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts.Tables.Cff
{
    internal struct CompactFontTables : IFontTables
    {
        public CMapTable Cmap { get; set; }

        public HeadTable Head { get; set; }

        public HorizontalHeadTable Hhea { get; set; }

        public HorizontalMetricsTable Htmx { get; set; }

        public MaximumProfileTable Maxp { get; set; }

        public NameTable Name { get; set; }

        public OS2Table Os2 { get; set; }

        public PostTable Post { get; set; }

        public GlyphDefinitionTable? Gdef { get; set; }

        public GSubTable? GSub { get; set; }

        public GPosTable? GPos { get; set; }

        public ColrTable? Colr { get; set; }

        public CpalTable? Cpal { get; set; }

        public KerningTable? Kern { get; set; }

        public VerticalHeadTable? Vhea { get; set; }

        public VerticalMetricsTable? Vmtx { get; set; }

        // Tables Related to CFF Outlines
        // +------+----------------------------------+
        // | Tag  | Name                             |
        // +======+==================================+
        // | CFF  | Compact Font Format 1.0          |
        // +------+----------------------------------+
        // | CFF2 | Compact Font Format 2.0          |
        // +------+----------------------------------+
        // | VORG | Vertical Origin (optional table) |
        // +------+----------------------------------+
        public CffTable Cff { get; set; }
    }
}
