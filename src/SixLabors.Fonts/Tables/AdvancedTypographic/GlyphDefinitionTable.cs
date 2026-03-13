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
    /// <returns>The <see cref="GlyphDefinitionTable"/>, or <see langword="null"/> if not present.</returns>
    public static GlyphDefinitionTable? Load(FontReader reader)
    {
        if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
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
}
