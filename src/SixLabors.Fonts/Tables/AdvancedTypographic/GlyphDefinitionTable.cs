// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;

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
    internal const string TableName = "GDEF";

    public ClassDefinitionTable? GlyphClassDefinition { get; private set; }

    public AttachmentListTable? AttachmentListTable { get; private set; }

    public LigatureCaretList? LigatureCaretList { get; private set; }

    public ClassDefinitionTable? MarkAttachmentClassDef { get; private set; }

    public MarkGlyphSetsTable? MarkGlyphSetsTable { get; private set; }

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
        ushort ligatureCaretListOffset = reader.ReadUInt16();
        ushort markAttachClassDefOffset = reader.ReadUInt16();
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

        ClassDefinitionTable? classDefinitionTable = glyphClassDefOffset is 0 ? null : ClassDefinitionTable.Load(reader, glyphClassDefOffset);
        AttachmentListTable? attachmentListTable = attachListOffset is 0 ? null : AttachmentListTable.Load(reader, attachListOffset);
        LigatureCaretList? ligatureCaretList = ligatureCaretListOffset is 0 ? null : LigatureCaretList.Load(reader, ligatureCaretListOffset);
        ClassDefinitionTable? markAttachmentClassDef = markAttachClassDefOffset is 0 ? null : ClassDefinitionTable.Load(reader, markAttachClassDefOffset);
        MarkGlyphSetsTable? markGlyphSetsTable = markGlyphSetsDefOffset is 0 ? null : MarkGlyphSetsTable.Load(reader, markGlyphSetsDefOffset);

        var glyphDefinitionTable = new GlyphDefinitionTable()
        {
            GlyphClassDefinition = classDefinitionTable,
            AttachmentListTable = attachmentListTable,
            LigatureCaretList = ligatureCaretList,
            MarkAttachmentClassDef = markAttachmentClassDef,
            MarkGlyphSetsTable = markGlyphSetsTable
        };

        // TODO: read itemVarStore.
        return glyphDefinitionTable;
    }
}
