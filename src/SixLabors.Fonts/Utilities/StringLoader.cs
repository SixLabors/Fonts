// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Text;

namespace SixLabors.Fonts.Utilities
{
    internal class StringLoader
    {
        public StringLoader(ushort length, ushort offset, Encoding encoding)
        {
            this.Length = length;
            this.Offset = offset;
            this.Encoding = encoding;
        }

        public ushort Length { get; }

        public ushort Offset { get; }

        public string Value { get; private set; }

        public Encoding Encoding { get; private set; }

        public static StringLoader Create(BinaryReader reader)
        {
            return Create(reader, Encoding.BigEndianUnicode);
        }

        public static StringLoader Create(BinaryReader reader, Encoding encoding)
        {
            return new StringLoader(reader.ReadUInt16(), reader.ReadUInt16(), encoding);
        }

        public void LoadValue(BinaryReader reader)
        {
            this.Value = reader.ReadString(this.Length, this.Encoding).Replace("\0", string.Empty);
        }
    }
}
