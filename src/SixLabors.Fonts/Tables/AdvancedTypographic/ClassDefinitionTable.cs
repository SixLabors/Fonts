// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;

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

    /// <summary>
    /// Tries to load a <see cref="ClassDefinitionTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the table. If 0, no table is loaded.</param>
    /// <param name="table">When this method returns, contains the loaded table if successful.</param>
    /// <returns><see langword="true"/> if the table was loaded; otherwise, <see langword="false"/>.</returns>
    public static bool TryLoad(BigEndianBinaryReader reader, long offset, [NotNullWhen(true)] out ClassDefinitionTable? table)
    {
        if (offset == 0)
        {
            table = null;
            return false;
        }

        reader.Seek(offset, SeekOrigin.Begin);
        ushort classFormat = reader.ReadUInt16();
        table = classFormat switch
        {
            1 => ClassDefinitionFormat1Table.Load(reader),
            2 => ClassDefinitionFormat2Table.Load(reader),
            _ => null
        };

        return table is not null;
    }

    /// <summary>
    /// Loads a <see cref="ClassDefinitionTable"/> from the binary reader at the specified offset.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">Offset from the beginning of the table.</param>
    /// <returns>The <see cref="ClassDefinitionTable"/>.</returns>
    /// <exception cref="InvalidFontFileException">Thrown when the class format is invalid.</exception>
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

/// <summary>
/// Class Definition Format 1: class assignment is defined by an array of class values
/// indexed by glyph ID minus a start glyph ID.
/// </summary>
internal sealed class ClassDefinitionFormat1Table : ClassDefinitionTable
{
    private readonly ushort startGlyphId;
    private readonly ushort[] classValueArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassDefinitionFormat1Table"/> class.
    /// </summary>
    /// <param name="startGlyphId">The first glyph ID of the class value array.</param>
    /// <param name="classValueArray">The array of class values, one per glyph ID.</param>
    private ClassDefinitionFormat1Table(ushort startGlyphId, ushort[] classValueArray)
    {
        this.startGlyphId = startGlyphId;
        this.classValueArray = classValueArray;
    }

    /// <summary>
    /// Loads a <see cref="ClassDefinitionFormat1Table"/> from the binary reader.
    /// The format identifier has already been read.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="ClassDefinitionFormat1Table"/>.</returns>
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

/// <summary>
/// Class Definition Format 2: class assignment is defined by an array of ranges,
/// each mapping a range of glyph IDs to a class value.
/// </summary>
internal sealed class ClassDefinitionFormat2Table : ClassDefinitionTable
{
    private readonly ClassRangeRecord[] records;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassDefinitionFormat2Table"/> class.
    /// </summary>
    /// <param name="records">The array of class range records.</param>
    private ClassDefinitionFormat2Table(ClassRangeRecord[] records)
        => this.records = records;

    /// <summary>
    /// Loads a <see cref="ClassDefinitionFormat2Table"/> from the binary reader.
    /// The format identifier has already been read.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <returns>The <see cref="ClassDefinitionFormat2Table"/>.</returns>
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
        ClassRangeRecord[] records = new ClassRangeRecord[classRangeCount];
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
        // Records are ordered by StartGlyphId, so use binary search to find the
        // candidate range whose StartGlyphId is <= glyphId.
        ClassRangeRecord[] records = this.records;
        int lo = 0;
        int hi = records.Length - 1;
        while (lo <= hi)
        {
            int mid = (int)(((uint)lo + (uint)hi) >> 1);
            ClassRangeRecord rec = records[mid];
            if (glyphId < rec.StartGlyphId)
            {
                hi = mid - 1;
            }
            else if (glyphId > rec.EndGlyphId)
            {
                lo = mid + 1;
            }
            else
            {
                return rec.Class;
            }
        }

        // Any glyph not included in the range of covered glyph IDs automatically belongs to Class 0.
        return 0;
    }
}
