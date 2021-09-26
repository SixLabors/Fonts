// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Text;

namespace SixLabors.Fonts.Utilities
{
    [DebuggerDisplay("Offset: {Offset}, Length: {Length}, Value: {Value}")]
    internal class StringLoader
    {
        public StringLoader(ushort length, ushort offset, Encoding encoding)
        {
            this.Length = length;
            this.Offset = offset;
            this.Encoding = encoding;
            this.Value = string.Empty;
        }

        public ushort Length { get; }

        public ushort Offset { get; }

        public string Value { get; private set; }

        public Encoding Encoding { get; }

        public static StringLoader Create(BigEndianBinaryReader reader)
            => Create(reader, Encoding.BigEndianUnicode);

        public static StringLoader Create(BigEndianBinaryReader reader, Encoding encoding)
            => new(reader.ReadUInt16(), reader.ReadUInt16(), encoding);

        public void LoadValue(BigEndianBinaryReader reader)
            => this.Value = reader.ReadString(this.Length, this.Encoding).Replace("\0", string.Empty);
    }
}
