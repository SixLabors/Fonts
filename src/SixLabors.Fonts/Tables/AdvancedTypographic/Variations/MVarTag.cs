// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Defines the tags used by the MVAR table to identify font-wide metrics
/// that can be varied in a variable font.
/// Each tag maps to a specific field in the OS/2, hhea, vhea, or post tables.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/mvar"/>
/// </summary>
internal static class MVarTag
{
    // Horizontal metrics (OS/2 typo or hhea).

    /// <summary>OS/2.sTypoAscender / hhea.ascender ('hasc').</summary>
    public static readonly Tag HorizontalAscender = Tag.Parse("hasc");

    /// <summary>OS/2.sTypoDescender / hhea.descender ('hdsc').</summary>
    public static readonly Tag HorizontalDescender = Tag.Parse("hdsc");

    /// <summary>OS/2.sTypoLineGap / hhea.lineGap ('hlgp').</summary>
    public static readonly Tag HorizontalLineGap = Tag.Parse("hlgp");

    /// <summary>OS/2.usWinAscent ('hcla').</summary>
    public static readonly Tag HorizontalClippingAscent = Tag.Parse("hcla");

    /// <summary>OS/2.usWinDescent ('hcld').</summary>
    public static readonly Tag HorizontalClippingDescent = Tag.Parse("hcld");

    // Vertical metrics (vhea).

    /// <summary>vhea.ascent ('vasc').</summary>
    public static readonly Tag VerticalAscender = Tag.Parse("vasc");

    /// <summary>vhea.descent ('vdsc').</summary>
    public static readonly Tag VerticalDescender = Tag.Parse("vdsc");

    /// <summary>vhea.lineGap ('vlgp').</summary>
    public static readonly Tag VerticalLineGap = Tag.Parse("vlgp");

    // OS/2 subscript metrics.

    /// <summary>OS/2.ySubscriptXSize ('sbxs').</summary>
    public static readonly Tag SubscriptXSize = Tag.Parse("sbxs");

    /// <summary>OS/2.ySubscriptYSize ('sbys').</summary>
    public static readonly Tag SubscriptYSize = Tag.Parse("sbys");

    /// <summary>OS/2.ySubscriptXOffset ('sbxo').</summary>
    public static readonly Tag SubscriptXOffset = Tag.Parse("sbxo");

    /// <summary>OS/2.ySubscriptYOffset ('sbyo').</summary>
    public static readonly Tag SubscriptYOffset = Tag.Parse("sbyo");

    // OS/2 superscript metrics.

    /// <summary>OS/2.ySuperscriptXSize ('spxs').</summary>
    public static readonly Tag SuperscriptXSize = Tag.Parse("spxs");

    /// <summary>OS/2.ySuperscriptYSize ('spys').</summary>
    public static readonly Tag SuperscriptYSize = Tag.Parse("spys");

    /// <summary>OS/2.ySuperscriptXOffset ('spxo').</summary>
    public static readonly Tag SuperscriptXOffset = Tag.Parse("spxo");

    /// <summary>OS/2.ySuperscriptYOffset ('spyo').</summary>
    public static readonly Tag SuperscriptYOffset = Tag.Parse("spyo");

    // OS/2 strikeout metrics.

    /// <summary>OS/2.yStrikeoutSize ('strs').</summary>
    public static readonly Tag StrikeoutSize = Tag.Parse("strs");

    /// <summary>OS/2.yStrikeoutPosition ('stro').</summary>
    public static readonly Tag StrikeoutPosition = Tag.Parse("stro");

    // post underline metrics.

    /// <summary>post.underlineThickness ('unds').</summary>
    public static readonly Tag UnderlineThickness = Tag.Parse("unds");

    /// <summary>post.underlinePosition ('undo').</summary>
    public static readonly Tag UnderlinePosition = Tag.Parse("undo");

    // OS/2 miscellaneous metrics.

    /// <summary>OS/2.sxHeight ('xhgt').</summary>
    public static readonly Tag XHeight = Tag.Parse("xhgt");

    /// <summary>OS/2.sCapHeight ('cpht').</summary>
    public static readonly Tag CapHeight = Tag.Parse("cpht");

    // hhea caret metrics.

    /// <summary>hhea.caretSlopeRise ('hcrn').</summary>
    public static readonly Tag HorizontalCaretRise = Tag.Parse("hcrn");

    /// <summary>hhea.caretSlopeRun ('hcrs').</summary>
    public static readonly Tag HorizontalCaretRun = Tag.Parse("hcrs");

    /// <summary>hhea.caretOffset ('hcof').</summary>
    public static readonly Tag HorizontalCaretOffset = Tag.Parse("hcof");

    // vhea caret metrics.

    /// <summary>vhea.caretSlopeRise ('vcrn').</summary>
    public static readonly Tag VerticalCaretRise = Tag.Parse("vcrn");

    /// <summary>vhea.caretSlopeRun ('vcrs').</summary>
    public static readonly Tag VerticalCaretRun = Tag.Parse("vcrs");

    /// <summary>vhea.caretOffset ('vcof').</summary>
    public static readonly Tag VerticalCaretOffset = Tag.Parse("vcof");
}
