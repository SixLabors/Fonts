// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Colr;
using SixLabors.Fonts.Tables.General.Kern;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Tables.General.Post;

namespace SixLabors.Fonts.Tables;

#pragma warning disable SA1600 // Elements should be documented
/// <summary>
/// Defines the contract for shared font tables
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/otff#font-tables"/>
/// </summary>
internal interface IFontTables
{
    // Required Tables both TTF and CFF
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
    CMapTable Cmap { get; set; }

    HeadTable Head { get; set; }

    HorizontalHeadTable Hhea { get; set; }

    HorizontalMetricsTable Htmx { get; set; }

    MaximumProfileTable Maxp { get; set; }

    NameTable Name { get; set; }

    OS2Table Os2 { get; set; }

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
    public GlyphDefinitionTable? Gdef { get; set; }

    public GSubTable? GSub { get; set; }

    public GPosTable? GPos { get; set; }

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
    ColrTable? Colr { get; set; }

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
    KerningTable? Kern { get; set; }

    VerticalHeadTable? Vhea { get; set; }

    VerticalMetricsTable? Vmtx { get; set; }
}
#pragma warning restore SA1600 // Elements should be documented

