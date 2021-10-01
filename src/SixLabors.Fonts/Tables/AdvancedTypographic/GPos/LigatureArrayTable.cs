// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// The LigatureArray table contains a count (ligatureCount) and an array of offsets (ligatureAttachOffsets) to LigatureAttach tables.
    /// The ligatureAttachOffsets array lists the offsets to LigatureAttach tables, one for each ligature glyph listed in the ligatureCoverage table,
    /// in the same order as the ligatureCoverage index.
    /// </summary>
    internal class LigatureArrayTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LigatureArrayTable"/> class.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the start of the ligature array table.</param>
        /// <param name="markClassCount">Number of defined mark classes.</param>
        public LigatureArrayTable(BigEndianBinaryReader reader, long offset, ushort markClassCount)
        {
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            // | Type         | Name                   | Description                                                                          |
            // +==============+========================+======================================================================================+
            // | uint16       | ligatureCount          | Number of LigatureAttach table offsets                                               |
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            // | MarkRecord   | ligArray[markCount]    | Array of offsets to LigatureAttach tables. Offsets are from beginning of             |
            // |              |                        | LigatureArray table, ordered by ligatureCoverage index.                              |
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            reader.Seek(offset, SeekOrigin.Begin);
            ushort ligatureCount = reader.ReadUInt16();
            this.LigatureAttachTables = new LigatureAttachTable[ligatureCount];
            for (int i = 0; i < ligatureCount; i++)
            {
                this.LigatureAttachTables[i] = new LigatureAttachTable(reader, markClassCount, offset);
            }
        }

        public LigatureAttachTable[] LigatureAttachTables { get; }
    }
}
