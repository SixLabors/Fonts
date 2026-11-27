// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// The GDEF table contains three kinds of information in subtables:
/// 1. glyph class definitions that classify different types of glyphs in a font;
/// 2. attachment point lists that identify glyph positioning attachments for each glyph;
/// and 3. ligature caret lists that provide information for caret positioning and text selection involving ligatures.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gdef"/>
/// </summary>
internal sealed class GlyphDefinitionTable : Table
{
    /// <summary>
    /// The OpenType table tag for the GDEF table.
    /// </summary>
    internal const string TableName = "GDEF";

    /// <summary>
    /// Gets the class definition table for glyph types (base, ligature, mark, component).
    /// </summary>
    public ClassDefinitionTable? GlyphClassDefinition { get; private set; }

    /// <summary>
    /// Gets the attachment point list table for identifying glyph attachment points.
    /// </summary>
    public AttachmentListTable? AttachmentListTable { get; private set; }

    /// <summary>
    /// Gets the ligature caret list table for positioning carets within ligatures.
    /// </summary>
    public LigatureCaretList? LigatureCaretList { get; private set; }

    /// <summary>
    /// Gets the class definition table for mark attachment types.
    /// </summary>
    public ClassDefinitionTable? MarkAttachmentClassDef { get; private set; }

    /// <summary>
    /// Gets the mark glyph sets table for filtering marks during lookup processing.
    /// </summary>
    public MarkGlyphSetsTable? MarkGlyphSetsTable { get; private set; }

    /// <summary>
    /// Gets the item variation store table for variable font GDEF variations.
    /// </summary>
    public ItemVariationStore? ItemVariationStore { get; private set; }

    /// <summary>
    /// Loads the <see cref="GlyphDefinitionTable"/> from the font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The glyph definition table, or <see langword="null"/> if it is absent or contains known incorrect glyph classes.</returns>
    public static GlyphDefinitionTable? Load(FontReader reader)
    {
        if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader, out TableHeader? header))
        {
            return null;
        }

        using (binaryReader)
        {
            return HasIncorrectGlyphClasses(reader, header.Length) ? null : Load(binaryReader);
        }
    }

    /// <summary>
    /// Tries to get the glyph class for the specified glyph.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="glyphClass">When this method returns, contains the glyph class if found.</param>
    /// <returns><see langword="true"/> if the glyph class was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetGlyphClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? glyphClass)
    {
        glyphClass = null;

        if (this.GlyphClassDefinition is null)
        {
            return false;
        }

        glyphClass = (GlyphClassDef)this.GlyphClassDefinition.ClassIndexOf(glyphId);
        return true;
    }

    /// <summary>
    /// Tries to get the mark attachment class for the specified glyph.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="markAttachmentClass">When this method returns, contains the mark attachment class if found.</param>
    /// <returns><see langword="true"/> if the mark attachment class was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetMarkAttachmentClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? markAttachmentClass)
    {
        markAttachmentClass = null;

        if (this.MarkAttachmentClassDef is null)
        {
            return false;
        }

        markAttachmentClass = (GlyphClassDef)this.MarkAttachmentClassDef.ClassIndexOf(glyphId);
        return true;
    }

    /// <summary>
    /// Determines whether the specified glyph belongs to the given mark glyph set.
    /// </summary>
    /// <param name="markGlyphSetIndex">The index of the mark glyph set.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns><see langword="true"/> if the glyph is in the set; otherwise, <see langword="false"/>.</returns>
    public bool IsInMarkGlyphSet(ushort markGlyphSetIndex, ushort glyphId)
        => this.MarkGlyphSetsTable?.Contains(markGlyphSetIndex, glyphId) == true;

    /// <summary>
    /// Loads the <see cref="GlyphDefinitionTable"/> from a big endian binary reader.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="GlyphDefinitionTable"/>.</returns>
    public static GlyphDefinitionTable Load(BigEndianBinaryReader reader)
    {
        // Header version 1.0
        // Type      | Name                     | Description
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | majorVersion             | Major version of the GDEF table, = 1
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | minorVersion             | Minor version of the GDEF table, = 0
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | glyphClassDefOffset      | Offset to class definition table for glyph type, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | attachListOffset         | Offset to attachment point list table, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | ligCaretListOffset       | Offset to ligature caret list table, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | markAttachClassDefOffset | Offset to class definition table for mark attachment type, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------

        // Header version 1.2
        // Type      | Name                     | Description
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | majorVersion             | Major version of the GDEF table, = 1
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | minorVersion             | Minor version of the GDEF table, = 0
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | glyphClassDefOffset      | Offset to class definition table for glyph type, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | attachListOffset         | Offset to attachment point list table, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | ligCaretListOffset       | Offset to ligature caret list table, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | markAttachClassDefOffset | Offset to class definition table for mark attachment type, from beginning of GDEF header (may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | markGlyphSetsDefOffset   | Offset to the table of mark glyph set definitions, from beginning of GDEF header (may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------

        // Header version 1.3
        // Type      | Name                     | Description
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | majorVersion             | Major version of the GDEF table, = 1
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // uint16    | minorVersion             | Minor version of the GDEF table, = 0
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | glyphClassDefOffset      | Offset to class definition table for glyph type, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | attachListOffset         | Offset to attachment point list table, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | ligCaretListOffset       | Offset to ligature caret list table, from beginning of GDEF header(may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | markAttachClassDefOffset | Offset to class definition table for mark attachment type, from beginning of GDEF header (may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset16  | markGlyphSetsDefOffset   | Offset to the table of mark glyph set definitions, from beginning of GDEF header (may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        // Offset32  | itemVarStoreOffset       | Offset to the Item Variation Store table, from beginning of GDEF header (may be NULL).
        // ----------|--------------------------|--------------------------------------------------------------------------------------------------------
        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();

        ushort glyphClassDefOffset = reader.ReadUInt16();
        ushort attachListOffset = reader.ReadUInt16();
        ushort ligatureCaretListOffset = reader.ReadOffset16();
        ushort markAttachClassDefOffset = reader.ReadOffset16();
        ushort markGlyphSetsDefOffset = 0;
        uint itemVarStoreOffset = 0;

        switch (minorVersion)
        {
            case 0:
                break;
            case 2:
                markGlyphSetsDefOffset = reader.ReadUInt16();
                break;
            case 3:
                markGlyphSetsDefOffset = reader.ReadUInt16();
                itemVarStoreOffset = reader.ReadUInt32();
                break;
            default:
                throw new InvalidFontFileException($"Invalid value for 'minor version' {minorVersion} of GDEF table. Should be '0', '2' or '3'.");
        }

        ClassDefinitionTable.TryLoad(reader, glyphClassDefOffset, out ClassDefinitionTable? classDefinitionTable);
        AttachmentListTable? attachmentListTable = attachListOffset is 0 ? null : AttachmentListTable.Load(reader, attachListOffset);
        LigatureCaretList? ligatureCaretList = ligatureCaretListOffset is 0 ? null : LigatureCaretList.Load(reader, ligatureCaretListOffset);
        ClassDefinitionTable.TryLoad(reader, markAttachClassDefOffset, out ClassDefinitionTable? markAttachmentClassDef);
        MarkGlyphSetsTable? markGlyphSetsTable = markGlyphSetsDefOffset is 0 ? null : MarkGlyphSetsTable.Load(reader, markGlyphSetsDefOffset);

        ItemVariationStore? itemVariationStore = null;
        if (itemVarStoreOffset != 0)
        {
            itemVariationStore = ItemVariationStore.Load(reader, itemVarStoreOffset);
        }

        return new GlyphDefinitionTable()
        {
            GlyphClassDefinition = classDefinitionTable,
            AttachmentListTable = attachmentListTable,
            LigatureCaretList = ligatureCaretList,
            MarkAttachmentClassDef = markAttachmentClassDef,
            MarkGlyphSetsTable = markGlyphSetsTable,
            ItemVariationStore = itemVariationStore
        };
    }

    /// <summary>
    /// Identifies known fonts that mark spacing glyphs as combining marks in GDEF.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <param name="definitionLength">The length of GDEF in bytes.</param>
    /// <returns>Whether shaping must use Unicode glyph classes for this font.</returns>
    private static bool HasIncorrectGlyphClasses(FontReader reader, uint definitionLength)
    {
        uint substitutionLength = reader.GetHeader(GSubTable.TableName)?.Length ?? 0;
        uint positioningLength = reader.GetHeader(GPosTable.TableName)?.Length ?? 0;

        // A false mark class can cause shaping to set a spacing glyph's advance
        // to zero. Match the GDEF, GSUB, and GPOS lengths together, as HarfBuzz does.
        // A length tuple identifies known releases but cannot prove file identity.
        return (definitionLength, substitutionLength, positioningLength) switch
        {
            // Times New Roman Italic and Bold Italic from Windows 7 and OS X 10.11.3.
            // GDEF classifies the straight double quote (U+0022) as a mark.
            // https://lists.freedesktop.org/archives/harfbuzz/2016-February/005489.html
            (442, 2874, 42038) or
            (430, 2874, 40662) or
            (442, 2874, 39116) or
            (430, 2874, 39374) or
            (490, 3046, 41638) or
            (478, 3046, 41902) => true,

            // Tahoma regular and bold from Windows 8, 8.1, and 10, and OS X 10.9.
            // Some spacing IPA letters have the mark class and overlap the next letter.
            // https://bugzilla.mozilla.org/show_bug.cgi?id=1279925
            (898, 12554, 46470) or
            (910, 12566, 47732) or
            (928, 23298, 59332) or
            (940, 23310, 60732) or
            (964, 23836, 60072) or
            (976, 23832, 61456) or
            (994, 24474, 60336) or
            (1006, 24470, 61740) or
            (1006, 24576, 61346) or
            (1018, 24572, 62828) or
            (1006, 24576, 61352) or
            (1018, 24572, 62834) or
            (832, 7324, 47162) or
            (844, 7302, 45474) => true,

            // Microsoft Himalaya from Windows 7, 8, and 8.1.
            // Incorrect mark classes affect the spacing of Tibetan text.
            // https://bugzilla.mozilla.org/show_bug.cgi?id=1279693
            (180, 13054, 7254) or
            (192, 12638, 7254) or
            (192, 12690, 7254) => true,

            // Cantarell 0.0.21 regular, oblique, bold, and bold oblique.
            // Affected accented letters, including Ü, overlap the next letter.
            // https://bugzilla.mozilla.org/show_bug.cgi?id=1279875
            (188, 248, 3852) or
            (188, 264, 3426) => true,

            // Padauk regular, bold, and book variants from versions 2.5, 2.80, and 3.0.
            // Chromium report for Padauk 2.5.
            // https://issues.chromium.org/issues/41295412
            (1058, 47032, 11818) or
            (1046, 47030, 12600) or
            (1058, 71796, 16770) or
            (1046, 71790, 17862) or
            (1046, 71788, 17112) or
            (1058, 71794, 17514) or
            (1330, 109904, 57938) or
            (1330, 109904, 58972) or
            (1004, 59092, 14836) => true,

            // Courier New regular and bold from macOS 15, Windows 10, and Windows 8.1.
            // GDEF classifies some spacing Latin letters as marks.
            // https://github.com/harfbuzz/harfbuzz/issues/4936
            (588, 5078, 14418) or
            (588, 5078, 14238) or
            (894, 17162, 33960) or
            (894, 17154, 34472) or
            (816, 7868, 17052) or
            (816, 7868, 17138) => true,

            _ => false
        };
    }
}
