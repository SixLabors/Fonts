// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// In a ComponentRecord, the zero-based ligatureAnchorOffsets array lists offsets to Anchor tables by mark class.
    /// If a component does not define an attachment point for a particular class of marks, then the offset to the corresponding Anchor table will be NULL.
    /// Example 8 at the end of this chapter shows a MarkLigPosFormat1 subtable used to attach mark accents to a ligature glyph in the Arabic script.
    /// </summary>
    internal class ComponentRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentRecord"/> class.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="markClassCount">Number of defined mark classes.</param>
        /// <param name="offset">Offset from beginning of LigatureAttach table.</param>
        public ComponentRecord(BigEndianBinaryReader reader, ushort markClassCount, long offset)
        {
            // +--------------+---------------------------------------+----------------------------------------------------------------------------------------+
            // | Type         | Name                                  | Description                                                                            |
            // +==============+=======================================+========================================================================================+
            // | Offset16     | ligatureAnchorOffsets[markClassCount] | Array of offsets (one per class) to Anchor tables. Offsets are from                    |
            // |              |                                       | beginning of LigatureAttach table, ordered by class (offsets may be NULL).             |
            // +--------------+---------------------------------------+----------------------------------------------------------------------------------------+
            this.LigatureAnchorTables = new AnchorTable[markClassCount];
            ushort[] ligatureAnchorOffsets = new ushort[markClassCount];
            for (int i = 0; i < markClassCount; i++)
            {
                ligatureAnchorOffsets[i] = reader.ReadOffset16();
            }

            long position = reader.BaseStream.Position;
            for (int i = 0; i < markClassCount; i++)
            {
                if (ligatureAnchorOffsets[i] is not 0)
                {
                    this.LigatureAnchorTables[i] = AnchorTable.Load(reader, offset + ligatureAnchorOffsets[i]);
                }
            }

            reader.BaseStream.Position = position;
        }

        public AnchorTable[] LigatureAnchorTables { get; }
    }
}
