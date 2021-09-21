// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// The MarkArray table defines the class and the anchor point for a mark glyph.
    /// Three GPOS subtable types — MarkToBase attachment, MarkToLigature attachment,
    /// and MarkToMark attachment — use the MarkArray table to specify data for attaching marks.
    /// The MarkArray table contains a count of the number of MarkRecords(markCount) and an array of those records(markRecords).
    /// Each mark record defines the class of the mark and an offset to the Anchor table that contains data for the mark.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#mark-array-table"/>
    /// </summary>
    internal class MarkArrayTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkArrayTable"/> class.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the start of the mark array table.</param>
        public MarkArrayTable(BigEndianBinaryReader reader, long offset)
        {
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            // | Type         | Name                   | Description                                                                          |
            // +==============+========================+======================================================================================+
            // | uint16       | markCount              | Number of MarkRecords                                                                |
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            // | MarkRecord   | markRecords[markCount] | Array of MarkRecords, ordered by corresponding glyphs                                |
            // |              |                        | in the associated mark Coverage table.                                               |
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            ushort markCount = reader.ReadUInt16();
            this.MarkRecords = new MarkRecord[markCount];
            for (int i = 0; i < markCount; i++)
            {
                this.MarkRecords[i] = new MarkRecord(reader, offset);
            }
        }

        /// <summary>
        /// Gets the mark records.
        /// </summary>
        public MarkRecord[] MarkRecords { get; }
    }
}
