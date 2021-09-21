// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal class Mark2ArrayTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mark2ArrayTable"/> class.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to the start of the mark array table.</param>
        /// <param name="markClassCount">The number of mark classes.</param>
        public Mark2ArrayTable(BigEndianBinaryReader reader, long offset, ushort markClassCount)
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
                this.Mark2Records[i] = new Mark2Record(reader, markClassCount);
            }
        }

        public Mark2Record[] Mark2Records { get; }
    }
}
