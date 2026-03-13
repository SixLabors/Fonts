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

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Contains the collection of OpenType tables required for fonts with CFF or CFF2 outlines.
/// </summary>
internal sealed class CompactFontTables : IFontTables
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompactFontTables"/> class with the required OpenType tables.
    /// </summary>
    /// <param name="cmap">The character-to-glyph mapping table.</param>
    /// <param name="head">The font header table.</param>
    /// <param name="hhea">The horizontal header table.</param>
    /// <param name="htmx">The horizontal metrics table.</param>
    /// <param name="maxp">The maximum profile table.</param>
    /// <param name="name">The naming table.</param>
    /// <param name="os2">The OS/2 and Windows metrics table.</param>
    /// <param name="post">The PostScript name mapping table.</param>
    /// <param name="cff">The CFF or CFF2 outline table.</param>
    public CompactFontTables(
        CMapTable cmap,
        HeadTable head,
        HorizontalHeadTable hhea,
        HorizontalMetricsTable htmx,
        MaximumProfileTable maxp,
        NameTable name,
        OS2Table os2,
        PostTable post,
        ICffTable cff)
    {
        this.Cmap = cmap;
        this.Head = head;
        this.Hhea = hhea;
        this.Htmx = htmx;
        this.Maxp = maxp;
        this.Name = name;
        this.Os2 = os2;
        this.Post = post;
        this.Cff = cff;
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
    /// Gets or sets the optional 'fvar' (Font Variations) table defining variation axes.
    /// </summary>
    public FVarTable? FVar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'avar' (Axis Variations) table for non-linear axis mapping.
    /// </summary>
    public AVarTable? AVar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'gvar' (Glyph Variations) table. Typically unused for CFF fonts.
    /// </summary>
    public GVarTable? GVar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'HVAR' (Horizontal Metrics Variations) table.
    /// </summary>
    public HVarTable? HVar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'VVAR' (Vertical Metrics Variations) table.
    /// </summary>
    public VVarTable? VVar { get; set; }

    /// <summary>
    /// Gets or sets the optional 'MVAR' (Metrics Variations) table for global metric deltas.
    /// </summary>
    public MVarTable? MVar { get; set; }

    /// <summary>
    /// Gets or sets the optional SVG table containing scalable vector glyph data.
    /// </summary>
    public SvgTable? Svg { get; set; }

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

    /// <summary>
    /// Gets or sets the CFF or CFF2 outline table.
    /// </summary>
    public ICffTable Cff { get; set; }
}
