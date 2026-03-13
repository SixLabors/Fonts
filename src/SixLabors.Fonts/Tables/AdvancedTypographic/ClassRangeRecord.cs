// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A ClassRangeRecord defines a range of glyph IDs that belong to a specific class.
/// Used in ClassDefinitionTable Format 2.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table-format-2"/>
/// </summary>
[DebuggerDisplay("StartGlyphId: {StartGlyphId}, EndGlyphId: {EndGlyphId}, Class: {Class}")]
internal readonly struct ClassRangeRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassRangeRecord"/> struct.
    /// </summary>
    /// <param name="startGlyphId">The first glyph ID in the range.</param>
    /// <param name="endGlyphId">The last glyph ID in the range.</param>
    /// <param name="glyphClass">The class value applied to all glyphs in the range.</param>
    public ClassRangeRecord(ushort startGlyphId, ushort endGlyphId, ushort glyphClass)
    {
        this.StartGlyphId = startGlyphId;
        this.EndGlyphId = endGlyphId;
        this.Class = glyphClass;
    }

    /// <summary>
    /// Gets the first glyph ID in the range.
    /// </summary>
    public ushort StartGlyphId { get; }

    /// <summary>
    /// Gets the last glyph ID in the range.
    /// </summary>
    public ushort EndGlyphId { get; }

    /// <summary>
    /// Gets the class value applied to all glyphs in the range.
    /// </summary>
    public ushort Class { get; }
}
