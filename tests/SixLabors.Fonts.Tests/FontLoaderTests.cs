// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontLoaderTests
    {
        [Fact]
        public void Issue21_LoopDetectedLoadingGlyphs()
        {
            Font font = new FontCollection().Install(TestFonts.CarterOneFileData()).CreateFont(12);

            GlyphMetrics g = font.Metrics.GetGlyphMetrics(new CodePoint('\0'));
        }

        [Fact]
        public void LoadFontMetadata()
        {
            var description = FontDescription.LoadDescription(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", description.FontNameInvariantCulture);
            Assert.Equal("Regular", description.FontSubFamilyNameInvariantCulture);
        }

        [Fact]
        public void LoadFontMetadataWoff()
        {
            var description = FontDescription.LoadDescription(TestFonts.SimpleFontFileWoffData());

            Assert.Equal("SixLaborsSampleAB regular", description.FontNameInvariantCulture);
            Assert.Equal("Regular", description.FontSubFamilyNameInvariantCulture);
        }

        [Fact]
        public void LoadFont()
        {
            IFontMetrics font = FontMetrics.LoadFont(TestFonts.SimpleFontFileData());

            Assert.Equal("SixLaborsSampleAB regular", font.Description.FontNameInvariantCulture);
            Assert.Equal("Regular", font.Description.FontSubFamilyNameInvariantCulture);

            GlyphMetrics glyph = font.GetGlyphMetrics(new CodePoint('a'));
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(72), 0);

            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(6, r.ControlPoints.Distinct().Count());
        }

        [Fact]
        public void LoadFontWoff()
        {
            IFontMetrics font = FontMetrics.LoadFont(TestFonts.SimpleFontFileWoffData());

            Assert.Equal("SixLaborsSampleAB regular", font.Description.FontNameInvariantCulture);
            Assert.Equal("Regular", font.Description.FontSubFamilyNameInvariantCulture);

            GlyphMetrics glyph = font.GetGlyphMetrics(new CodePoint('a'));
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(72), 0);

            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(6, r.ControlPoints.Distinct().Count());
        }
    }
}
