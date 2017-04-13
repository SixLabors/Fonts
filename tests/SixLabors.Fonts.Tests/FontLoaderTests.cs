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
        public void Issue21_LoopDetectedLoadingGlyphs()
        {
            var font = new FontCollection().Install(TestFonts.CarterOneFileData());

            GlyphInstance g = font.FontInstance.GetGlyph('\0');
        }

        [Fact]
        public void LoadFontMetadata()
        {
            FontDescription description = FontDescription.LoadDescription(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", description.FontName);
            Assert.Equal("Regular", description.FontSubFamilyName);
        }

        [Fact]
        public void LoadFontMetadataWoff()
        {
            FontDescription description = FontDescription.LoadDescription(TestFonts.SimpleFontFileWoffData());

            Assert.Equal("SixLaborsSampleAB regular", description.FontName);
            Assert.Equal("Regular", description.FontSubFamilyName);
        }

        [Fact]
        public void LoadFont()
        {
            FontInstance font = FontInstance.LoadFont(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", font.Description.FontName);
            Assert.Equal("Regular", font.Description.FontSubFamilyName);

            var glyph = font.GetGlyph('a');
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(72));
            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(7, r.ControlPoints.Count);
        }

        [Fact]
        public void LoadFontWoff()
        {
            FontInstance font = FontInstance.LoadFont(TestFonts.SimpleFontFileWoffData());

            Assert.Equal("SixLaborsSampleAB regular", font.Description.FontName);
            Assert.Equal("Regular", font.Description.FontSubFamilyName);

            var glyph = font.GetGlyph('a');
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(72));
            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(7, r.ControlPoints.Count);
        }
    }
}
