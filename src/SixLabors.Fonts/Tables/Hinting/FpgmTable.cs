// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Hinting

{
    [TableName(TableName)]
    internal class FpgmTable : Table
    {
        internal const string TableName = "fpgm";

        public FpgmTable(byte[] instructions)
        {
            this.Instructions = instructions;
        }

        public byte[] Instructions { get; }

        public static FpgmTable? Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader, out TableHeader? header))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader, header.Length);
            }
        }

        public static FpgmTable Load(BigEndianBinaryReader reader, uint tableLength)
        {
            // HEADER

            // Type     | Description
            // ---------| ------------
            // uint8[n] | Instructions. n is the number of uint8 items that fit in the size of the table.

            var instructions = reader.ReadUInt8Array((int)tableLength);

            return new FpgmTable(instructions);
        }
    }
}
