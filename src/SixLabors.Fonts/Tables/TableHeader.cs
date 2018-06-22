// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

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

        public static TableHeader Read(BinaryReader reader)
        {
            return new TableHeader(
                reader.ReadTag(),
                reader.ReadUInt32(),
                reader.ReadOffset32(),
                reader.ReadUInt32());
        }

        public virtual BinaryReader CreateReader(Stream stream)
        {
            stream.Seek(this.Offset, SeekOrigin.Begin);

            return new BinaryReader(stream, true);
        }
    }
}