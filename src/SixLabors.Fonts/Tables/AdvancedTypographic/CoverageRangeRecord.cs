// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// A CoverageRangeRecord defines a range of glyph IDs and its starting coverage index.
/// Used in CoverageTable Format 2.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-2"/>
/// </summary>
[DebuggerDisplay("StartGlyphId: {StartGlyphId}, EndGlyphId: {EndGlyphId}, Index: {Index}")]
internal readonly struct CoverageRangeRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageRangeRecord"/> struct.
    /// </summary>
    /// <param name="startGlyphId">The first glyph ID in the range.</param>
    /// <param name="endGlyphId">The last glyph ID in the range.</param>
    /// <param name="startCoverageIndex">The coverage index of the first glyph ID in the range.</param>
    public CoverageRangeRecord(ushort startGlyphId, ushort endGlyphId, ushort startCoverageIndex)
    {
        this.StartGlyphId = startGlyphId;
        this.EndGlyphId = endGlyphId;
        this.Index = startCoverageIndex;
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
    /// Gets the coverage index of the first glyph ID in the range.
    /// </summary>
    public ushort Index { get; }
}
