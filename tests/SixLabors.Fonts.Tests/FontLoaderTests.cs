using System.Linq;

namespace SixLabors.Fonts.Tests
{
    using Xunit;

    public class FontLoaderTests
    {
        [Fact]
        public void Issue21_LoopDetectedLoadingGlyphs()
        {
            Font font = new FontCollection().Install(TestFonts.CarterOneFileData()).CreateFont(12);

            GlyphInstance g = font.Instance.GetGlyph('\0');
        }

        [Fact]
        public void LoadFontMetadata()
        {
            var description = FontDescription.LoadDescription(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", description.FontName);
            Assert.Equal("Regular", description.FontSubFamilyName);
        }

        [Fact]
        public void LoadFontMetadataWoff()
        {
            var description = FontDescription.LoadDescription(TestFonts.SimpleFontFileWoffData());

            Assert.Equal("SixLaborsSampleAB regular", description.FontName);
            Assert.Equal("Regular", description.FontSubFamilyName);
        }

        [Fact]
        public void LoadFont()
        {
            IFontInstance font = FontInstance.LoadFont(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", font.Description.FontName);
            Assert.Equal("Regular", font.Description.FontSubFamilyName);

            GlyphInstance glyph = font.GetGlyph('a');
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(72), 0);
            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(6, r.ControlPoints.Distinct().Count());
        }

        [Fact]
        public void LoadFontWoff()
        {
            IFontInstance font = FontInstance.LoadFont(TestFonts.SimpleFontFileWoffData());

            Assert.Equal("SixLaborsSampleAB regular", font.Description.FontName);
            Assert.Equal("Regular", font.Description.FontSubFamilyName);

            GlyphInstance glyph = font.GetGlyph('a');
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(72), 0);
            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(6, r.ControlPoints.Distinct().Count());
        }
    }
}
