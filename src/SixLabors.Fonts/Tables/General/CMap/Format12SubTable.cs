// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

/// <summary>
/// Format 12 is a segmented coverage subtable used for character codes beyond the BMP (U+0000 to U+10FFFF).
/// It uses 32-bit character codes and groups of sequential mappings.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-12-segmented-coverage"/>
/// </summary>
internal sealed class Format12SubTable : CMapSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Format12SubTable"/> class.
    /// </summary>
    /// <param name="language">The language code for this subtable.</param>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="encoding">The platform-specific encoding identifier.</param>
    /// <param name="groups">The array of sequential map groups.</param>
    public Format12SubTable(uint language, PlatformIDs platform, ushort encoding, SequentialMapGroup[] groups)
        : base(platform, encoding, 4)
    {
        this.Language = language;
        this.SequentialMapGroups = groups;
    }

    /// <summary>
    /// Gets the array of sequential map groups defining character-to-glyph mappings.
    /// </summary>
    public SequentialMapGroup[] SequentialMapGroups { get; }

    /// <summary>
    /// Gets the language code for this subtable.
    /// </summary>
    public uint Language { get; }

    /// <inheritdoc/>
    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        int charAsInt = codePoint.Value;

        for (int i = 0; i < this.SequentialMapGroups.Length; i++)
        {
            ref SequentialMapGroup seg = ref this.SequentialMapGroups[i];

            if (charAsInt >= seg.StartCodePoint && charAsInt <= seg.EndCodePoint)
            {
                glyphId = (ushort)(charAsInt - seg.StartCodePoint + seg.StartGlyphId);
                return true;
            }
        }

        glyphId = 0;
        return false;
    }

    /// <inheritdoc/>
    public override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        for (int i = 0; i < this.SequentialMapGroups.Length; i++)
        {
            ref SequentialMapGroup seg = ref this.SequentialMapGroups[i];
            if (glyphId >= seg.StartGlyphId && glyphId <= seg.StartGlyphId + seg.EndCodePoint - seg.StartCodePoint)
            {
                // Reverse the calculation:
                // Forward: glyphId = (codePoint - StartCodePoint) + StartGlyphId
                // Reverse: codePoint = (glyphId - StartGlyphId) + StartCodePoint
                codePoint = new CodePoint(glyphId - seg.StartGlyphId + seg.StartCodePoint);
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    /// <inheritdoc/>
    public override IEnumerable<int> GetAvailableCodePoints()
        => this.SequentialMapGroups.SelectMany(segment =>
        {
            int start = (int)segment.StartCodePoint;
            int end = (int)segment.EndCodePoint;
            return Enumerable.Range(start, end - start + 1);
        });

    /// <summary>
    /// Loads one or more <see cref="Format12SubTable"/> instances from the specified encoding records and reader.
    /// </summary>
    /// <param name="encodings">The encoding records that share this subtable.</param>
    /// <param name="reader">The binary reader positioned after the format field.</param>
    /// <returns>An enumerable of <see cref="Format12SubTable"/> instances, one per encoding record.</returns>
    public static IEnumerable<Format12SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
    {
        // 'cmap' Subtable Format 4:
        // Type               | Name              | Description
        // -------------------|-------------------|------------------------------------------------------------------------
        // uint16             | format            | Subtable format; set to 12.
        // uint16             | reserved          | Reserved; set to 0
        // uint32             | length            | Byte length of this subtable(including the header)
        // uint32             | language          | For requirements on use of the language field, see "Use of the language field in 'cmap' subtables" in this document.
        // uint32             | numGroups         | Number of groupings which follow
        // SequentialMapGroup | groups[numGroups] | Array of SequentialMapGroup records.

        // format has already been read by this point skip it
        ushort reserved = reader.ReadUInt16();
        uint length = reader.ReadUInt32();
        uint language = reader.ReadUInt32();
        uint numGroups = reader.ReadUInt32();

        var groups = new SequentialMapGroup[numGroups];
        for (var i = 0; i < numGroups; i++)
        {
            groups[i] = SequentialMapGroup.Load(reader);
        }

        foreach (EncodingRecord encoding in encodings)
        {
            yield return new Format12SubTable(language, encoding.PlatformID, encoding.EncodingID, groups);
        }
    }

    /// <summary>
    /// Represents a sequential map group record that maps a contiguous range of character codes
    /// to a contiguous range of glyph indices.
    /// </summary>
    internal readonly struct SequentialMapGroup
    {
        /// <summary>
        /// The first character code in this group.
        /// </summary>
        public readonly uint StartCodePoint;

        /// <summary>
        /// The last character code in this group (inclusive).
        /// </summary>
        public readonly uint EndCodePoint;

        /// <summary>
        /// The glyph index corresponding to the starting character code.
        /// </summary>
        public readonly uint StartGlyphId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequentialMapGroup"/> struct.
        /// </summary>
        /// <param name="startCodePoint">The first character code in this group.</param>
        /// <param name="endCodePoint">The last character code in this group.</param>
        /// <param name="startGlyph">The glyph index corresponding to the starting character code.</param>
        public SequentialMapGroup(uint startCodePoint, uint endCodePoint, uint startGlyph)
        {
            this.StartCodePoint = startCodePoint;
            this.EndCodePoint = endCodePoint;
            this.StartGlyphId = startGlyph;
        }

        /// <summary>
        /// Loads a <see cref="SequentialMapGroup"/> from the specified reader.
        /// </summary>
        /// <param name="reader">The binary reader positioned at the sequential map group data.</param>
        /// <returns>The parsed <see cref="SequentialMapGroup"/>.</returns>
        public static SequentialMapGroup Load(BigEndianBinaryReader reader)
        {
            var startCodePoint = reader.ReadUInt32();
            var endCodePoint = reader.ReadUInt32();
            var startGlyph = reader.ReadUInt32();
            return new SequentialMapGroup(startCodePoint, endCodePoint, startGlyph);
        }
    }
}
