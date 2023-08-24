// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The GSUB and GPOS tables use the Glyph Class Definition table (GlyphClassDef) to identify which glyph classes to adjust with lookups.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gdef#glyph-class-definition-table"/>
/// </summary>
public enum GlyphClassDef
{
    /// <summary>
    /// Base glyph (single character, spacing glyph).
    /// </summary>
    BaseGlyph = 1,

    /// <summary>
    /// Ligature glyph (multiple character, spacing glyph).
    /// </summary>
    LigatureGlyph = 2,

    /// <summary>
    /// Mark glyph (non-spacing combining glyph).
    /// </summary>
    MarkGlyph = 3,

    /// <summary>
    /// Component glyph (part of single character, spacing glyph).
    /// </summary>
    ComponentGlyph = 4,
}
