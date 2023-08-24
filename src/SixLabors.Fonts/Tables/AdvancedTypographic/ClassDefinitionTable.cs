// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// In OpenType Layout, index values identify glyphs. For efficiency and ease of representation, a font developer
/// can group glyph indices to form glyph classes. Class assignments vary in meaning from one lookup subtable
/// to another. For example, in the GSUB and GPOS tables, classes are used to describe glyph contexts.
/// GDEF tables also use the idea of glyph classes.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#class-definition-table"/>
/// </summary>
internal abstract class ClassDefinitionTable
{
    /// <summary>
    /// Gets the class id for the given glyph id.
    /// Any glyph not included in the range of covered glyph IDs automatically belongs to Class 0.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>The class id.</returns>
    public abstract int ClassIndexOf(ushort glyphId);

    public static ClassDefinitionTable Load(BigEndianBinaryReader reader, long offset)
    {
        reader.Seek(offset, SeekOrigin.Begin);
        ushort classFormat = reader.ReadUInt16();
        return classFormat switch
        {
            1 => ClassDefinitionFormat1Table.Load(reader),
            2 => ClassDefinitionFormat2Table.Load(reader),
            _ => throw new InvalidFontFileException($"Invalid value for 'classFormat' {classFormat}. Should be '1' or '2'.")
        };
    }
}

internal sealed class ClassDefinitionFormat1Table : ClassDefinitionTable
{
    private readonly ushort startGlyphId;
    private readonly ushort[] classValueArray;

    private ClassDefinitionFormat1Table(ushort startGlyphId, ushort[] classValueArray)
    {
        this.startGlyphId = startGlyphId;
        this.classValueArray = classValueArray;
    }

    public static ClassDefinitionFormat1Table Load(BigEndianBinaryReader reader)
    {
        // +--------+-----------------------------+------------------------------------------+
        // | Type   | Name                        | Description                              |
        // +========+=============================+==========================================+
        // | uint16 | classFormat                 | Format identifier — format = 1           |
        // +--------+-----------------------------+------------------------------------------+
        // | uint16 | startGlyphID                | First glyph ID of the classValueArray    |
        // +--------+-----------------------------+------------------------------------------+
        // | uint16 | glyphCount                  | Size of the classValueArray              |
        // +--------+-----------------------------+------------------------------------------+
        // | uint16 | classValueArray[glyphCount] | Array of Class Values — one per glyph ID |
        // +--------+-----------------------------+------------------------------------------+
        ushort startGlyphId = reader.ReadUInt16();
        ushort glyphCount = reader.ReadUInt16();
        ushort[] classValueArray = reader.ReadUInt16Array(glyphCount);
        return new ClassDefinitionFormat1Table(startGlyphId, classValueArray);
    }

    /// <inheritdoc />
    public override int ClassIndexOf(ushort glyphId)
    {
        int i = glyphId - this.startGlyphId;
        if (i >= 0 && i < this.classValueArray.Length)
        {
            return this.classValueArray[i];
        }

        // Any glyph not included in the range of covered glyph IDs automatically belongs to Class 0.
        return 0;
    }
}

internal sealed class ClassDefinitionFormat2Table : ClassDefinitionTable
{
    private readonly ClassRangeRecord[] records;

    private ClassDefinitionFormat2Table(ClassRangeRecord[] records)
        => this.records = records;

    public static ClassDefinitionFormat2Table Load(BigEndianBinaryReader reader)
    {
        // +------------------+------------------------------------+-----------------------------------------+
        // | Type             | Name                               | Description                             |
        // +==================+====================================+=========================================+
        // | uint16           | classFormat                        | Format identifier — format = 2          |
        // +------------------+------------------------------------+-----------------------------------------+
        // | uint16           | classRangeCount                    | Number of ClassRangeRecords             |
        // +------------------+------------------------------------+-----------------------------------------+
        // | ClassRangeRecord | classRangeRecords[classRangeCount] | Array of ClassRangeRecords — ordered by |
        // |                  |                                    | startGlyphID                            |
        // +------------------+------------------------------------+-----------------------------------------+
        ushort classRangeCount = reader.ReadUInt16();
        var records = new ClassRangeRecord[classRangeCount];
        for (int i = 0; i < records.Length; ++i)
        {
            // +--------+--------------+------------------------------------+
            // | Type   | Name         | Description                        |
            // +========+==============+====================================+
            // | uint16 | startGlyphID | First glyph ID in the range        |
            // +--------+--------------+------------------------------------+
            // | uint16 | endGlyphID   | Last glyph ID in the range         |
            // +--------+--------------+------------------------------------+
            // | uint16 | class        | Applied to all glyphs in the range |
            // +--------+--------------+------------------------------------+
            records[i] = new ClassRangeRecord(
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16());
        }

        return new ClassDefinitionFormat2Table(records);
    }

    /// <inheritdoc />
    public override int ClassIndexOf(ushort glyphId)
    {
        for (int i = 0; i < this.records.Length; i++)
        {
            ClassRangeRecord rec = this.records[i];
            if (rec.StartGlyphId <= glyphId && glyphId <= rec.EndGlyphId)
            {
                return rec.Class;
            }
        }

        // Any glyph not included in the range of covered glyph IDs automatically belongs to Class 0.
        return 0;
    }
}
