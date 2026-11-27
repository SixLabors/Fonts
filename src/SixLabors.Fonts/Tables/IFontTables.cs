// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.AdvancedTypographic.Aat;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;

namespace SixLabors.Fonts.Tables;

/// <summary>
/// Provides the font tables shared by TrueType and CFF fonts.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/otff#font-tables"/>
/// </summary>
internal interface IFontTables
{
    // Required tables for TrueType and CFF fonts.
    // +-------+-----------------------------------+
    // | Tag   | Name                              |
    // +=======+===================================+
    // | cmap  | Character to glyph mapping        |
    // +-------+-----------------------------------+
    // | head  | Font header                       |
    // +-------+-----------------------------------+
    // | hhea  | Horizontal header                 |
    // +-------+-----------------------------------+
    // | hmtx  | Horizontal metrics                |
    // +-------+-----------------------------------+
    // | maxp  | Maximum profile                   |
    // +-------+-----------------------------------+
    // | name  | Naming table                      |
    // +-------+-----------------------------------+
    // | OS/2  | OS/2 and Windows specific metrics |
    // +-------+-----------------------------------+
    // | post  | PostScript information            |
    // +-------+-----------------------------------+

    /// <summary>
    /// Gets or sets the character-to-glyph mapping table.
    /// </summary>
    CMapTable Cmap { get; set; }

    /// <summary>
    /// Gets or sets the font header.
    /// </summary>
    HeadTable Head { get; set; }

    /// <summary>
    /// Gets or sets the horizontal metrics header.
    /// </summary>
    HorizontalHeadTable Hhea { get; set; }

    /// <summary>
    /// Gets or sets the horizontal glyph metrics table.
    /// </summary>
    HorizontalMetricsTable Htmx { get; set; }

    /// <summary>
    /// Gets or sets the maximum profile table.
    /// </summary>
    MaximumProfileTable Maxp { get; set; }

    /// <summary>
    /// Gets or sets the font naming table.
    /// </summary>
    NameTable Name { get; set; }

    /// <summary>
    /// Gets or sets the OS/2 metrics and classification table.
    /// </summary>
    OS2Table Os2 { get; set; }

    /// <summary>
    /// Gets or sets the PostScript information table.
    /// </summary>
    PostTable Post { get; set; }

    // Advanced Typographic Tables
    // +------+-------------------------+
    // | Tag | Name                     |
    // +======+=========================+
    // | BASE | Baseline data           |
    // +------+-------------------------+
    // | GDEF | Glyph definition data   |
    // +------+-------------------------+
    // | GPOS | Glyph positioning data  |
    // +------+-------------------------+
    // | GSUB | Glyph substitution data |
    // +------+-------------------------+
    // | JSTF | Justification data      |
    // +------+-------------------------+
    // | MATH | Math layout data        |
    // +------+-------------------------+

    /// <summary>
    /// Gets or sets the optional baseline table.
    /// </summary>
    public BaseTable? Base { get; set; }

    /// <summary>
    /// Gets or sets the optional glyph definition table.
    /// </summary>
    public GlyphDefinitionTable? Gdef { get; set; }

    /// <summary>
    /// Gets or sets the optional OpenType glyph substitution table.
    /// </summary>
    public GSubTable? GSub { get; set; }

    /// <summary>
    /// Gets or sets the optional OpenType glyph positioning table.
    /// </summary>
    public GPosTable? GPos { get; set; }

    // Apple Advanced Typography tables.

    /// <summary>
    /// Gets or sets the optional classic glyph substitution table (<c>mort</c>).
    /// </summary>
    MortTable? Mort { get; set; }

    /// <summary>
    /// Gets or sets the optional extended glyph substitution table (<c>morx</c>).
    /// </summary>
    MorxTable? Morx { get; set; }

    /// <summary>
    /// Gets or sets the optional extended kerning table (<c>kerx</c>).
    /// </summary>
    KerxTable? Kerx { get; set; }

    // Tables Related to Color Fonts
    // +------+------------------------------------------+
    // | Tag  | Name                                     |
    // +======+==========================================+
    // | COLR | Color table                              |
    // +------+------------------------------------------+
    // | CPAL | Color palette table                      |
    // +------+------------------------------------------+
    // | CBDT | Color bitmap data                        |
    // +------+------------------------------------------+
    // | CBLC | Color bitmap location data               |
    // +------+------------------------------------------+
    // | sbix | Standard bitmap graphics                 |
    // +------+------------------------------------------+
    // | SVG  | The SVG (Scalable Vector Graphics) table |
    // +------+------------------------------------------+

    /// <summary>
    /// Gets or sets the optional glyph color table.
    /// </summary>
    ColrTable? Colr { get; set; }

    /// <summary>
    /// Gets or sets the optional color palette table.
    /// </summary>
    CpalTable? Cpal { get; set; }

    // +------+---------------------------+
    // | Tag  | Name                      |
    // +======+===========================+
    // | DSIG | Digital signature         |
    // +------+---------------------------+
    // | hdmx | Horizontal device metrics |
    // +------+---------------------------+
    // | kern | Kerning                   |
    // +------+---------------------------+
    // | LTSH | Linear threshold data     |
    // +------+---------------------------+
    // | MERG | Merge                     |
    // +------+---------------------------+
    // | meta | Metadata                  |
    // +------+---------------------------+
    // | STAT | Style attributes          |
    // +------+---------------------------+
    // | PCLT | PCL 5 data                |
    // +------+---------------------------+
    // | VDMX | Vertical device metrics   |
    // +------+---------------------------+
    // | vhea | Vertical Metrics header   |
    // +------+---------------------------+
    // | vmtx | Vertical Metrics          |
    // +------+---------------------------+

    /// <summary>
    /// Gets or sets the optional legacy kerning table.
    /// </summary>
    KernTable? Kern { get; set; }

    /// <summary>
    /// Gets or sets the optional vertical metrics header.
    /// </summary>
    VerticalHeadTable? Vhea { get; set; }

    /// <summary>
    /// Gets or sets the optional vertical glyph metrics table.
    /// </summary>
    VerticalMetricsTable? Vmtx { get; set; }
}
