using System.Collections.Generic;

using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.WellKnownIds;

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class NameTableTests
    {
        [Fact]
        public void LoadFormat0()
        {
            var writer = new BinaryWriter();

            writer.WriteNameTable(new Dictionary<NameIds, string>
                                      {
                { NameIds.CopyrightNotice, "copyright"},
                { NameIds.FullFontName, "fullname" },
                { NameIds.FontFamilyName, "family" },
                { NameIds.FontSubfamilyName, "subfamily" },
                { NameIds.UniqueFontID, "id" },
                { (NameIds)90, "other1" },
                { (NameIds)91, "other2" }
                                      });

            NameTable table = NameTable.Load(writer.GetReader());

            Assert.Equal("fullname", table.FontName);
            Assert.Equal("family", table.FontFamilyName);
            Assert.Equal("subfamily", table.FontSubFamilyName);
            Assert.Equal("id", table.Id);
            Assert.Equal("copyright", table.GetNameById(NameIds.CopyrightNotice));
            Assert.Equal("other1", table.GetNameById(90));
            Assert.Equal("other2", table.GetNameById(91));
        }
        [Fact]
        public void LoadFormat1()
        {
            var writer = new BinaryWriter();

            writer.WriteNameTable(new Dictionary<NameIds, string>
                                      {
                { NameIds.CopyrightNotice, "copyright"},
                { NameIds.FullFontName, "fullname" },
                { NameIds.FontFamilyName, "family" },
                { NameIds.FontSubfamilyName, "subfamily" },
                { NameIds.UniqueFontID, "id" },
                { (NameIds)90, "other1" },
                { (NameIds)91, "other2" }
                                      }, new List<string>
                                             {
                                                 "lang1",
                                                 "lang2"
                                             });

            NameTable table = NameTable.Load(writer.GetReader());

            Assert.Equal("fullname", table.FontName);
            Assert.Equal("family", table.FontFamilyName);
            Assert.Equal("subfamily", table.FontSubFamilyName);
            Assert.Equal("id", table.Id);
            Assert.Equal("copyright", table.GetNameById(NameIds.CopyrightNotice));
            Assert.Equal("other1", table.GetNameById(90));
            Assert.Equal("other2", table.GetNameById(91));
        }
    }
}
