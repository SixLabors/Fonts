// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Unicode;

using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace SixLabors.Fonts.Tests;

public class FontPaletteTests
{
    /// <summary>
    /// Reads the effective default palette straight from the font's CPAL table so tests
    /// derive override lists from the real entry count instead of a guessed size.
    /// </summary>
    private static GlyphColor[] GetDefaultPaletteColors()
    {
        using Stream stream = TestFonts.TwemojiMozillaData();
        using FontReader reader = new(stream);
        return reader.GetTable<CpalTable>().GetPaletteColors(null);
    }

    /// <summary>
    /// Overrides every palette entry of the font with one color, so assertions do not
    /// depend on which CPAL entries the tested glyph references.
    /// </summary>
    private static FontPalette CreateUniformOverride(GlyphColor color)
    {
        GlyphColor[] palette = GetDefaultPaletteColors();
        FontPaletteOverride[] overrides = new FontPaletteOverride[palette.Length];
        for (int i = 0; i < overrides.Length; i++)
        {
            overrides[i] = new FontPaletteOverride(i, color);
        }

        return new FontPalette(0, overrides);
    }

    /// <summary>
    /// Overrides every palette entry with its RGB complement, keeping alpha.
    /// </summary>
    private static FontPalette CreateInvertedPalette()
    {
        GlyphColor[] palette = GetDefaultPaletteColors();
        FontPaletteOverride[] overrides = new FontPaletteOverride[palette.Length];
        for (int i = 0; i < overrides.Length; i++)
        {
            GlyphColor c = palette[i];
            overrides[i] = new FontPaletteOverride(i, new GlyphColor((byte)(255 - c.R), (byte)(255 - c.G), (byte)(255 - c.B), c.A));
        }

        return new FontPalette(0, overrides);
    }

    [Fact]
    public void RenderColrGlyph_PaletteOverrideReplacesColors()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 12);

        ColorGlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0,
            FontPalette = CreateUniformOverride(GlyphColor.Red)
        });

        Assert.Equal(3, renderer.Colors.Count);
        Assert.All(renderer.Colors, c => Assert.Equal(GlyphColor.Red, c));
    }

    [Fact]
    public void RenderColrGlyph_OutOfRangePaletteIndexUsesDefaultPalette()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 12);

        ColorGlyphRenderer defaultRenderer = new();
        TextRenderer.RenderTo(defaultRenderer, "😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0
        });

        ColorGlyphRenderer selectedRenderer = new();
        TextRenderer.RenderTo(selectedRenderer, "😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0,
            FontPalette = new FontPalette(9999)
        });

        Assert.Equal(defaultRenderer.Colors, selectedRenderer.Colors);
    }

    [Fact]
    public void RenderColrGlyph_TextRunPaletteOverridesOptionsPalette()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 12);

        ColorGlyphRenderer defaultRenderer = new();
        TextRenderer.RenderTo(defaultRenderer, "😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0
        });

        // The run covers the first emoji only; the second renders with the options palette.
        ColorGlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "😀😀", new TextOptions(font)
        {
            ColorFontSupport = ColorFontSupport.ColrV0,
            TextRuns = [new TextRun { Start = 0, End = 1, FontPalette = CreateUniformOverride(GlyphColor.Red) }]
        });

        Assert.Equal(6, renderer.Colors.Count);
        Assert.All(renderer.Colors.Take(3), c => Assert.Equal(GlyphColor.Red, c));
        Assert.Equal(defaultRenderer.Colors, [.. renderer.Colors.Skip(3)]);
    }

    [Fact]
    public void RenderGlyphById_UsesGlyphOptionsPalette()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 12);
        Assert.True(font.TryGetGlyphId(new CodePoint(0x1F600), out ushort glyphId));

        ColorGlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, glyphId, new GlyphOptions
        {
            Font = font,
            ColorFontSupport = ColorFontSupport.ColrV0,
            FontPalette = CreateUniformOverride(GlyphColor.Blue)
        });

        Assert.Equal(3, renderer.Colors.Count);
        Assert.All(renderer.Colors, c => Assert.Equal(GlyphColor.Blue, c));
    }

    [Fact]
    public void RenderColrEmojiWithInvertedPalette()
    {
        Font font = TestFonts.GetFont(TestFonts.TwemojiMozillaFile, 128);

        FontRectangle advance = TextMeasurer.MeasureAdvance("😀", new TextOptions(font));
        int width = (int)Math.Ceiling(advance.Width);
        int height = (int)Math.Ceiling(advance.Height);

        // Two large emoji on one canvas: the left renders with the font's default palette,
        // the right renders with a palette that inverts every CPAL entry. One canvas also
        // exercises the renderer glyph cache, which must treat the palette variants of one
        // glyph as distinct entries.
        TextLayoutTestUtilities.TestImage(
            width * 2,
            height,
            img => img.Mutate(ctx => ctx.Paint(canvas =>
            {
                canvas.DrawText(
                    new RichTextOptions(font) { ColorFontSupport = ColorFontSupport.ColrV0 },
                    "😀",
                    Brushes.Solid(Color.Black),
                    pen: null);

                canvas.DrawText(
                    new RichTextOptions(font) { ColorFontSupport = ColorFontSupport.ColrV0, FontPalette = CreateInvertedPalette(), Origin = new Vector2(width, 0) },
                    "😀",
                    Brushes.Solid(Color.Black),
                    pen: null);
            })));
    }
}
