// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Verifies that per-glyph measurement matches rendering exactly: the bounds returned by the
/// <see cref="TextMeasurer"/> glyph-id overloads must equal the boxes the renderer reports
/// through <see cref="IGlyphRenderer.BeginGlyph"/> for the same input, and the advance and
/// renderable-bounds overloads must compose consistently with the text-level measurements.
/// </summary>
public class TextMeasurerGlyphIdTests
{
    [Theory]
    [InlineData('A', 0F, 0F, 72F)]
    [InlineData('A', 13.5F, 27.25F, 72F)]
    [InlineData('g', 100F, 50F, 96F)]
    [InlineData(' ', 10F, 10F, 72F)]
    public void MeasureBounds_MatchesRenderedGlyphBounds(char character, float originX, float originY, float dpi)
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, character);

        GlyphOptions options = new()
        {
            Font = font,
            Dpi = dpi,
            Origin = new Vector2(originX, originY)
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, glyphId, options);

        Assert.Single(renderer.GlyphRects);
        Assert.Equal(renderer.GlyphRects[0], TextMeasurer.MeasureBounds(glyphId, options));
    }

    [Fact]
    public void MeasureGlyph_ReturnsEmpty_ForMissingGlyph()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        GlyphOptions options = new()
        {
            Font = font
        };

        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureBounds(ushort.MaxValue, options));
        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureAdvance(ushort.MaxValue, options));
        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureRenderableBounds(ushort.MaxValue, options));
    }

    [Fact]
    public void MeasureAdvance_MatchesLaidOutGlyphAdvanceSize()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, 'A');

        GlyphOptions options = new()
        {
            Font = font
        };

        // Layout produces a positioned advance box per glyph; a lone glyph measured without
        // layout must occupy the same logical cell (advance width by line height).
        GlyphMetrics laidOut = TextMeasurer.GetGlyphMetrics("A", new TextOptions(font)).Span[0];
        FontRectangle measured = TextMeasurer.MeasureAdvance(glyphId, options);

        Assert.Equal(laidOut.Advance.Width, measured.Width, 3F);
        Assert.Equal(laidOut.Advance.Height, measured.Height, 3F);
    }

    [Fact]
    public void MeasureRenderableBounds_IsUnionOfAdvanceAndBounds()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, 'g');

        GlyphOptions options = new()
        {
            Font = font,
            Origin = new Vector2(20F, 60F)
        };

        // The advance is zero-based; renderable bounds union it placed at the origin, exactly
        // as the text-level composition does.
        FontRectangle advance = TextMeasurer.MeasureAdvance(glyphId, options);
        FontRectangle absoluteAdvance = new(options.Origin.X, options.Origin.Y, advance.Width, advance.Height);
        FontRectangle expected = FontRectangle.Union(absoluteAdvance, TextMeasurer.MeasureBounds(glyphId, options));

        Assert.Equal(expected, TextMeasurer.MeasureRenderableBounds(glyphId, options));
    }

    [Fact]
    public void MeasureBounds_MatchesUnionOfRenderedGlyphBounds()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        (GlyphRun glyphRun, GlyphOptions options) = CreateRun(font);

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, glyphRun, options);

        Assert.Equal(glyphRun.Count, renderer.GlyphRects.Count);
        FontRectangle expected = renderer.GlyphRects[0];
        for (int i = 1; i < renderer.GlyphRects.Count; i++)
        {
            expected = FontRectangle.Union(expected, renderer.GlyphRects[i]);
        }

        Assert.Equal(expected, TextMeasurer.MeasureBounds(glyphRun, options));
    }

    [Fact]
    public void MeasureGlyphRun_MatchesUnionOfSingleGlyphMeasurements()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        (GlyphRun glyphRun, GlyphOptions options) = CreateRun(font);

        FontRectangle expectedAdvance = default;
        FontRectangle expectedRenderable = default;
        for (int i = 0; i < glyphRun.Count; i++)
        {
            GlyphOptions positioned = new()
            {
                Font = font,
                Origin = glyphRun.Origins.Span[i]
            };

            ushort glyphId = glyphRun.GlyphIds.Span[i];

            // Per-glyph advances are zero-based; the run extent unions them at their origins,
            // exactly as the text-level composition does.
            FontRectangle advance = TextMeasurer.MeasureAdvance(glyphId, positioned);
            FontRectangle absoluteAdvance = new(positioned.Origin.X, positioned.Origin.Y, advance.Width, advance.Height);
            FontRectangle renderable = TextMeasurer.MeasureRenderableBounds(glyphId, positioned);
            expectedAdvance = i == 0 ? absoluteAdvance : FontRectangle.Union(expectedAdvance, absoluteAdvance);
            expectedRenderable = i == 0 ? renderable : FontRectangle.Union(expectedRenderable, renderable);
        }

        Assert.Equal(new FontRectangle(0, 0, expectedAdvance.Width, expectedAdvance.Height), TextMeasurer.MeasureAdvance(glyphRun, options));
        Assert.Equal(expectedRenderable, TextMeasurer.MeasureRenderableBounds(glyphRun, options));
    }

    [Fact]
    public void MeasureAdvance_IsZeroBased_AndUnmovedByOriginOrBaseline()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, 'A');

        FontRectangle reference = TextMeasurer.MeasureAdvance(glyphId, new GlyphOptions { Font = font });

        Assert.Equal(0, reference.X);
        Assert.Equal(0, reference.Y);
        Assert.True(reference.Width > 0);
        Assert.True(reference.Height > 0);

        // The advance is a logical measure: no origin and no baseline anchor may move it.
        foreach (TextBaseline baseline in Enum.GetValues<TextBaseline>())
        {
            GlyphOptions options = new()
            {
                Font = font,
                Origin = new Vector2(13.5F, 27.25F),
                TextBaseline = baseline
            };

            Assert.Equal(reference, TextMeasurer.MeasureAdvance(glyphId, options));
        }

        // Bounds are positioned geometry and do move with the anchor.
        GlyphOptions lineBox = new() { Font = font, Origin = new Vector2(13.5F, 27.25F) };
        GlyphOptions alphabetic = new() { Font = font, Origin = new Vector2(13.5F, 27.25F), TextBaseline = TextBaseline.Alphabetic };
        Assert.NotEqual(TextMeasurer.MeasureBounds(glyphId, lineBox), TextMeasurer.MeasureBounds(glyphId, alphabetic));
    }

    [Fact]
    public void MeasureAdvance_IsZeroBased_ForVerticalLayouts()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, 'A');

        foreach (LayoutMode layoutMode in new[] { LayoutMode.VerticalLeftRight, LayoutMode.VerticalMixedLeftRight })
        {
            FontRectangle reference = TextMeasurer.MeasureAdvance(glyphId, new GlyphOptions { Font = font, LayoutMode = layoutMode });

            Assert.Equal(0, reference.X);
            Assert.Equal(0, reference.Y);
            Assert.True(reference.Width > 0);
            Assert.True(reference.Height > 0);

            foreach (TextBaseline baseline in Enum.GetValues<TextBaseline>())
            {
                GlyphOptions options = new()
                {
                    Font = font,
                    LayoutMode = layoutMode,
                    Origin = new Vector2(40F, 80F),
                    TextBaseline = baseline
                };

                Assert.Equal(reference, TextMeasurer.MeasureAdvance(glyphId, options));
            }
        }
    }

    [Fact]
    public void MeasureBounds_MatchesRenderedGlyphBounds_ForEveryBaseline()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, 'g');

        foreach (TextBaseline baseline in Enum.GetValues<TextBaseline>())
        {
            GlyphOptions options = new()
            {
                Font = font,
                Dpi = 96F,
                Origin = new Vector2(13.5F, 27.25F),
                TextBaseline = baseline
            };

            GlyphRenderer renderer = new();
            TextRenderer.RenderTo(renderer, glyphId, options);

            Assert.Single(renderer.GlyphRects);
            Assert.Equal(renderer.GlyphRects[0], TextMeasurer.MeasureBounds(glyphId, options));
        }
    }

    [Fact]
    public void MeasureRenderableBounds_ComposesAdvanceAtOrigin_ForEveryBaseline()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, 'g');

        foreach (TextBaseline baseline in Enum.GetValues<TextBaseline>())
        {
            GlyphOptions options = new()
            {
                Font = font,
                Origin = new Vector2(20F, 60F),
                TextBaseline = baseline
            };

            FontRectangle advance = TextMeasurer.MeasureAdvance(glyphId, options);
            FontRectangle absoluteAdvance = new(options.Origin.X, options.Origin.Y, advance.Width, advance.Height);
            FontRectangle expected = FontRectangle.Union(absoluteAdvance, TextMeasurer.MeasureBounds(glyphId, options));

            Assert.Equal(expected, TextMeasurer.MeasureRenderableBounds(glyphId, options));
        }
    }

    [Fact]
    public void Run_MeasureBounds_MatchesRenderedUnion_ForEveryBaseline()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        (GlyphRun glyphRun, GlyphOptions options) = CreateRun(font);

        foreach (TextBaseline baseline in Enum.GetValues<TextBaseline>())
        {
            options.TextBaseline = baseline;

            GlyphRenderer renderer = new();
            TextRenderer.RenderTo(renderer, glyphRun, options);

            Assert.Equal(glyphRun.Count, renderer.GlyphRects.Count);
            FontRectangle expected = renderer.GlyphRects[0];
            for (int i = 1; i < renderer.GlyphRects.Count; i++)
            {
                expected = FontRectangle.Union(expected, renderer.GlyphRects[i]);
            }

            Assert.Equal(expected, TextMeasurer.MeasureBounds(glyphRun, options));
        }
    }

    [Fact]
    public void MeasureGlyphRun_RestoresOptionsOrigin()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        (GlyphRun glyphRun, GlyphOptions options) = CreateRun(font);
        options.Origin = new Vector2(5F, 7F);

        _ = TextMeasurer.MeasureBounds(glyphRun, options);

        Assert.Equal(new Vector2(5F, 7F), options.Origin);
    }

    [Fact]
    public void MeasureGlyphRun_ReturnsEmpty_ForEmptyRun()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        GlyphRun glyphRun = new(ReadOnlyMemory<ushort>.Empty, ReadOnlyMemory<Vector2>.Empty);
        GlyphOptions options = new()
        {
            Font = font
        };

        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureBounds(glyphRun, options));
        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureAdvance(glyphRun, options));
        Assert.Equal(FontRectangle.Empty, TextMeasurer.MeasureRenderableBounds(glyphRun, options));
    }

    [Fact]
    public void GetGlyphMetrics_MatchesSingleGlyphMeasurements()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort glyphId = GetGlyphId(font, 'g');

        GlyphOptions options = new()
        {
            Font = font,
            Origin = new Vector2(20F, 60F),
            GraphemeIndex = 7
        };

        GlyphMetrics metrics = TextMeasurer.GetGlyphMetrics(glyphId, options);

        Assert.Equal(TextMeasurer.MeasureAdvance(glyphId, options), metrics.Advance);
        Assert.Equal(TextMeasurer.MeasureBounds(glyphId, options), metrics.Bounds);
        Assert.Equal(TextMeasurer.MeasureRenderableBounds(glyphId, options), metrics.RenderableBounds);
        Assert.Equal(new CodePoint('g'), metrics.CodePoint);
        Assert.Equal(7, metrics.GraphemeIndex);
        Assert.Equal(0, metrics.StringIndex);
        Assert.Equal(font, metrics.Font);
    }

    [Fact]
    public void GetGlyphMetrics_ReturnsOneIndexCorrelatedEntryPerRunGlyph()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        (GlyphRun glyphRun, GlyphOptions options) = CreateRun(font);

        ReadOnlySpan<GlyphMetrics> entries = TextMeasurer.GetGlyphMetrics(glyphRun, options).Span;

        Assert.Equal(glyphRun.Count, entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            GlyphOptions positioned = new()
            {
                Font = font,
                Origin = glyphRun.Origins.Span[i]
            };

            ushort glyphId = glyphRun.GlyphIds.Span[i];
            Assert.Equal(TextMeasurer.MeasureAdvance(glyphId, positioned), entries[i].Advance);
            Assert.Equal(TextMeasurer.MeasureBounds(glyphId, positioned), entries[i].Bounds);
            Assert.Equal(TextMeasurer.MeasureRenderableBounds(glyphId, positioned), entries[i].RenderableBounds);
            Assert.Equal(i, entries[i].GraphemeIndex);
            Assert.Equal(i, entries[i].StringIndex);
        }
    }

    [Fact]
    public void GetGlyphMetrics_ReturnsEmptyRectangles_ForMissingRunGlyph()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 32);
        ushort[] glyphIds = [GetGlyphId(font, 'A'), ushort.MaxValue, GetGlyphId(font, 'B')];
        Vector2[] origins = [new(0F, 40F), new(25F, 40F), new(50F, 40F)];
        GlyphRun glyphRun = new(glyphIds, origins);
        GlyphOptions options = new()
        {
            Font = font
        };

        ReadOnlySpan<GlyphMetrics> entries = TextMeasurer.GetGlyphMetrics(glyphRun, options).Span;

        Assert.Equal(3, entries.Length);
        Assert.NotEqual(FontRectangle.Empty, entries[0].Bounds);
        Assert.Equal(FontRectangle.Empty, entries[1].Advance);
        Assert.Equal(FontRectangle.Empty, entries[1].Bounds);
        Assert.Equal(FontRectangle.Empty, entries[1].RenderableBounds);
        Assert.NotEqual(FontRectangle.Empty, entries[2].Bounds);
    }


    [Fact]
    public void GetIntersections_DescenderBand_IsNarrowerThanInkBounds()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 64);
        ushort glyphId = GetGlyphId(font, 'p');

        // The band arithmetic below is baseline-relative, so anchor the origin on the
        // alphabetic baseline rather than the default em-box top.
        GlyphOptions options = new()
        {
            Font = font,
            Origin = new Vector2(10F, 100F),
            TextBaseline = TextBaseline.Alphabetic
        };

        FontRectangle bounds = TextMeasurer.MeasureBounds(glyphId, options);

        // A thin band just below the baseline crosses only the descender stem.
        ReadOnlySpan<float> intersections = TextMeasurer.GetIntersections(glyphId, options, 103F, 106F).Span;

        Assert.True(intersections.Length >= 2);
        Assert.True(intersections.Length % 2 == 0);

        float coveredWidth = 0F;
        for (int i = 0; i < intersections.Length; i += 2)
        {
            Assert.True(intersections[i] <= intersections[i + 1]);
            Assert.True(intersections[i] >= bounds.Left - 1F);
            Assert.True(intersections[i + 1] <= bounds.Right + 1F);
            coveredWidth += intersections[i + 1] - intersections[i];
        }

        // The stem is a small fraction of the glyph's full ink width; the box approximation
        // this API replaces would report the full width.
        Assert.True(coveredWidth < bounds.Width * 0.5F);
    }

    [Fact]
    public void GetIntersections_BandOutsideInk_ReturnsEmpty()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 64);
        ushort glyphId = GetGlyphId(font, 'o');

        GlyphOptions options = new()
        {
            Font = font,
            Origin = new Vector2(10F, 100F)
        };

        // 'o' has no descender; a band below the baseline never touches its outline.
        Assert.True(TextMeasurer.GetIntersections(glyphId, options, 105F, 110F).IsEmpty);
    }

    [Fact]
    public void GetIntersections_Run_MatchesUnionOfSingleGlyphIntersections()
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 64);
        ushort glyphId = GetGlyphId(font, 'p');
        Vector2[] origins = [new(10F, 100F), new(60F, 100F)];
        GlyphRun glyphRun = new(new[] { glyphId, glyphId }, origins);
        GlyphOptions options = new()
        {
            Font = font
        };

        ReadOnlySpan<float> run = TextMeasurer.GetIntersections(glyphRun, options, 103F, 106F).Span;

        GlyphOptions first = new() { Font = font, Origin = origins[0] };
        GlyphOptions second = new() { Font = font, Origin = origins[1] };
        ReadOnlySpan<float> a = TextMeasurer.GetIntersections(glyphId, first, 103F, 106F).Span;
        ReadOnlySpan<float> b = TextMeasurer.GetIntersections(glyphId, second, 103F, 106F).Span;

        // The glyphs are far apart, so the run result is the concatenation of the singles.
        Assert.Equal(a.Length + b.Length, run.Length);
        for (int i = 0; i < a.Length; i++)
        {
            Assert.Equal(a[i], run[i], 2F);
        }

        for (int i = 0; i < b.Length; i++)
        {
            Assert.Equal(b[i], run[a.Length + i], 2F);
        }
    }

    /// <summary>
    /// Creates a small positioned run with fractional origins and a whitespace glyph.
    /// </summary>
    /// <param name="font">The font to resolve glyph ids against.</param>
    /// <returns>The positioned run and matching options.</returns>
    private static (GlyphRun GlyphRun, GlyphOptions Options) CreateRun(Font font)
    {
        ushort[] glyphIds =
        [
            GetGlyphId(font, 'H'),
            GetGlyphId(font, 'e'),
            GetGlyphId(font, 'y'),
            GetGlyphId(font, ' '),
            GetGlyphId(font, '!')
        ];

        Vector2[] origins =
        [
            new(10F, 40F),
            new(33.5F, 40F),
            new(52F, 42.25F),
            new(70F, 40F),
            new(80F, 38F)
        ];

        return (new GlyphRun(glyphIds, origins), new GlyphOptions { Font = font });
    }

    /// <summary>
    /// Resolves the glyph id for a character through the public glyph lookup.
    /// </summary>
    /// <param name="font">The font to resolve against.</param>
    /// <param name="character">The character to resolve.</param>
    /// <returns>The glyph id.</returns>
    private static ushort GetGlyphId(Font font, char character)
    {
        Assert.True(font.TryGetGlyphs(new CodePoint(character), out Glyph? glyph));
        return glyph.Value.GlyphMetrics.GlyphId;
    }
}
