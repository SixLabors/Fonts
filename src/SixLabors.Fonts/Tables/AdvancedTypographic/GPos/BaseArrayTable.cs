// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal class BaseArrayTable
    {
        public BaseArrayTable(BigEndianBinaryReader reader, long offset, ushort classCount)
        {
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            // | Type         | Name                   | Description                                                                          |
            // +==============+========================+======================================================================================+
            // | uint16       | baseCount              | Number of BaseRecords                                                                |
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            // | BaseRecord   | baseRecords[baseCount] | Array of BaseRecords, in order of baseCoverage Index.                                |
            // +--------------+------------------------+--------------------------------------------------------------------------------------+
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            ushort baseCount = reader.ReadUInt16();
            this.BaseRecords = new BaseRecord[baseCount];
            for (int i = 0; i < baseCount; i++)
            {
                this.BaseRecords[i] = new BaseRecord(reader, classCount);
            }
        }

        public BaseRecord[] BaseRecords { get; }
    }
}
