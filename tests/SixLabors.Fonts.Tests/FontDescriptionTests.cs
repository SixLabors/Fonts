using System.Collections.Generic;
using System.Globalization;
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

            var description = FontDescription.LoadDescription(writer.GetStream());
            Assert.Equal("name", description.FontNameInvariantCulture);
            Assert.Equal("sub", description.FontSubFamilyNameInvariantCulture);
            Assert.Equal("fam", description.FontFamilyInvariantCulture);
        }

        [Fact]
        public void LoadFontDescription_CultureNamePriority_FirstWindows()
        {
            var usCulture = new CultureInfo(0x0409);
            var c1 = new CultureInfo(1034); // spanish - international
            var c2 = new CultureInfo(3082); // spanish - traditional

            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(1, 0, 0, 0);
            writer.WriteTableHeader("name", 0, 28, 999);
            writer.WriteNameTable(
                (WellKnownIds.NameIds.FullFontName, "name_c1", c1),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_c1", c1),
                (WellKnownIds.NameIds.FontFamilyName, "fam_c1", c1),
                (WellKnownIds.NameIds.FullFontName, "name_c2", c2),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_c2", c2),
                (WellKnownIds.NameIds.FontFamilyName, "fam_c2", c2)
                );

            var description = FontDescription.LoadDescription(writer.GetStream());

            // unknown culture shoudl prioritise US, but missing so will return first
            Assert.Equal("name_c1", description.FontNameInvariantCulture);
            Assert.Equal("sub_c1", description.FontSubFamilyNameInvariantCulture);
            Assert.Equal("fam_c1", description.FontFamilyInvariantCulture);
        }

        [Fact]
        public void LoadFontDescription_CultureNamePriority_US()
        {
            var usCulture = new CultureInfo(0x0409);
            var c1 = new CultureInfo(1034); // spanish - international
            var c2 = new CultureInfo(3082); // spanish - traditional

            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(1, 0, 0, 0);
            writer.WriteTableHeader("name", 0, 28, 999);
            writer.WriteNameTable(
                (WellKnownIds.NameIds.FullFontName, "name_c1", c1),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_c1", c1),
                (WellKnownIds.NameIds.FontFamilyName, "fam_c1", c1),
                (WellKnownIds.NameIds.FullFontName, "name_c2", c2),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_c2", c2),
                (WellKnownIds.NameIds.FontFamilyName, "fam_c2", c2),
                (WellKnownIds.NameIds.FullFontName, "name_us", usCulture),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_us", usCulture),
                (WellKnownIds.NameIds.FontFamilyName, "fam_us", usCulture)
                );

            var description = FontDescription.LoadDescription(writer.GetStream());

            // unknown culture shoudl prioritise US, but missing so will return first
            Assert.Equal("name_us", description.FontNameInvariantCulture);
            Assert.Equal("sub_us", description.FontSubFamilyNameInvariantCulture);
            Assert.Equal("fam_us", description.FontFamilyInvariantCulture);
        }

        [Fact]
        public void LoadFontDescription_CultureNamePriority_Exactmatch()
        {
            var usCulture = new CultureInfo(0x0409);
            var c1 = new CultureInfo(1034); // spanish - international
            var c2 = new CultureInfo(3082); // spanish - traditional

            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader(1, 0, 0, 0);
            writer.WriteTableHeader("name", 0, 28, 999);
            writer.WriteNameTable(
                (WellKnownIds.NameIds.FullFontName, "name_c1", c1),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_c1", c1),
                (WellKnownIds.NameIds.FontFamilyName, "fam_c1", c1),
                (WellKnownIds.NameIds.FullFontName, "name_c2", c2),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_c2", c2),
                (WellKnownIds.NameIds.FontFamilyName, "fam_c2", c2),
                (WellKnownIds.NameIds.FullFontName, "name_us", usCulture),
                (WellKnownIds.NameIds.FontSubfamilyName, "sub_us", usCulture),
                (WellKnownIds.NameIds.FontFamilyName, "fam_us", usCulture)
                );

            var description = FontDescription.LoadDescription(writer.GetStream());

            // unknown culture shoudl prioritise US, but missing so will return first
            Assert.Equal("name_c2", description.FontName(c2));
            Assert.Equal("sub_c2", description.FontSubFamilyName(c2));
            Assert.Equal("fam_c2", description.FontFamily(c2));
        }
    }
}
