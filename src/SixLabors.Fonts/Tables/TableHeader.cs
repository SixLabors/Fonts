using System;
using System.Text;

namespace SixLabors.Fonts.Tables
{
    internal class TableHeader
    {
        public TableHeader(string tag, uint checkSum, uint? offset, uint len)
        {
            this.Tag = tag;
            this.CheckSum = checkSum;
            this.Offset = offset;
            this.Length = len;
        }

        public string Tag { get; }

        public uint? Offset { get; }

        public uint CheckSum { get; }

        public uint Length { get; }

        public static TableHeader Read(BinaryReader reader)
        {
            return new TableHeader(
                reader.ReadUint32String(),
                reader.ReadUInt32(),
                reader.ReadOffset32(),
                reader.ReadUInt32());
        }
    }
}