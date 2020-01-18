using System;
using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class IndexLocationTableTests
    {
        [Fact]
        public void ShouldThrowExceptionWhenHeadTableCouldNotBeFound()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader();

            using (System.IO.MemoryStream stream = writer.GetStream())
            {
                MissingFontTableException exception = Assert.Throws<MissingFontTableException>(() =>
                {
                    IndexLocationTable.Load(new FontReader(stream));
                });

                Assert.Equal("head", exception.Table);
            }
        }

        [Fact]
        public void ShouldThrowExceptionWhenMaximumProfileTableCouldNotBeFound()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(new TableHeader("head", 0, 0, 0));

            writer.WriteHeadTable(new HeadTable(HeadTable.HeadFlags.None,
                HeadTable.HeadMacStyle.Italic | HeadTable.HeadMacStyle.Bold,
                1024,
                new DateTime(2017, 02, 06, 07, 47, 00),
                new DateTime(2017, 02, 07, 07, 47, 00),
                new Bounds(0, 0, 1024, 1022), 0, HeadTable.IndexLocationFormats.Offset16));

            using (System.IO.MemoryStream stream = writer.GetStream())
            {
                InvalidFontTableException exception = Assert.Throws<InvalidFontTableException>(() =>
                {
                    IndexLocationTable.Load(new FontReader(stream));
                });

                Assert.Equal("maxp", exception.Table);
            }
        }

        [Fact]
        public void ShouldReturnNullWhenTableCouldNotBeFound()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(new TableHeader("head", 0, 0, 0), new TableHeader("maxp", 0, 0, 0));

            writer.WriteHeadTable(new HeadTable(HeadTable.HeadFlags.None,
                HeadTable.HeadMacStyle.Italic | HeadTable.HeadMacStyle.Bold,
                1024,
                new DateTime(2017, 02, 06, 07, 47, 00),
                new DateTime(2017, 02, 07, 07, 47, 00),
                new Bounds(0, 0, 1024, 1022), 0, HeadTable.IndexLocationFormats.Offset16));

            using (System.IO.MemoryStream stream = writer.GetStream())
            {
                Assert.Null(IndexLocationTable.Load(new FontReader(stream)));
            }
        }
    }
}
