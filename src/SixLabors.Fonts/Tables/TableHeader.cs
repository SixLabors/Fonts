// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.IO;

namespace SixLabors.Fonts.Tables
{
    internal class TableHeader
    {
        public TableHeader(string tag, uint checkSum, uint offset, uint len)
        {
            this.Tag = tag;
            this.CheckSum = checkSum;
            this.Offset = offset;
            this.Length = len;
        }

        public string Tag { get; }

        public uint Offset { get; }

        public uint CheckSum { get; }

        public uint Length { get; }

        public static TableHeader Read(BigEndianBinaryReader reader) => new TableHeader(
                reader.ReadTag(),
                reader.ReadUInt32(),
                reader.ReadOffset32(),
                reader.ReadUInt32());

        public virtual BigEndianBinaryReader CreateReader(Stream stream)
        {
            stream.Seek(this.Offset, SeekOrigin.Begin);

            return new BigEndianBinaryReader(stream, true);
        }
    }
}
