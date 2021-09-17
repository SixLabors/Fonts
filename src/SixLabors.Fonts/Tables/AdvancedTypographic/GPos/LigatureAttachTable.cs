// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// Each LigatureAttach table consists of an array (componentRecords) and count (componentCount) of the component glyphs in a ligature.
    /// The array stores the ComponentRecords in the same order as the components in the ligature.
    /// The order of the records also corresponds to the writing direction — that is, the logical direction — of the text.
    /// For text written left to right, the first component is on the left; for text written right to left, the first component is on the right.
    /// </summary>
    internal class LigatureAttachTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LigatureAttachTable"/> class.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="markClassCount">Number of defined mark classes.</param>
        public LigatureAttachTable(BigEndianBinaryReader reader, ushort markClassCount)
        {
            // +-------------------+---------------------------------+--------------------------------------------------------------------------------------+
            // | Type              | Name                            | Description                                                                          |
            // +===================+=================================+======================================================================================+
            // | uint16            | componentCount                  | Number of ComponentRecords in this ligature.                                         |
            // +-------------------+---------------------------------+--------------------------------------------------------------------------------------+
            // | ComponentRecords  | componentRecords[componentCount]| Array of Component records, ordered in writing direction.                            |
            // +-------------------+---------------------------------+--------------------------------------------------------------------------------------+
            ushort componentCount = reader.ReadUInt16();
            this.ComponentRecords = new ComponentRecord[componentCount];
            for (int i = 0; i < componentCount; i++)
            {
                this.ComponentRecords[i] = new ComponentRecord(reader, markClassCount);
            }
        }

        public ComponentRecord[] ComponentRecords { get; }
    }
}
