// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

/// <summary>
/// Format 13 maps ranges of character codes onto a single glyph each, rather than
/// onto a run of glyphs as format 12 does. Fonts that answer for characters they
/// have no artwork for use it, mapping every character of a range to one glyph
/// such as a blank or a last-resort shape.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cmap#format-13-many-to-one-range-mappings"/>
/// </summary>
internal sealed class Format13SubTable : CMapSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Format13SubTable"/> class.
    /// </summary>
    /// <param name="language">The language code for this subtable.</param>
    /// <param name="platform">The platform identifier.</param>
    /// <param name="encoding">The platform-specific encoding identifier.</param>
    /// <param name="groups">The array of constant map groups.</param>
    public Format13SubTable(uint language, PlatformIDs platform, ushort encoding, ConstantMapGroup[] groups)
        : base(platform, encoding, 13)
    {
        this.Language = language;
        this.ConstantMapGroups = groups;
    }

    /// <summary>
    /// Gets the array of constant map groups defining character-to-glyph mappings.
    /// </summary>
    public ConstantMapGroup[] ConstantMapGroups { get; }

    /// <summary>
    /// Gets the language code for this subtable.
    /// </summary>
    public uint Language { get; }

    /// <inheritdoc/>
    public override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
    {
        int charAsInt = codePoint.Value;

        for (int i = 0; i < this.ConstantMapGroups.Length; i++)
        {
            ref ConstantMapGroup group = ref this.ConstantMapGroups[i];

            if (charAsInt >= group.StartCodePoint && charAsInt <= group.EndCodePoint)
            {
                glyphId = (ushort)group.GlyphId;
                return true;
            }
        }

        glyphId = 0;
        return false;
    }

    /// <inheritdoc/>
    public override bool TryGetCodePoint(ushort glyphId, out CodePoint codePoint)
    {
        // The mapping is many to one, so a glyph names a whole range of
        // characters and the first of them is the only sensible answer.
        for (int i = 0; i < this.ConstantMapGroups.Length; i++)
        {
            ref ConstantMapGroup group = ref this.ConstantMapGroups[i];
            if (glyphId == group.GlyphId)
            {
                codePoint = new CodePoint((int)group.StartCodePoint);
                return true;
            }
        }

        codePoint = default;
        return false;
    }

    /// <inheritdoc/>
    public override IEnumerable<int> GetAvailableCodePoints()
        => this.ConstantMapGroups.SelectMany(group =>
        {
            int start = (int)group.StartCodePoint;
            int end = (int)group.EndCodePoint;
            return Enumerable.Range(start, end - start + 1);
        });

    /// <summary>
    /// Loads one or more <see cref="Format13SubTable"/> instances from the specified encoding records and reader.
    /// </summary>
    /// <param name="encodings">The encoding records that share this subtable.</param>
    /// <param name="reader">The binary reader positioned after the format field.</param>
    /// <returns>An enumerable of <see cref="Format13SubTable"/> instances, one per encoding record.</returns>
    public static IEnumerable<Format13SubTable> Load(IEnumerable<EncodingRecord> encodings, BigEndianBinaryReader reader)
    {
        // 'cmap' Subtable Format 13:
        // Type             | Name              | Description
        // -----------------|-------------------|------------------------------------------------------------------------
        // uint16           | format            | Subtable format; set to 13.
        // uint16           | reserved          | Reserved; set to 0
        // uint32           | length            | Byte length of this subtable (including the header)
        // uint32           | language          | For requirements on use of the language field, see "Use of the language field in 'cmap' subtables" in this document.
        // uint32           | numGroups         | Number of groupings which follow
        // ConstantMapGroup | groups[numGroups] | Array of ConstantMapGroup records.

        // format has already been read by this point skip it
        ushort reserved = reader.ReadUInt16();
        uint length = reader.ReadUInt32();
        uint language = reader.ReadUInt32();
        uint numGroups = reader.ReadUInt32();

        ConstantMapGroup[] groups = new ConstantMapGroup[numGroups];
        for (int i = 0; i < numGroups; i++)
        {
            groups[i] = ConstantMapGroup.Load(reader);
        }

        foreach (EncodingRecord encoding in encodings)
        {
            yield return new Format13SubTable(language, encoding.PlatformID, encoding.EncodingID, groups);
        }
    }

    /// <summary>
    /// Represents a constant map group record that maps a contiguous range of
    /// character codes to a single glyph index.
    /// </summary>
    internal readonly struct ConstantMapGroup
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
        /// The glyph index every character code in this group maps to.
        /// </summary>
        public readonly uint GlyphId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantMapGroup"/> struct.
        /// </summary>
        /// <param name="startCodePoint">The first character code in this group.</param>
        /// <param name="endCodePoint">The last character code in this group.</param>
        /// <param name="glyphId">The glyph index every character code in this group maps to.</param>
        public ConstantMapGroup(uint startCodePoint, uint endCodePoint, uint glyphId)
        {
            this.StartCodePoint = startCodePoint;
            this.EndCodePoint = endCodePoint;
            this.GlyphId = glyphId;
        }

        /// <summary>
        /// Loads a <see cref="ConstantMapGroup"/> from the specified reader.
        /// </summary>
        /// <param name="reader">The binary reader positioned at the constant map group data.</param>
        /// <returns>The parsed <see cref="ConstantMapGroup"/>.</returns>
        public static ConstantMapGroup Load(BigEndianBinaryReader reader)
        {
            uint startCodePoint = reader.ReadUInt32();
            uint endCodePoint = reader.ReadUInt32();
            uint glyphId = reader.ReadUInt32();
            return new ConstantMapGroup(startCodePoint, endCodePoint, glyphId);
        }
    }
}
