// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables
{
    public class TableHeaderTests
    {
        public static TheoryData<string, uint, uint?, uint> ReadAllValuesData { get; } =
            new TheoryData<string, uint, uint?, uint>
            {
                { "TAG1", 98, 18, 1218 },
                { "TAG2", 198, 0, 121 },
            };

        [Theory]
        [MemberData(nameof(ReadAllValuesData))]
        public void ReadAllValues(string tag, uint checksum, uint offset, uint length)
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteTableHeader(tag, checksum, offset, length);

            var header = TableHeader.Read(writer.GetReader());

            Assert.Equal(checksum, header.CheckSum);
            Assert.Equal(length, header.Length);
            Assert.Equal(offset, header.Offset);
            Assert.Equal(tag, header.Tag);
        }
    }
}
