// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Svg;

/// <summary>
/// Represents an entry in the SVG Document Index of the SVG table.
/// Each entry maps a contiguous range of glyph IDs to an SVG document.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/svg"/>
/// </summary>
internal readonly struct SvgDocumentIndexEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SvgDocumentIndexEntry"/> struct.
    /// </summary>
    /// <param name="startGlyphId">The first glyph ID in the range (inclusive).</param>
    /// <param name="endGlyphId">The last glyph ID in the range (inclusive).</param>
    /// <param name="svgDocOffset">The offset from the beginning of the SVG Document Index to the SVG document.</param>
    /// <param name="svgDocLength">The length of the SVG document data in bytes.</param>
    public SvgDocumentIndexEntry(ushort startGlyphId, ushort endGlyphId, uint svgDocOffset, uint svgDocLength)
    {
        this.StartGlyphId = startGlyphId;
        this.EndGlyphId = endGlyphId;
        this.SvgDocOffset = svgDocOffset;
        this.SvgDocLength = svgDocLength;
    }

    /// <summary>
    /// Gets the first glyph ID in this range (inclusive).
    /// </summary>
    public ushort StartGlyphId { get; }

    /// <summary>
    /// Gets the last glyph ID in this range (inclusive).
    /// </summary>
    public ushort EndGlyphId { get; }

    /// <summary>
    /// Gets the offset from the beginning of the SVG Document Index to the SVG document.
    /// </summary>
    public uint SvgDocOffset { get; }

    /// <summary>
    /// Gets the length of the SVG document data in bytes.
    /// </summary>
    public uint SvgDocLength { get; }
}
