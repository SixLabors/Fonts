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
            Font font = new FontCollection().Add(TestFonts.CarterOneFileData()).CreateFont(12);
            font.FontMetrics.GetGlyphMetrics(new CodePoint('\0'), ColorFontSupport.None).First();
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
        public void LoadFont_WithTtfFormat()
        {
            Font font = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(12);

            GlyphMetrics glyph = font.FontMetrics.GetGlyphMetrics(new CodePoint('A'), ColorFontSupport.None).First();
            var r = new GlyphRenderer();
            glyph.RenderTo(r, font.Size, System.Numerics.Vector2.Zero, new TextOptions(font));

            Assert.Equal(37, r.ControlPoints.Count);
            Assert.Single(r.GlyphKeys);
            Assert.Single(r.GlyphRects);
        }

        [Fact]
        public void LoadFont_WithWoff1Format()
        {
            Font font = new FontCollection().Add(TestFonts.OpenSansFileWoff1).CreateFont(12);

            GlyphMetrics glyph = font.FontMetrics.GetGlyphMetrics(new CodePoint('A'), ColorFontSupport.None).First();
            var r = new GlyphRenderer();
            glyph.RenderTo(r, font.Size, System.Numerics.Vector2.Zero, new TextOptions(font));

            Assert.Equal(37, r.ControlPoints.Count);
            Assert.Single(r.GlyphKeys);
            Assert.Single(r.GlyphRects);
        }

        [Fact]
        public void LoadFontMetadata_WithWoff1Format()
        {
            var description = FontDescription.LoadDescription(TestFonts.OpensSansWoff1Data());

            Assert.Equal("Open Sans Regular", description.FontNameInvariantCulture);
            Assert.Equal("Regular", description.FontSubFamilyNameInvariantCulture);
        }

#if NETCOREAPP3_0_OR_GREATER
        [Fact]
        public void LoadFontMetadata_WithWoff2Format()
        {
            var description = FontDescription.LoadDescription(TestFonts.OpensSansWoff2Data());

            Assert.Equal("Open Sans Regular", description.FontNameInvariantCulture);
            Assert.Equal("Regular", description.FontSubFamilyNameInvariantCulture);
        }

        [Fact]
        public void LoadFont_WithWoff2Format()
        {
            Font font = new FontCollection().Add(TestFonts.OpensSansWoff2Data()).CreateFont(12);

            GlyphMetrics glyph = font.FontMetrics.GetGlyphMetrics(new CodePoint('A'), ColorFontSupport.None).First();
            var r = new GlyphRenderer();
            glyph.RenderTo(r, font.Size, System.Numerics.Vector2.Zero, new TextOptions(font));

            Assert.Equal(37, r.ControlPoints.Count);
            Assert.Single(r.GlyphKeys);
            Assert.Single(r.GlyphRects);
        }
#endif

        [Fact]
        public void LoadFont()
        {
            Font font = new FontCollection().Add(TestFonts.SimpleFontFileData()).CreateFont(12);

            Assert.Equal("SixLaborsSampleAB regular", font.FontMetrics.Description.FontNameInvariantCulture);
            Assert.Equal("Regular", font.FontMetrics.Description.FontSubFamilyNameInvariantCulture);

            GlyphMetrics glyph = font.FontMetrics.GetGlyphMetrics(new CodePoint('a'), ColorFontSupport.None).First();
            var r = new GlyphRenderer();
            glyph.RenderTo(r, font.Size, System.Numerics.Vector2.Zero, new TextOptions(font));

            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(6, r.ControlPoints.Distinct().Count());
        }

        [Fact]
        public void LoadFontWoff()
        {
            Font font = new FontCollection().Add(TestFonts.SimpleFontFileWoffData()).CreateFont(12);

            Assert.Equal("SixLaborsSampleAB regular", font.FontMetrics.Description.FontNameInvariantCulture);
            Assert.Equal("Regular", font.FontMetrics.Description.FontSubFamilyNameInvariantCulture);

            GlyphMetrics glyph = font.FontMetrics.GetGlyphMetrics(new CodePoint('a'), ColorFontSupport.None).First();
            var r = new GlyphRenderer();
            glyph.RenderTo(r, 12, System.Numerics.Vector2.Zero, new TextOptions(font));

            // the test font only has characters .notdef, 'a' & 'b' defined
            Assert.Equal(6, r.ControlPoints.Distinct().Count());
        }
    }
}
