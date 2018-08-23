using System.Collections.Generic;

using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontDescriptionTests
    {
        [Fact]
        public void LoadFontDescription()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(1, 0, 0, 0);
            writer.WriteTableHeader("name", 0, 28, 999);
            writer.WriteNameTable(
                new Dictionary<WellKnownIds.NameIds, string>
                    {
                        { WellKnownIds.NameIds.FullFontName, "name" } ,
                         { WellKnownIds.NameIds.FontSubfamilyName, "sub" },
                         { WellKnownIds.NameIds.FontFamilyName, "fam" }
                    });

            FontDescription description = FontDescription.LoadDescription(writer.GetStream());
            Assert.Equal("name", description.FontName);
            Assert.Equal("sub", description.FontSubFamilyName);
            Assert.Equal("fam", description.FontFamily);
        }
    }
}
