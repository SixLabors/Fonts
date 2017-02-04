using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SixLabors.Fonts.Tables;

using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontReaderTests
    {
        [Fact]
        public void ReadTrueTypeOutlineType()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(0, 0, 0, 0);

            var reader = new FontReader(writer.GetStream());
            Assert.Equal(FontReader.OutlineTypes.TrueType, reader.OutlineType);
        }

        [Fact]
        public void ReadCcfOutlineType()
        {
            var writer = new BinaryWriter();
            writer.WriteCffFileHeader(0, 0, 0, 0);

            var reader = new FontReader(writer.GetStream());
            Assert.Equal(FontReader.OutlineTypes.CFF, reader.OutlineType);
        }

        [Fact]
        public void ReadTableHeaders()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(2, 0, 0, 0);
            writer.WriteTableHeader("TAG1", 0, 10, 0);
            writer.WriteTableHeader("TAG2", 0, 1, 0);

            var reader = new FontReader(writer.GetStream());

            Assert.Equal(2, reader.Tables.Length);
            var names = reader.Tables.OfType<UnknownTable>().Select(x => x.Name);
            Assert.Contains("TAG1", names);
            Assert.Contains("TAG2", names);
        }
    }
}
