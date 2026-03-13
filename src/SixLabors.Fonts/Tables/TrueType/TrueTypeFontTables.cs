// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;
using SixLabors.Fonts.Tables.General.Svg;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using SixLabors.Fonts.Tables.TrueType.Hinting;

namespace SixLabors.Fonts.Tables.TrueType;

/// <summary>
/// Implements <see cref="IFontTables"/> for TrueType fonts, providing access to
/// required and optional tables including TrueType-specific tables such as
/// 'glyf', 'loca', 'cvt', 'fpgm', and 'prep'.
/// </summary>
internal sealed class TrueTypeFontTables : IFontTables
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrueTypeFontTables"/> class.
    /// </summary>
    /// <param name="cmap">The character-to-glyph mapping table.</param>
    /// <param name="head">The font header table.</param>
    /// <param name="hhea">The horizontal header table.</param>
    /// <param name="htmx">The horizontal metrics table.</param>
    /// <param name="maxp">The maximum profile table.</param>
    /// <param name="name">The naming table.</param>
    /// <param name="os2">The OS/2 and Windows metrics table.</param>
    /// <param name="post">The PostScript name table.</param>
    /// <param name="glyph">The glyph data table.</param>
    /// <param name="loca">The index-to-location table.</param>
    public TrueTypeFontTables(
        CMapTable cmap,
        HeadTable head,
        HorizontalHeadTable hhea,
        HorizontalMetricsTable htmx,
        MaximumProfileTable maxp,
        NameTable name,
        OS2Table os2,
        PostTable post,
        GlyphTable glyph,
        IndexLocationTable loca)
    {
        this.Cmap = cmap;
        this.Head = head;
        this.Hhea = hhea;
        this.Htmx = htmx;
        this.Maxp = maxp;
        this.Name = name;
        this.Os2 = os2;
        this.Post = post;
        this.Glyf = glyph;
        this.Loca = loca;
    }

    /// <inheritdoc/>
    public CMapTable Cmap { get; set; }

    /// <inheritdoc/>
    public HeadTable Head { get; set; }

    /// <inheritdoc/>
    public HorizontalHeadTable Hhea { get; set; }

    /// <inheritdoc/>
    public HorizontalMetricsTable Htmx { get; set; }

    /// <inheritdoc/>
    public MaximumProfileTable Maxp { get; set; }

    /// <inheritdoc/>
    public NameTable Name { get; set; }

    /// <inheritdoc/>
    public OS2Table Os2 { get; set; }

    /// <inheritdoc/>
    public PostTable Post { get; set; }

    /// <inheritdoc/>
    public GlyphDefinitionTable? Gdef { get; set; }

    /// <inheritdoc/>
    public GSubTable? GSub { get; set; }

    /// <inheritdoc/>
    public GPosTable? GPos { get; set; }

    /// <inheritdoc/>
    public ColrTable? Colr { get; set; }

    /// <inheritdoc/>
    public CpalTable? Cpal { get; set; }

    /// <inheritdoc/>
    public KerningTable? Kern { get; set; }

    /// <inheritdoc/>
    public VerticalHeadTable? Vhea { get; set; }

    /// <inheritdoc/>
    public VerticalMetricsTable? Vmtx { get; set; }

    /// <summary>
    /// Gets or sets the optional SVG table containing scalable vector glyph data.
    /// </summary>
    public SvgTable? Svg { get; set; }

    // Tables Related to TrueType Outlines
    // +------+-----------------------------------------------+
    // | Tag  | Name                                          |
    // +======+===============================================+
    // | cvt  | Control Value Table (optional table)          |
    // +------+-----------------------------------------------+
    // | fpgm | Font program (optional table)                 |
    // +------+-----------------------------------------------+
    // | glyf | Glyph data                                    |
    // +------+-----------------------------------------------+
    // | loca | Index to location                             |
    // +------+-----------------------------------------------+
    // | prep | CVT Program (optional table)                  |
    // +------+-----------------------------------------------+
    // | gasp | Grid-fitting/Scan-conversion (optional table) |
    // +------+-----------------------------------------------+

    /// <summary>
    /// Gets or sets the optional 'cvt ' (Control Value Table) for TrueType hinting.
    /// </summary>
    public CvtTable? Cvt { get; set; }

    /// <summary>
    /// Gets or sets the optional 'fpgm' (Font Program) table for TrueType hinting.
    /// </summary>
    public FpgmTable? Fpgm { get; set; }

    /// <summary>
    /// Gets or sets the 'glyf' (Glyph Data) table containing TrueType glyph outlines.
    /// </summary>
    public GlyphTable Glyf { get; set; }

    /// <summary>
    /// Gets or sets the 'loca' (Index to Location) table mapping glyph IDs to offsets in 'glyf'.
    /// </summary>
    public IndexLocationTable Loca { get; set; }

    /// <summary>
    /// Gets or sets the optional 'prep' (Control Value Program) table for TrueType hinting.
    /// </summary>
    public PrepTable? Prep { get; set; }

    /// <summary>
    /// Gets or sets the optional 'fvar' (Font Variations) table defining variation axes.
    /// </summary>
    public FVarTable? Fvar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'avar' (Axis Variations) table for non-linear axis mapping.
    /// </summary>
    public AVarTable? Avar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'gvar' (Glyph Variations) table for TrueType outline deltas.
    /// </summary>
    public GVarTable? Gvar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'HVAR' (Horizontal Metrics Variations) table.
    /// </summary>
    public HVarTable? Hvar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'VVAR' (Vertical Metrics Variations) table.
    /// </summary>
    public VVarTable? Vvar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'MVAR' (Metrics Variations) table for global metric deltas.
    /// </summary>
    public MVarTable? Mvar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'cvar' (CVT Variations) table for control value deltas.
    /// </summary>
    public CVarTable? Cvar { get; set; }
}
