// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal readonly struct BaseRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRecord"/> struct.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="classCount">The class count.</param>
        /// <param name="offset">Offset to the from beginning of BaseArray table.</param>
        public BaseRecord(BigEndianBinaryReader reader, ushort classCount, long offset)
        {
            // +--------------+-----------------------------------+----------------------------------------------------------------------------------------+
            // | Type         | Name                              | Description                                                                            |
            // +==============+===================================+========================================================================================+
            // | Offset16     | baseAnchorOffsets[markClassCount] | Array of offsets (one per mark class) to Anchor tables.                                |
            // |              |                                   | Offsets are from beginning of BaseArray table, ordered by class (offsets may be NULL). |
            // +--------------+-----------------------------------+----------------------------------------------------------------------------------------+
            this.BaseAnchorTables = new AnchorTable[classCount];
            for (int i = 0; i < classCount; i++)
            {
                ushort baseAnchorOffset = reader.ReadOffset16();
                this.BaseAnchorTables[i] = AnchorTable.Load(reader, offset + baseAnchorOffset);
            }
        }

        /// <summary>
        /// Gets the base anchor tables.
        /// </summary>
        public AnchorTable[] BaseAnchorTables { get; }
    }
}
