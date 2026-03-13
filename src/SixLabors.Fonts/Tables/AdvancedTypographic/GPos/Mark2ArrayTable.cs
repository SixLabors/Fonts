// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos;

/// <summary>
/// Represents the Mark2Array table used in MarkToMark attachment positioning (GPOS LookupType 6).
/// The Mark2Array table contains an array of Mark2Records, one for each mark2 glyph (the base mark), ordered by the mark2 Coverage index.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/gpos#mark-to-mark-attachment-positioning-format-1-mark-to-mark-attachment"/>
/// </summary>
internal class Mark2ArrayTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Mark2ArrayTable"/> class.
    /// </summary>
    /// <param name="reader">The big endian binary reader.</param>
    /// <param name="markClassCount">The number of mark classes.</param>
    /// <param name="offset">The offset to the start of the mark array table.</param>
    public Mark2ArrayTable(BigEndianBinaryReader reader, ushort markClassCount, long offset)
    {
        // +--------------+------------------------+--------------------------------------------------------------------------------------+
        // | Type         | Name                   | Description                                                                          |
        // +==============+========================+======================================================================================+
        // | uint16       | mark2Count             | Number of Mark2Records.                                                              |
        // +--------------+------------------------+--------------------------------------------------------------------------------------+
        // | Mark2Record  | mark2Records[markCount]| Array of Mark2Records, in Coverage order.                                            |
        // +--------------+------------------------+--------------------------------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);
        ushort markCount = reader.ReadUInt16();
        this.Mark2Records = new Mark2Record[markCount];
        for (int i = 0; i < markCount; i++)
        {
            this.Mark2Records[i] = new Mark2Record(reader, markClassCount, offset);
        }
    }

    /// <summary>
    /// Gets the array of Mark2 records, in Coverage order.
    /// </summary>
    public Mark2Record[] Mark2Records { get; }
}
