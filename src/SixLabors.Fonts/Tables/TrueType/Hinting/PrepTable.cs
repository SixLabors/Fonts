// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Hinting
{
    internal class PrepTable : Table
    {
        internal const string TableName = "prep";

        public PrepTable(byte[] instructions) => this.Instructions = instructions;

        public byte[] Instructions { get; }

        public static PrepTable? Load(FontReader fontReader)
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

        public static PrepTable Load(BigEndianBinaryReader reader, uint tableLength)
        {
            // HEADER

            // Type     | Description
            // ---------| ------------
            // uint8[n] | Set of instructions executed whenever point size or font or transformation change. n is the number of uint8 items that fit in the size of the table.
            byte[]? instructions = reader.ReadUInt8Array((int)tableLength);

            return new PrepTable(instructions);
        }
    }
}
