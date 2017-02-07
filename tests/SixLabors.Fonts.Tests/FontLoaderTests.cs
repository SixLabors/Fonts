using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tests
{
    using Xunit;

    public class FontLoaderTests
    {
        [Fact]
        public void LoadFontMetadata()
        {
            FontDescription description = FontDescription.Load(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSamplesAB", description.FontName);
            Assert.Equal("AB", description.FontSubFamilyName);
        }
    }
}
