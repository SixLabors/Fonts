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
        public Mark2Record(BigEndianBinaryReader reader, ushort markClassCount)
        {
            // +--------------+------------------------------------+--------------------------------------------------------------------------------------+
            // | Type         | Name                               | Description                                                                          |
            // +==============+====================================+======================================================================================+
            // | Offset16     | mark2AnchorOffsets[markClassCount] | Array of offsets (one per class) to Anchor tables. Offsets are from beginning of     |
            // |              |                                    | Mark2Array table, in class order (offsets may be NULL).                              |
            // +--------------+------------------------------------+--------------------------------------------------------------------------------------+
            this.Mark2AnchorOffsets = new ushort[markClassCount];
            for (int i = 0; i < markClassCount; i++)
            {
                this.Mark2AnchorOffsets[i] = reader.ReadOffset16();
            }
        }

        public ushort[] Mark2AnchorOffsets { get; }
    }
}
