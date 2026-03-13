// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Represents the BaseArray table used in MarkToBase attachment positioning (GPOS LookupType 4).
/// The BaseArray table contains an array of BaseRecords, one for each base glyph, ordered by the base Coverage index.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-to-base-attachment-positioning-format-1-mark-to-base-attachment-point"/>
/// </summary>
internal class BaseArrayTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseArrayTable"/> class.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the base array table.</param>
    /// <param name="classCount">The class count.</param>
    public BaseArrayTable(BigEndianBinaryReader reader, long offset, ushort classCount)
    {
        // +--------------+------------------------+--------------------------------------------------------------------------------------+
        // | Type         | Name                   | Description                                                                          |
        // +==============+========================+======================================================================================+
        // | uint16       | baseCount              | Number of BaseRecords                                                                |
        // +--------------+------------------------+--------------------------------------------------------------------------------------+
        // | BaseRecord   | baseRecords[baseCount] | Array of BaseRecords, in order of baseCoverage Index.                                |
        // +--------------+------------------------+--------------------------------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort baseCount = reader.ReadUInt16();
        this.BaseRecords = new BaseRecord[baseCount];
        for (int i = 0; i < baseCount; i++)
        {
            this.BaseRecords[i] = new BaseRecord(reader, classCount, offset);
        }
    }

    /// <summary>
    /// Gets the base records.
    /// </summary>
    public BaseRecord[] BaseRecords { get; }
}
