// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// A Mark2Record declares one Anchor table for each mark class (including Class 0) identified in the MarkRecords of the MarkArray.
    /// Each Anchor table specifies one mark2 attachment point used to attach all the mark1 glyphs in a particular class to the mark2 glyph.
    /// </summary>
    internal class Mark2Record
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mark2Record"/> class.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="markClassCount">The Number of Mark2 records.</param>
        /// <param name="offset">Offset to the beginning of MarkArray table.</param>
        public Mark2Record(BigEndianBinaryReader reader, ushort markClassCount, long offset)
        {
            // +--------------+------------------------------------+--------------------------------------------------------------------------------------+
            // | Type         | Name                               | Description                                                                          |
            // +==============+====================================+======================================================================================+
            // | Offset16     | mark2AnchorOffsets[markClassCount] | Array of offsets (one per class) to Anchor tables. Offsets are from beginning of     |
            // |              |                                    | Mark2Array table, in class order (offsets may be NULL).                              |
            // +--------------+------------------------------------+--------------------------------------------------------------------------------------+
            ushort[] mark2AnchorOffsets = new ushort[markClassCount];
            this.MarkAnchorTable = new AnchorTable[markClassCount];
            for (int i = 0; i < markClassCount; i++)
            {
                mark2AnchorOffsets[i] = reader.ReadOffset16();
            }

            for (int i = 0; i < markClassCount; i++)
            {
                if (mark2AnchorOffsets[i] != 0)
                {
                    this.MarkAnchorTable[i] = AnchorTable.Load(reader, offset + mark2AnchorOffsets[i]);
                }
            }
        }

        /// <summary>
        /// Gets the mark anchor table.
        /// </summary>
        public AnchorTable[] MarkAnchorTable { get; }
    }
}
