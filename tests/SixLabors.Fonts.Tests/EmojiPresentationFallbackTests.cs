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

    /// <summary>
    /// Gets the color emoji fonts in the test set: COLR v1, COLR v0 with a cmap format 14, and COLR v0.
    /// </summary>
    public static TheoryData<string> ColorEmojiFonts { get; } = new()
    {
        TestFonts.NotoColorEmojiRegular,
        TestFonts.SegoeuiEmojiFile,
        TestFonts.TwemojiMozillaFile
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

    [Fact]
    public void ArialThenSegoeUIEmoji_TextSelectorWithoutAPlainSunTakesTheBaseGlyph()
    {
        // Arial has no sun and Segoe UI Emoji has a color table, so no font answers ☀︎ and
        // the pass that ignores the selector takes the base glyph from Segoe UI Emoji.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Arial);
        FontFamily emoji = TestFonts.GetFontFamily(TestFonts.SegoeuiEmojiFile);

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

    [Fact]
    public void ArialThenSegoeUIEmoji_SmilingFaceSelectorsChooseTheFont()
    {
        // Arial maps the smiling face, and Segoe UI Emoji declares the emoji sequence in its cmap.
        FontFamily text = TestFonts.GetFontFamily(TestFonts.Arial);
        FontFamily emoji = TestFonts.GetFontFamily(TestFonts.SegoeuiEmojiFile);
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

    private static TheoryData<string, string> BuildSequenceCases()
    {
        // Keycap, flag, joiner with a selector inside, modifier, and the family joiner sequence.
        // Segoe UI Emoji draws no country flags; a regional indicator pair renders as two
        // outline letters there, so the flag is paired with the other two fonts only.
        const string flag = "🇯🇵";
        string[] sequences = ["1️⃣", flag, "❤️‍🔥", "👍🏽", "👨‍👩‍👧‍👦"];
        string[] fonts = [TestFonts.NotoColorEmojiRegular, TestFonts.SegoeuiEmojiFile, TestFonts.TwemojiMozillaFile];
        TheoryData<string, string> cases = [];
        foreach (string sequence in sequences)
        {
            foreach (string font in fonts)
            {
                if (sequence == flag && font == TestFonts.SegoeuiEmojiFile)
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
