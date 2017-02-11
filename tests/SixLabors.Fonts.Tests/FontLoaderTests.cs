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
            FontDescription description = FontDescription.LoadDescription(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", description.FontName);
            Assert.Equal("Regular", description.FontSubFamilyName);
        }

        [Fact]
        public void LoadFont()
        {
            Font font = Font.LoadFont(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", font.FontName);
            Assert.Equal("Regular", font.FontSubFamilyName);

            var glyph = font.GetGlyph('a');
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, 72);
            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(7, r.ControlPoints.Count);
        }
    }
}
