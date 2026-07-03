// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

public class TextRendererGlyphIdTests
{
    [Fact]
    public void TryGetGlyphMetrics_GlyphId_MatchesCodePointPath()
    {
        Font font = TestFonts.GetFont(TestFonts.SimpleFontFile, 12);
        CodePoint codePoint = new('A');

        Assert.True(font.TryGetGlyphs(codePoint, out Glyph? glyph));
        ushort glyphId = glyph.Value.GlyphMetrics.GlyphId;

        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            codePoint,
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out FontGlyphMetrics codePointMetrics));

        Assert.True(font.FontMetrics.TryGetGlyphMetrics(
            glyphId,
            TextAttributes.None,
            TextDecorations.None,
            LayoutMode.HorizontalTopBottom,
            ColorFontSupport.None,
            out FontGlyphMetrics glyphIdMetrics));

        Assert.Equal(codePointMetrics.GlyphId, glyphIdMetrics.GlyphId);
        Assert.Equal(codePointMetrics.CodePoint, glyphIdMetrics.CodePoint);
        Assert.Equal(codePointMetrics.AdvanceWidth, glyphIdMetrics.AdvanceWidth);
        Assert.Equal(codePointMetrics.AdvanceHeight, glyphIdMetrics.AdvanceHeight);
    }

    [Fact]
    public void RenderGlyph_GlyphId_MatchesStringRenderingForColorGlyph()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.NotoColorEmojiRegular);
        Font font = family.CreateFont(128);
        CodePoint codePoint = new(0x1F638); // Grinning cat face with smiling eyes.

        Assert.True(font.TryGetGlyphs(codePoint, ColorFontSupport.ColrV1, out Glyph? glyph));

        TextOptions textOptions = new(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV1
        };

        LayerCaptureRenderer textRenderer = new();
        TextRenderer.RenderTo(textRenderer, char.ConvertFromUtf32(codePoint.Value), textOptions);

        GlyphOptions glyphOptions = new()
        {
            Font = font,
            ColorFontSupport = ColorFontSupport.ColrV1
        };

        LayerCaptureRenderer glyphIdRenderer = new();
        TextRenderer.RenderTo(glyphIdRenderer, glyph.Value.GlyphMetrics.GlyphId, glyphOptions);

        Assert.Equal(textRenderer.GlyphKeys.Count, glyphIdRenderer.GlyphKeys.Count);
        Assert.Equal(textRenderer.GlyphRects[0].Width, glyphIdRenderer.GlyphRects[0].Width);
        Assert.Equal(textRenderer.GlyphRects[0].Height, glyphIdRenderer.GlyphRects[0].Height);
        Assert.Equal(textRenderer.SolidLayers, glyphIdRenderer.SolidLayers);
        Assert.Equal(textRenderer.GlyphKeys[0].GlyphId, glyphIdRenderer.GlyphKeys[0].GlyphId);
        Assert.Equal(textRenderer.GlyphKeys[0].CodePoint, glyphIdRenderer.GlyphKeys[0].CodePoint);
    }

    [Fact]
    public void RenderGlyph_UsesTextRunCreatedByGlyphOptions()
    {
        Font font = TestFonts.GetFont(TestFonts.SimpleFontFile, 12);
        CodePoint codePoint = new('A');

        Assert.True(font.TryGetGlyphs(codePoint, out Glyph? glyph));

        CustomGlyphOptions options = new()
        {
            Font = font,
            GraphemeIndex = 42,
            TextAttributes = TextAttributes.Superscript,
            TextDecorations = TextDecorations.Underline
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, glyph.Value.GlyphMetrics.GlyphId, options);

        GlyphRendererParameters parameters = Assert.Single(renderer.GlyphKeys);
        CustomTextRun run = Assert.IsType<CustomTextRun>(parameters.TextRun);
        Assert.Equal(42, parameters.GraphemeIndex);
        Assert.Equal(42, run.Start);
        Assert.Equal(43, run.End);
        Assert.Equal(TextAttributes.Superscript, run.TextAttributes);
        Assert.Equal(TextDecorations.Underline, run.TextDecorations);
    }

    [Fact]
    public void RenderGlyph_UnknownGlyphId_DoesNotRender()
    {
        Font font = TestFonts.GetFont(TestFonts.SimpleFontFile, 12);
        GlyphOptions glyphOptions = new()
        {
            Font = font
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, ushort.MaxValue, glyphOptions);

        Assert.Empty(renderer.GlyphKeys);
        Assert.Empty(renderer.ControlPoints);
    }

    [Fact]
    public void RenderGlyph_ColorGlyphById_EmitsPaintedLayers()
    {
        FontFamily family = TestFonts.GetFontFamily(TestFonts.NotoColorEmojiRegular);
        Font font = family.CreateFont(128);
        CodePoint codePoint = new(0x1F638); // Grinning cat face with smiling eyes.

        Assert.True(font.TryGetGlyphs(codePoint, ColorFontSupport.ColrV1, out Glyph? glyph));

        GlyphOptions glyphOptions = new()
        {
            Font = font,
            ColorFontSupport = ColorFontSupport.ColrV1
        };

        LayerCaptureRenderer renderer = new();
        TextRenderer.RenderTo(renderer, glyph.Value.GlyphMetrics.GlyphId, glyphOptions);

        Assert.Single(renderer.GlyphKeys);
        Assert.NotEmpty(renderer.SolidLayers);
    }

    private sealed class CustomGlyphOptions : GlyphOptions
    {
        protected internal override TextRun CreateTextRun()
            => new CustomTextRun
            {
                Start = this.GraphemeIndex,
                End = this.GraphemeIndex + 1,
                Font = this.Font,
                TextAttributes = this.TextAttributes,
                TextDecorations = this.TextDecorations
            };
    }

    private sealed class CustomTextRun : TextRun
    {
    }

    private sealed class LayerCaptureRenderer : GlyphRenderer
    {
        public List<GlyphColor> SolidLayers { get; } = [];

        public override void BeginLayer(Paint paint, FillRule fillRule, ClipQuad? clipBounds)
        {
            if (paint is SolidPaint solidPaint)
            {
                this.SolidLayers.Add(solidPaint.Color);
            }

            base.BeginLayer(paint, fillRule, clipBounds);
        }
    }
}
