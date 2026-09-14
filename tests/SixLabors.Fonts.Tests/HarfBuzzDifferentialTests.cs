// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using HarfBuzzSharp;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBFace = HarfBuzzSharp.Face;
using HBFont = HarfBuzzSharp.Font;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Differential shaping checks against HarfBuzz. Both engines shape identical font
/// bytes; glyph ids, placement offsets, and advances must match exactly. These tests are the correctness
/// gate for shaping performance work: any change to the shaping pipeline must keep
/// them green, and new benchmark scenarios must add a matching case here.
/// </summary>
public class HarfBuzzDifferentialTests
{
    public static TheoryData<string, string, bool> ShapingCases()
        => new()
        {
            // Latin: ligature opportunities and kern-sensitive pairs.
            { TestFonts.OpenSansFile, "The quick brown fox jumps over the lazy dog; fifty fluffy waffles.", false },
            { TestFonts.OpenSansFile, "AVATAR To Ya. WAV Tc office flag 1/2 fi ffl", false },
            { TestFonts.Arial, "Taumata", false },

            // Arabic: joining forms, mandatory ligatures, mark anchoring.
            { TestFonts.ArabicFontFile, "سلام عليكم ورحمة الله وبركاته لا إله إلا الله", true },
            { TestFonts.ArabicFontFile, "لآلئ", true },

            // Devanagari: conjuncts, matras, and reordering.
            { TestFonts.NotoSansDevanagariRegular, "क्षत्रिय द्वारा प्रकृति की रक्षा कर्तव्य है", false },
            { TestFonts.NotoSansDevanagariRegular, "श्रद्धांजलि", false },
            { TestFonts.SinhalaSansRegular, "\u0D9A\u0DCA\u0DC2", false },
            { TestFonts.SinhalaSansRegular, "\u0D9A\u0DCA\u200D\u0DC2", false },

            // Emoji joiner sequences: the zero width joiner and variation selector
            // participate in the fonts' sequence lookups (including contextual
            // rules whose lookahead spans them) and then render invisibly.
            { TestFonts.SegoeuiEmojiFile, "\U0001F469\U0001F3FB\u200D\U0001F91D\u200D\U0001F469\U0001F3FC", false },
            { TestFonts.NotoColorEmojiRegular, "\u2764\uFE0F\u200D\U0001F525", false },
            { TestFonts.NotoColorEmojiRegular, "\u2764\uFE0F\u200D\U0001FA79", false },

            // Emoji sequences from ImageSharp.Drawing discussion 418: the family joiner sequence,
            // the shrug with a skin tone modifier with and without the female sign and its
            // emoji presentation selector, and the sun with text and emoji presentation selectors.
            // The shaping API returns what HarfBuzz returns for a selector the font cannot
            // answer; the layout applies the request. Twemoji Mozilla has no space glyph, so
            // its hidden selectors are deleted rather than replaced by a zero-advance space.
            { TestFonts.SegoeuiEmojiFile, "👨‍👩‍👧‍👦", false },
            { TestFonts.SegoeuiEmojiFile, "🤷🏽‍♀️ 🤷🏽", false },
            { TestFonts.SegoeuiEmojiFile, "☀︎ ☀️", false },
            { TestFonts.NotoColorEmojiRegular, "👨‍👩‍👧‍👦", false },
            { TestFonts.NotoColorEmojiRegular, "🤷🏽‍♀️ 🤷🏽", false },
            { TestFonts.NotoColorEmojiRegular, "☀︎ ☀️", false },
            { TestFonts.TwemojiMozillaFile, "☀︎ ☀️", false },

            // Joiners inside shaping contexts: the joiner must steer the shaping
            // (ligature suppression/formation, joining forms, half forms) and then
            // render invisibly at zero advance.
            { TestFonts.OpenSansFile, "of\u200Cfice fi\u200Dnal fluff", false },
            { TestFonts.ArabicFontFile, "\u0644\u200C\u0627 \u0644\u200D\u0627", true },
            { TestFonts.NotoSansDevanagariRegular, "\u0915\u094D\u200D\u0937 \u0915\u094D\u200C\u0937", false },
        };

    [Theory]
    [MemberData(nameof(ShapingCases))]
    public void ShapesIdenticallyToHarfBuzz(string fontFile, string text, bool rightToLeft)
    {
        using Blob blob = Blob.FromFile(fontFile);
        using HBFace face = new(blob, 0);
        using HBFont hbFont = new(face);
        hbFont.SetFunctionsOpenType();

        // Using the em size keeps HarfBuzz's integer positions exact while exercising
        // the public contract that shaping is scaled to the supplied font size.
        int shapingSize = (int)face.UnitsPerEm;
        hbFont.SetScale(shapingSize, shapingSize);

        Font font = new FontCollection().Add(fontFile).CreateFont(shapingSize);
        TextShapingBuffer shapingBuffer = new();
        shapingBuffer.Add(text);
        shapingBuffer.TextDirection = rightToLeft ? TextDirection.RightToLeft : TextDirection.LeftToRight;

        TextShaper.ShapeRun(font, shapingBuffer);

        ReadOnlySpan<ShapedGlyph> glyphs = shapingBuffer.Glyphs;

        // Both engines hand back a run in the order it is read, so the two are
        // compared position for position without transforming either result.
        using HBBuffer buffer = new();
        buffer.AddUtf16(text);
        buffer.GuessSegmentProperties();
        hbFont.Shape(buffer);

        GlyphInfo[] infos = buffer.GetGlyphInfoSpan().ToArray();
        GlyphPosition[] positions = buffer.GetGlyphPositionSpan().ToArray();

        uint[] expectedGlyphIds = Array.ConvertAll(infos, x => x.Codepoint);
        uint[] actualGlyphIds = glyphs.ToArray().Select(x => (uint)x.GlyphId).ToArray();
        Assert.Equal(expectedGlyphIds, actualGlyphIds);

        for (int i = 0; i < glyphs.Length; i++)
        {
            Assert.Equal(infos[i].Codepoint, glyphs[i].GlyphId);
            Assert.Equal(positions[i].XAdvance, glyphs[i].AdvanceWidth);
            Assert.Equal(positions[i].XOffset, glyphs[i].Offset.X);
            Assert.Equal(positions[i].YOffset, glyphs[i].Offset.Y);
        }
    }

    /// <summary>
    /// Verifies one font run used by the browser tracking fixture against HarfBuzz directly.
    /// </summary>
    /// <param name="fontFile">The font file used by both shaping engines.</param>
    /// <param name="text">The exact text rendered with the supplied font.</param>
    /// <param name="textDirection">The directional-run contract for the script.</param>
    /// <param name="layoutMode">The SixLabors layout mode selecting the shaping orientation.</param>
    internal static void AssertBrowserFixtureRunMatchesHarfBuzz(string fontFile, string text, TextDirection textDirection, LayoutMode layoutMode)
    {
        using Blob blob = Blob.FromFile(fontFile);
        using HBFace face = new(blob, 0);
        using HBFont hbFont = new(face);
        hbFont.SetFunctionsOpenType();

        int shapingSize = (int)face.UnitsPerEm;
        hbFont.SetScale(shapingSize, shapingSize);

        Font font = new FontCollection().Add(fontFile).CreateFont(shapingSize);

        // Upright vertical text follows the vertical inline axis. Horizontal and
        // mixed vertical text retain the resolved direction of the script run.
        Direction harfBuzzDirection = layoutMode.IsVertical()
            ? textDirection == TextDirection.LeftToRight ? Direction.TopToBottom : Direction.BottomToTop
            : textDirection == TextDirection.LeftToRight ? Direction.LeftToRight : Direction.RightToLeft;

        using HBBuffer buffer = new();
        buffer.AddUtf16(text);
        buffer.Direction = harfBuzzDirection;
        buffer.GuessSegmentProperties();
        hbFont.Shape(buffer);

        ReadOnlySpan<GlyphInfo> infos = buffer.GetGlyphInfoSpan();
        ReadOnlySpan<GlyphPosition> positions = buffer.GetGlyphPositionSpan();
        uint[] expectedGlyphIds = infos.ToArray().Select(x => x.Codepoint).ToArray();

        TextShapingBuffer shapingBuffer = new()
        {
            LayoutMode = layoutMode,
            TextDirection = textDirection
        };

        shapingBuffer.Add(text);

        // Layout has already split the paragraph into font and directional runs at
        // this boundary, so ShapeRun exercises the same shaping contract it consumes.
        TextShaper.ShapeRun(font, shapingBuffer);

        ReadOnlySpan<ShapedGlyph> glyphs = shapingBuffer.Glyphs;
        Assert.Equal(infos.Length, glyphs.Length);

        // Compare the complete visual-order stream first so a substitution or
        // reordering error reports the full shaped result rather than one glyph.
        uint[] actualGlyphIds = glyphs.ToArray().Select(x => (uint)x.GlyphId).ToArray();
        Assert.Equal(expectedGlyphIds, actualGlyphIds);

        for (int i = 0; i < glyphs.Length; i++)
        {
            Assert.Equal(infos[i].Codepoint, glyphs[i].GlyphId);
            Assert.Equal(positions[i].XAdvance, glyphs[i].AdvanceWidth);
            Assert.Equal(positions[i].YAdvance, glyphs[i].AdvanceHeight);
            Assert.Equal(positions[i].XOffset, glyphs[i].Offset.X);
            Assert.Equal(positions[i].YOffset, glyphs[i].Offset.Y);
        }
    }
}
