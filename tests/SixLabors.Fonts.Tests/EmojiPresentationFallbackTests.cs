// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using HarfBuzzSharp;
using HBFace = HarfBuzzSharp.Face;
using HBFont = HarfBuzzSharp.Font;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Font fallback with a text font and a color emoji font: a presentation selector decides which
/// font owns the cluster, and an emoji sequence stays in one font as one glyph.
/// </summary>
public class EmojiPresentationFallbackTests
{
    // The sun has text presentation by default. U+FE0E asks for the text glyph, U+FE0F for the emoji glyph.
    private const string Suns = "☀︎ ☀️ ☀";

    // The shrug with a skin tone joined to the female sign with emoji presentation, then the plain shrug.
    private const string Shrugs = "🤷🏽‍♀️ 🤷🏽";

    // The text that tests/Browser/EmojiPresentation.html renders: text presentation, emoji presentation and
    // no selector for two symbols, then joiner, modifier, keycap and flag sequences, and a face that
    // DejaVu Sans maps without a selector.
    private const string ComparisonText = "☀︎ ☀️ ☀ ☺︎ ☺️ ☺\n🤷🏽‍♀️ 🤷🏽 👨‍👩‍👧‍👦 ❤️‍🔥\n1️⃣ 🇯🇵 👍🏽 😀";

    private static readonly CodePoint Sun = new(0x2600);

    private static readonly CodePoint SmilingFace = new(0x263A);

    private const int TextPresentationSelector = 0xFE0E;

    private const int EmojiPresentationSelector = 0xFE0F;

    /// <summary>
    /// Gets the color emoji fonts in the test set: COLR v1, COLR v0 with a cmap format 14, COLR v1 that
    /// also carries COLR v0 layer records with a cmap format 14, and COLR v0.
    /// </summary>
    public static TheoryData<string> ColorEmojiFonts { get; } = new()
    {
        TestFonts.NotoColorEmojiRegular,
        TestFonts.SegoeuiEmojiFile,
        TestFonts.SegoeuiEmoji170File,
        TestFonts.TwemojiMozillaFile
    };

    /// <summary>
    /// Gets both Segoe UI Emoji builds in the test set: 1.33 with COLR v0 and 1.70 with COLR v1.
    /// </summary>
    public static TheoryData<string> SegoeUIEmojiFonts { get; } = new()
    {
        TestFonts.SegoeuiEmojiFile,
        TestFonts.SegoeuiEmoji170File
    };

    /// <summary>
    /// Gets the sequences a text font maps only in part, paired with every color emoji font. DejaVu Sans
    /// maps the digit, the heart, the joiner and the selectors but none of the emoji, so each
    /// cluster must move to the emoji font as a whole.
    /// </summary>
    public static TheoryData<string, string> SequencesAndColorEmojiFonts { get; } = BuildSequenceCases();

    /// <summary>
    /// Gets the font chains that tests/Browser/EmojiPresentation.html renders beside the output, with the index
    /// of the chain font that owns each cluster of <see cref="ComparisonText"/>, whitespace excluded. A text
    /// selector takes the first font whose glyph is not painted, an emoji selector the first font whose glyph
    /// is painted, and a cluster without a selector the first font that maps all of it.
    /// </summary>
    public static TheoryData<string, string[], int[]> BrowserComparisonChains { get; } = new()
    {
        {
            "DejaVuSans-NotoColorEmoji",
            [TestFonts.Anchor2FontFile, TestFonts.NotoColorEmojiRegular],
            [0, 1, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 0]
        },
        {
            "NotoColorEmoji-DejaVuSans",
            [TestFonts.NotoColorEmojiRegular, TestFonts.Anchor2FontFile],
            [1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
        },
        {
            "Arial-SegoeUIEmoji-DejaVuSans",
            [TestFonts.Arial, TestFonts.SegoeuiEmojiFile, TestFonts.Anchor2FontFile],
            [2, 1, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1]
        },
        {
            "Arial-SegoeUIEmoji170-DejaVuSans",
            [TestFonts.Arial, TestFonts.SegoeuiEmoji170File, TestFonts.Anchor2FontFile],
            [2, 1, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1]
        }
    };

    [Fact]
    public void TextFontFirst_SelectorsChooseTheFont()
    {
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily emoji = TestFonts.GetFontFamily(TestFonts.NotoColorEmojiRegular);

        GlyphRendererParameters[] suns = RenderSuns(text, emoji);

        Assert.Equal(3, suns.Length);
        AssertFont(text, suns[0]);
        AssertFont(emoji, suns[1]);
        AssertFont(text, suns[2]);
        Assert.Equal(GlyphType.Painted, suns[1].GlyphType);
    }

    [Fact]
    public void EmojiFontFirst_SelectorsChooseTheFont()
    {
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily emoji = TestFonts.GetFontFamily(TestFonts.NotoColorEmojiRegular);

        GlyphRendererParameters[] suns = RenderSuns(emoji, text);

        Assert.Equal(3, suns.Length);
        AssertFont(text, suns[0]);
        AssertFont(emoji, suns[1]);
        AssertFont(emoji, suns[2]);
    }

    [Fact]
    public void NoColorFont_EmojiSelectorTakesTheBaseGlyph()
    {
        // Neither font has a color table, so no font answers ☀️ and the pass that ignores
        // the selector takes the base glyph from the first font that maps it.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily other = TestFonts.GetFontFamily(TestFonts.OpenSansFile);

        GlyphRendererParameters[] suns = RenderSuns(text, other);

        Assert.Equal(3, suns.Length);
        Assert.All(suns, sun => AssertFont(text, sun));
        Assert.All(suns, sun => Assert.Equal(GlyphType.Standard, sun.GlyphType));
    }

    [Fact]
    public void TextFontFirst_ZwjSequenceStaysInTheEmojiFont()
    {
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily emoji = TestFonts.GetFontFamily(TestFonts.NotoColorEmojiRegular);
        TextOptions options = new(text.CreateFont(48))
        {
            FallbackFontFamilies = [emoji]
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, Shrugs, options);
        string textFont = text.Name.ToUpperInvariant();

        // Both clusters render as one painted glyph each from the emoji font. The text font
        // keeps only the space; it must not keep the female sign out of the first cluster.
        GlyphRendererParameters[] painted = renderer.GlyphKeys.Where(k => k.GlyphType == GlyphType.Painted).ToArray();
        Assert.Equal(2, painted.Length);
        Assert.All(painted, glyph => AssertFont(emoji, glyph));
        Assert.NotEqual(painted[0].GlyphId, painted[1].GlyphId);
        Assert.DoesNotContain(renderer.GlyphKeys, k => k.Font == textFont && k.CodePoint.Value != 0x20);
        Assert.DoesNotContain(renderer.GlyphKeys, k => k.GlyphType == GlyphType.Fallback);
    }

    [Theory]
    [MemberData(nameof(ColorEmojiFonts))]
    public void TextFontFirst_SelectorsChooseTheFont_ForEveryColorFont(string emojiFile)
    {
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily emoji = TestFonts.GetFontFamily(emojiFile);

        GlyphRendererParameters[] suns = RenderSuns(text, emoji);

        Assert.Equal(3, suns.Length);
        AssertFont(text, suns[0]);
        AssertFont(emoji, suns[1]);
        AssertFont(text, suns[2]);
        Assert.Equal(GlyphType.Painted, suns[1].GlyphType);
    }

    [Theory]
    [MemberData(nameof(ColorEmojiFonts))]
    public void VariationSequences_ResolveLikeHarfBuzz_ForEveryMappedCodePoint(string emojiFile)
    {
        // Every base character the font maps, paired with each presentation selector, resolves
        // through the cmap format 14 default and non-default tables the way HarfBuzz resolves it.
        using Blob blob = Blob.FromFile(emojiFile);
        using HBFace face = new(blob, 0);
        using HBFont hbFont = new(face);

        FontMetrics metrics = TestFonts.GetFont(emojiFile, 12).FontMetrics;
        ReadOnlySpan<CodePoint> codePoints = metrics.GetAvailableCodePoints().Span;
        int[] selectors = [TextPresentationSelector, EmojiPresentationSelector];
        foreach (CodePoint codePoint in codePoints)
        {
            foreach (int selector in selectors)
            {
                bool declared = hbFont.TryGetVariationGlyph((uint)codePoint.Value, (uint)selector, out uint expected);
                Assert.True(metrics.TryGetGlyphId(codePoint, new CodePoint(selector), out ushort actual, out bool consumed));
                Assert.True(declared == consumed, $"U+{codePoint.Value:X4} U+{selector:X4}: HarfBuzz declared {declared}, Fonts consumed {consumed}.");
                if (declared)
                {
                    Assert.Equal(expected, actual);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(SegoeUIEmojiFonts))]
    public void SegoeUIEmoji_DefaultVariationSequences_ResolveToTheBaseGlyph(string emojiFile)
    {
        // Both builds list the female sign and the heart with U+FE0F in their default UVS
        // ranges, so the pair resolves to the base glyph and the selector is consumed.
        FontMetrics metrics = TestFonts.GetFont(emojiFile, 12).FontMetrics;
        CodePoint selector = new(EmojiPresentationSelector);
        foreach (int value in new[] { 0x2640, 0x2764 })
        {
            CodePoint codePoint = new(value);
            Assert.True(metrics.TryGetGlyphId(codePoint, out ushort baseGlyph));
            Assert.True(metrics.TryGetGlyphId(codePoint, selector, out ushort pairGlyph, out bool consumed));
            Assert.True(consumed, $"U+{value:X4} U+FE0F was not consumed.");
            Assert.Equal(baseGlyph, pairGlyph);
        }
    }

    [Theory]
    [MemberData(nameof(ColorEmojiFonts))]
    public void EmojiFontFirst_SelectorsChooseTheFont_ForEveryColorFont(string emojiFile)
    {
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily emoji = TestFonts.GetFontFamily(emojiFile);

        GlyphRendererParameters[] suns = RenderSuns(emoji, text);

        Assert.Equal(3, suns.Length);
        AssertFont(text, suns[0]);
        AssertFont(emoji, suns[1]);
        AssertFont(emoji, suns[2]);
    }

    [Theory]
    [MemberData(nameof(SegoeUIEmojiFonts))]
    public void ArialThenSegoeUIEmoji_TextSelectorWithoutAPlainSunTakesTheBaseGlyph(string emojiFile)
    {
        // Arial has no sun and Segoe UI Emoji has a color table, so no font answers ☀︎ and
        // the pass that ignores the selector takes the base glyph from Segoe UI Emoji.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Arial);
        FontFamily emoji = TestFonts.GetFontFamily(emojiFile);

        GlyphRendererParameters[] suns = RenderSuns(text, emoji);

        Assert.Equal(3, suns.Length);
        Assert.All(suns, sun => AssertFont(emoji, sun));
        Assert.All(suns, sun => Assert.Equal(GlyphType.Painted, sun.GlyphType));
        Assert.Equal(suns[2].GlyphId, suns[0].GlyphId);
    }

    [Theory]
    [MemberData(nameof(ColorEmojiFonts))]
    public void ColorFontAlone_TextSelectorTakesTheBaseGlyph(string emojiFile)
    {
        // None of the color fonts declares the sun's text selector sequence, and each has a
        // color table, so none answers ☀︎ and the pass that ignores the selector takes the base glyph.
        Assert.False(TryGetVariationGlyph(emojiFile, TextPresentationSelector, out _));

        FontFamily emoji = TestFonts.GetFontFamily(emojiFile);
        TextOptions options = new(emoji.CreateFont(48));

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, Suns, options);
        GlyphRendererParameters[] suns = renderer.GlyphKeys.Where(k => k.CodePoint == Sun).ToArray();

        Assert.Equal(3, suns.Length);
        Assert.All(suns, sun => AssertFont(emoji, sun));
        Assert.All(suns, sun => Assert.Equal(GlyphType.Painted, sun.GlyphType));
        Assert.Equal(suns[2].GlyphId, suns[0].GlyphId);
    }

    [Fact]
    public void ColorSupportNone_EmojiSelectorTakesTheFirstFontsBaseGlyph()
    {
        // With no color format enabled no font counts as a color font, so no font answers
        // ☀️ and the pass that ignores the selector takes the base glyph from the first font.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily emoji = TestFonts.GetFontFamily(TestFonts.NotoColorEmojiRegular);
        TextOptions options = new(text.CreateFont(48))
        {
            ColorFontSupport = ColorFontSupport.None,
            FallbackFontFamilies = [emoji]
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, Suns, options);
        GlyphRendererParameters[] suns = renderer.GlyphKeys.Where(k => k.CodePoint == Sun).ToArray();

        Assert.Equal(3, suns.Length);
        Assert.All(suns, sun => AssertFont(text, sun));
        Assert.All(suns, sun => Assert.Equal(GlyphType.Standard, sun.GlyphType));
    }

    [Theory]
    [MemberData(nameof(SegoeUIEmojiFonts))]
    public void ArialThenSegoeUIEmoji_SmilingFaceSelectorsChooseTheFont(string emojiFile)
    {
        // Arial maps the smiling face, and Segoe UI Emoji declares the emoji sequence in its cmap.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Arial);
        FontFamily emoji = TestFonts.GetFontFamily(emojiFile);
        TextOptions options = new(text.CreateFont(48))
        {
            FallbackFontFamilies = [emoji]
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "☺︎ ☺️ ☺", options);
        GlyphRendererParameters[] faces = renderer.GlyphKeys.Where(k => k.CodePoint == SmilingFace).ToArray();

        Assert.Equal(3, faces.Length);
        AssertFont(text, faces[0]);
        AssertFont(emoji, faces[1]);
        AssertFont(text, faces[2]);
        Assert.Equal(GlyphType.Painted, faces[1].GlyphType);
    }

    [Fact]
    public void MonochromeEmojiFont_EmojiSelectorTakesTheBaseGlyph()
    {
        // Neither font has a color table, so no font answers ☀️ and the pass that ignores
        // the selector takes the base glyph from the first font that maps it.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily monochrome = TestFonts.GetFontFamily(TestFonts.NotoEmojiVariableFont);

        GlyphRendererParameters[] suns = RenderSuns(text, monochrome);

        Assert.Equal(3, suns.Length);
        Assert.All(suns, sun => AssertFont(text, sun));
        Assert.All(suns, sun => Assert.Equal(GlyphType.Standard, sun.GlyphType));
    }

    [Fact]
    public void MonochromeEmojiFont_ReplacesAClusterTheTextFontCannotMap()
    {
        // Noto Emoji maps both shrugs as plain glyphs. No font answers the U+FE0F of the
        // first, so the pass that ignores the selector takes Noto Emoji's glyphs for it too.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily monochrome = TestFonts.GetFontFamily(TestFonts.NotoEmojiVariableFont);
        TextOptions options = new(text.CreateFont(48))
        {
            FallbackFontFamilies = [monochrome]
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, Shrugs, options);
        string textFont = text.Name.ToUpperInvariant();

        Assert.DoesNotContain(renderer.GlyphKeys, k => k.GlyphType == GlyphType.Fallback);
        Assert.DoesNotContain(renderer.GlyphKeys, k => k.Font == textFont && k.CodePoint.Value != 0x20);
        Assert.Contains(renderer.GlyphKeys, k => k.Font != textFont && k.GlyphId != 0 && k.GlyphType == GlyphType.Standard);
    }

    [Theory]
    [MemberData(nameof(SequencesAndColorEmojiFonts))]
    public void TextFontFirst_SequenceStaysInTheEmojiFont(string sequence, string emojiFile)
    {
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Anchor2FontFile);
        FontFamily emoji = TestFonts.GetFontFamily(emojiFile);
        TextOptions options = new(text.CreateFont(48))
        {
            FallbackFontFamilies = [emoji]
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, sequence, options);
        string textFont = text.Name.ToUpperInvariant();

        // Every visible glyph is a painted glyph of the emoji font. Segoe UI Emoji builds
        // the family from several positioned glyphs, so the count is not fixed.
        Assert.DoesNotContain(renderer.GlyphKeys, k => k.GlyphType == GlyphType.Fallback);
        Assert.DoesNotContain(renderer.GlyphKeys, k => k.Font == textFont);
        Assert.Contains(renderer.GlyphKeys, k => k.GlyphType == GlyphType.Painted);
        Assert.All(renderer.GlyphKeys.Where(k => k.GlyphType == GlyphType.Painted), glyph => AssertFont(emoji, glyph));
    }

    [Theory]
    [MemberData(nameof(BrowserComparisonChains))]
    public void EmojiPresentation_BrowserComparison(string chainName, string[] fontFiles, int[] fontIndexes)
    {
        FontCollection collection = new();
        FontFamily[] chain = Array.ConvertAll(fontFiles, file => TestFonts.GetFontFamily(collection, file));
        TextOptions options = new(chain[0].CreateFont(30))
        {
            Dpi = 96F,
            LineSpacing = 1.4F,
            FallbackFontFamilies = chain[1..]
        };

        // Selectors and joiners are default ignorable and render nothing, so they are left out.
        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, ComparisonText, options);
        IGrouping<int, GlyphRendererParameters>[] clusters = renderer.GlyphKeys
            .Where(k => !CodePoint.IsWhiteSpace(k.CodePoint))
            .GroupBy(k => k.GraphemeIndex)
            .OrderBy(g => g.Key)
            .ToArray();

        Assert.Equal(fontIndexes.Length, clusters.Length);
        for (int i = 0; i < clusters.Length; i++)
        {
            FontFamily expected = chain[fontIndexes[i]];
            GlyphRendererParameters[] visible = clusters[i].Where(k => !CodePoint.IsVariationSelector(k.CodePoint) && !CodePoint.IsZeroWidthJoiner(k.CodePoint)).ToArray();
            Assert.NotEmpty(visible);
            Assert.All(visible, glyph =>
            {
                AssertFont(expected, glyph);
                Assert.NotEqual((ushort)0, glyph.GlyphId);
                Assert.NotEqual(GlyphType.Fallback, glyph.GlyphType);
            });
        }

        // A string property is decorated in the file name. The formattable string keeps the chain name plain.
        FormattableString name = $"{chainName}";
        TextLayoutTestUtilities.TestLayout(ComparisonText, options, properties: [name]);
    }

    [Fact]
    public void EmojiPresentation_BrowserComparison_Hand()
    {
        // The toned thumbs-up of Segoe UI Emoji 1.70 shades its palm and fingers with radial
        // gradients that the font squashes and skews with PaintTransform. At 192 points the
        // shading is large enough to compare against the browser pixel for pixel.
        FontCollection collection = new();
        Font font = TestFonts.GetFontFamily(collection, TestFonts.SegoeuiEmoji170File).CreateFont(192);
        TextOptions options = new(font)
        {
            Dpi = 96F,
            LineSpacing = 1.4F
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "👍🏽", options);
        GlyphRendererParameters[] visible = renderer.GlyphKeys.Where(k => !CodePoint.IsVariationSelector(k.CodePoint)).ToArray();
        Assert.NotEmpty(visible);
        Assert.All(visible, glyph => Assert.Equal(GlyphType.Painted, glyph.GlyphType));

        // A string property is decorated in the file name. The formattable string keeps the font name plain.
        FormattableString name = $"SegoeUIEmoji170";
        TextLayoutTestUtilities.TestLayout("👍🏽", options, properties: [name]);
    }

    private static TheoryData<string, string> BuildSequenceCases()
    {
        // Keycap, flag, joiner with a selector inside, modifier, and the family joiner sequence.
        // Segoe UI Emoji draws no country flags; a regional indicator pair renders as two
        // outline letters there, so the flag is paired with the other fonts only.
        const string flag = "🇯🇵";
        string[] sequences = ["1️⃣", flag, "❤️‍🔥", "👍🏽", "👨‍👩‍👧‍👦"];
        string[] fonts = [TestFonts.NotoColorEmojiRegular, TestFonts.SegoeuiEmojiFile, TestFonts.SegoeuiEmoji170File, TestFonts.TwemojiMozillaFile];
        TheoryData<string, string> cases = [];
        foreach (string sequence in sequences)
        {
            foreach (string font in fonts)
            {
                if (sequence == flag && (font == TestFonts.SegoeuiEmojiFile || font == TestFonts.SegoeuiEmoji170File))
                {
                    continue;
                }

                cases.Add(sequence, font);
            }
        }

        return cases;
    }

    private static GlyphRendererParameters[] RenderSuns(FontFamily primary, FontFamily fallback)
    {
        TextOptions options = new(primary.CreateFont(48))
        {
            FallbackFontFamilies = [fallback]
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, Suns, options);
        return renderer.GlyphKeys.Where(k => k.CodePoint == Sun).ToArray();
    }

    private static void AssertFont(FontFamily expected, GlyphRendererParameters glyph)
        => Assert.Equal(expected.Name.ToUpperInvariant(), glyph.Font);

    private static bool TryGetVariationGlyph(string fontFile, int selector, out uint glyph)
    {
        using Blob blob = Blob.FromFile(fontFile);
        using HBFace face = new(blob, 0);
        using HBFont font = new(face);
        return font.TryGetVariationGlyph((uint)Sun.Value, (uint)selector, out glyph);
    }
}
