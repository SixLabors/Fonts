// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;
using System.Text;
using SixLabors.Fonts.Unicode;
using Xunit.Abstractions;

namespace SixLabors.Fonts.Tests.Unicode;

public class GraphemeEnumeratorTests
{
    private readonly ITestOutputHelper output;

    public GraphemeEnumeratorTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void CanEnumerateSpan()
    {
        // Test example taken from.
        // https://docs.microsoft.com/en-us/dotnet/api/system.text.rune?view=net-5.0#when-to-use-the-rune-type
        // This Osage sample includes supplementary-plane scalars encoded as
        // surrogate pairs in UTF-16.
        const string text = "𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟";
        int count = 0;
        Span<char> span = text.ToCharArray();
        foreach (GraphemeCluster grapheme in span.EnumerateGraphemes())
        {
            Assert.True(grapheme.Utf16Length > 0);
            count++;
        }

        Assert.Equal(9, count);
    }

    [Fact]
    public void CanEnumerateReadonlySpan()
    {
        // Test example taken from.
        // https://docs.microsoft.com/en-us/dotnet/api/system.text.rune?view=net-5.0#when-to-use-the-rune-type
        // This Osage sample includes supplementary-plane scalars encoded as
        // surrogate pairs in UTF-16.
        const string text = "𐓏𐓘𐓻𐓘𐓻𐓟 𐒻𐓟";
        int count = 0;

        foreach (GraphemeCluster grapheme in text.AsSpan().EnumerateGraphemes())
        {
            Assert.True(grapheme.Utf16Length > 0);
            count++;
        }

        Assert.Equal(9, count);
    }

    [Fact]
    public void CanEnumerateInvalidReadonlySpan()
    {
        // The string below contains 2 combining characters then
        // a single high surrogate code unit, then 2 more sets or combining characters.
        // 'a' with U+0304 COMBINING MACRON and U+0308 COMBINING DIAERESIS,
        // then 'b', an unmatched high surrogate, and 'c' with U+0327 COMBINING CEDILLA.
        const string text = "a\u0304\u0308b\ud800c\u0327";
        int count = 0;
        foreach (GraphemeCluster grapheme in text.AsSpan().EnumerateGraphemes())
        {
            Assert.True(grapheme.Utf16Length > 0);
            count++;
        }

        Assert.Equal(4, count);
    }

    [Fact]
    public void Should_Enumerate_Emoji()
    {
        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction#example-count-char-rune-and-text-element-instances
        // U+1F469 WOMAN, U+1F3FD EMOJI MODIFIER FITZPATRICK TYPE-4,
        // U+200D ZERO WIDTH JOINER, U+1F692 FIRE ENGINE.
        const string text = "👩🏽‍🚒";

        int count = 0;
        foreach (GraphemeCluster grapheme in new SpanGraphemeEnumerator(text.AsSpan()))
        {
            Assert.Equal(4, grapheme.CodePointCount);
            Assert.Equal(7, grapheme.Utf16Length);
            Assert.Equal(2, grapheme.TerminalCellWidth);
            Assert.NotEqual(GraphemeClusterFlags.None, grapheme.Flags & GraphemeClusterFlags.ContainsEmoji);
            Assert.NotEqual(GraphemeClusterFlags.None, grapheme.Flags & GraphemeClusterFlags.ContainsZwjSequence);
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public void Should_Enumerate_Alpha()
    {
        const string text = "ABCDEFGHIJ";
        int count = 0;

        foreach (GraphemeCluster grapheme in new SpanGraphemeEnumerator(text.AsSpan()))
        {
            Assert.Equal(1, grapheme.CodePointCount);
            Assert.Equal(1, grapheme.Utf16Length);
            Assert.Equal(1, grapheme.TerminalCellWidth);
            Assert.NotEqual(GraphemeClusterFlags.None, grapheme.Flags & GraphemeClusterFlags.IsSingleCodePoint);
            count++;
        }

        Assert.Equal(10, count);
    }

    [Fact]
    public void Should_Return_Grapheme_Metadata()
    {
        // U+754C CJK UNIFIED IDEOGRAPH-754C is wide; U+0301 COMBINING ACUTE
        // ACCENT belongs to the preceding 'e' cluster.
        const string text = "A界e\u0301";
        SpanGraphemeEnumerator enumerator = new(text.AsSpan());

        Assert.True(enumerator.MoveNext());
        GraphemeCluster ascii = enumerator.Current;
        Assert.Equal("A", ascii.Span.ToString());
        Assert.Equal(0, ascii.Utf16Offset);
        Assert.Equal(1, ascii.Utf16Length);
        Assert.Equal(1, ascii.CodePointCount);
        Assert.Equal(1, ascii.TerminalCellWidth);
        Assert.Equal(new CodePoint('A'), ascii.FirstCodePoint);
        Assert.NotEqual(GraphemeClusterFlags.None, ascii.Flags & GraphemeClusterFlags.IsSingleCodePoint);

        Assert.True(enumerator.MoveNext());
        GraphemeCluster wide = enumerator.Current;
        Assert.Equal("界", wide.Span.ToString());
        Assert.Equal(1, wide.Utf16Offset);
        Assert.Equal(1, wide.Utf16Length);
        Assert.Equal(1, wide.CodePointCount);
        Assert.Equal(2, wide.TerminalCellWidth);
        Assert.NotEqual(GraphemeClusterFlags.None, wide.Flags & GraphemeClusterFlags.ContainsWide);

        Assert.True(enumerator.MoveNext());
        GraphemeCluster combining = enumerator.Current;

        // U+0301 COMBINING ACUTE ACCENT is part of the same grapheme cluster as 'e'.
        Assert.Equal("e\u0301", combining.Span.ToString());
        Assert.Equal(2, combining.Utf16Offset);
        Assert.Equal(2, combining.Utf16Length);
        Assert.Equal(2, combining.CodePointCount);
        Assert.Equal(1, combining.TerminalCellWidth);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Should_Apply_Terminal_Width_Options()
    {
        TerminalWidthOptions options = new()
        {
            AmbiguousWidth = TerminalAmbiguousWidth.Wide,
            ControlCharacterWidth = TerminalControlCharacterWidth.Zero
        };

        // U+00A1 INVERTED EXCLAMATION MARK has East_Asian_Width=A; U+000A LINE
        // FEED exercises the configured control-character policy.
        SpanGraphemeEnumerator enumerator = new("\u00A1\n".AsSpan(), options);

        Assert.True(enumerator.MoveNext());
        GraphemeCluster ambiguous = enumerator.Current;
        Assert.Equal(2, ambiguous.TerminalCellWidth);
        Assert.NotEqual(GraphemeClusterFlags.None, ambiguous.Flags & GraphemeClusterFlags.ContainsAmbiguous);
        Assert.NotEqual(GraphemeClusterFlags.None, ambiguous.Flags & GraphemeClusterFlags.ContainsWide);

        Assert.True(enumerator.MoveNext());
        GraphemeCluster control = enumerator.Current;
        Assert.Equal(0, control.TerminalCellWidth);
        Assert.NotEqual(GraphemeClusterFlags.None, control.Flags & GraphemeClusterFlags.ContainsControl);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Should_Allow_Emoji_Override_To_Be_Disabled()
    {
        TerminalWidthOptions options = new()
        {
            EmojiWidth = TerminalEmojiWidth.EastAsianWidth
        };

        // U+2640 FEMALE SIGN followed by U+FE0F VARIATION SELECTOR-16.
        SpanGraphemeEnumerator enumerator = new("♀️".AsSpan(), options);

        Assert.True(enumerator.MoveNext());
        GraphemeCluster emojiPresentation = enumerator.Current;
        Assert.Equal(1, emojiPresentation.TerminalCellWidth);
        Assert.NotEqual(GraphemeClusterFlags.None, emojiPresentation.Flags & GraphemeClusterFlags.ContainsEmoji);
        Assert.NotEqual(GraphemeClusterFlags.None, emojiPresentation.Flags & GraphemeClusterFlags.ContainsVariationSelector);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Should_Resolve_Emoji_Presentation_And_Zwj_Clusters_As_Wide_By_Default()
    {
        // U+2640 FEMALE SIGN with U+FE0F VARIATION SELECTOR-16, followed by
        // U+1F469 WOMAN, U+1F3FD EMOJI MODIFIER FITZPATRICK TYPE-4,
        // U+200D ZERO WIDTH JOINER, U+1F692 FIRE ENGINE.
        SpanGraphemeEnumerator enumerator = new("♀️👩🏽‍🚒".AsSpan());

        Assert.True(enumerator.MoveNext());
        GraphemeCluster emojiPresentation = enumerator.Current;
        Assert.Equal(2, emojiPresentation.TerminalCellWidth);
        Assert.NotEqual(GraphemeClusterFlags.None, emojiPresentation.Flags & GraphemeClusterFlags.ContainsEmoji);
        Assert.NotEqual(GraphemeClusterFlags.None, emojiPresentation.Flags & GraphemeClusterFlags.ContainsVariationSelector);

        Assert.True(enumerator.MoveNext());
        GraphemeCluster zwj = enumerator.Current;
        Assert.Equal(2, zwj.TerminalCellWidth);
        Assert.Equal(4, zwj.CodePointCount);
        Assert.NotEqual(GraphemeClusterFlags.None, zwj.Flags & GraphemeClusterFlags.ContainsEmoji);
        Assert.NotEqual(GraphemeClusterFlags.None, zwj.Flags & GraphemeClusterFlags.ContainsZwjSequence);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Should_Not_Widen_Non_Emoji_Variation_Selector_Sequence()
    {
        // U+FE0F VARIATION SELECTOR-16 only requests emoji presentation for
        // scalars that Unicode defines as valid emoji presentation sequence bases.
        SpanGraphemeEnumerator enumerator = new("A\uFE0F".AsSpan());

        Assert.True(enumerator.MoveNext());
        GraphemeCluster cluster = enumerator.Current;
        Assert.Equal(1, cluster.TerminalCellWidth);
        Assert.Equal(2, cluster.CodePointCount);
        Assert.NotEqual(GraphemeClusterFlags.None, cluster.Flags & GraphemeClusterFlags.ContainsVariationSelector);
        Assert.Equal(GraphemeClusterFlags.None, cluster.Flags & GraphemeClusterFlags.ContainsEmoji);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Should_Resolve_Keycap_Sequence_As_Wide_By_Default()
    {
        // U+0023 NUMBER SIGN, U+FE0F VARIATION SELECTOR-16,
        // U+20E3 COMBINING ENCLOSING KEYCAP.
        SpanGraphemeEnumerator enumerator = new("#\uFE0F\u20E3".AsSpan());

        Assert.True(enumerator.MoveNext());
        GraphemeCluster cluster = enumerator.Current;
        Assert.Equal(2, cluster.TerminalCellWidth);
        Assert.Equal(3, cluster.CodePointCount);
        Assert.NotEqual(GraphemeClusterFlags.None, cluster.Flags & GraphemeClusterFlags.ContainsEmoji);
        Assert.NotEqual(GraphemeClusterFlags.None, cluster.Flags & GraphemeClusterFlags.ContainsVariationSelector);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Should_Respect_Text_Presentation_Selector()
    {
        // U+2640 FEMALE SIGN followed by U+FE0E VARIATION SELECTOR-15 requests
        // text presentation, so the emoji-wide override should not apply.
        SpanGraphemeEnumerator enumerator = new("\u2640\uFE0E".AsSpan());

        Assert.True(enumerator.MoveNext());
        GraphemeCluster cluster = enumerator.Current;
        Assert.Equal(1, cluster.TerminalCellWidth);
        Assert.Equal(2, cluster.CodePointCount);
        Assert.NotEqual(GraphemeClusterFlags.None, cluster.Flags & GraphemeClusterFlags.ContainsEmoji);
        Assert.NotEqual(GraphemeClusterFlags.None, cluster.Flags & GraphemeClusterFlags.ContainsVariationSelector);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Should_Treat_Null_As_Zero_Width()
    {
        // U+0000 NULL is not printable, but terminal width implementations
        // normally treat it as zero instead of applying the control policy.
        SpanGraphemeEnumerator enumerator = new("\0".AsSpan());

        Assert.True(enumerator.MoveNext());
        GraphemeCluster cluster = enumerator.Current;
        Assert.Equal(0, cluster.TerminalCellWidth);
        Assert.Equal(GraphemeClusterFlags.AllZeroWidth | GraphemeClusterFlags.IsSingleCodePoint, cluster.Flags);
    }

    [Theory]
    [MemberData(nameof(TerminalWidthData))]
    public void Should_Return_Terminal_Width_For_Common_Cases(string text, int expectedWidth, int[] expectedClusterWidths)
    {
        List<int> clusterWidths = [];
        int width = 0;

        foreach (GraphemeCluster grapheme in text.AsSpan().EnumerateGraphemes())
        {
            int terminalCellWidth = grapheme.TerminalCellWidth;
            clusterWidths.Add(terminalCellWidth);

            if (terminalCellWidth < 0)
            {
                width = -1;
                break;
            }

            width += terminalCellWidth;
        }

        Assert.Equal(expectedClusterWidths, clusterWidths);
        Assert.Equal(expectedWidth, width);
    }

    [Fact]
    public void ICUTests() => Assert.True(this.ICUTestsImpl());

    public bool ICUTestsImpl()
    {
        this.output.WriteLine("Grapheme Cluster Tests");
        this.output.WriteLine("----------------------");

        // Read the test file
        string[] lines = File.ReadAllLines(Path.Combine(TestEnvironment.UnicodeTestDataFullPath, "GraphemeBreakTest.txt"));

        // Process each line
        List<Test> tests = new();
        for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
        {
            // Get the line, remove comments
            string line = lines[lineNumber - 1].Split('#')[0].Trim();

            // Ignore blank/comment only lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            List<uint> codePoints = new();
            List<int> breakPoints = new();

            // Parse the test
            int p = 0;
            while (p < line.Length)
            {
                // Ignore white space
                if (char.IsWhiteSpace(line[p]))
                {
                    p++;
                    continue;
                }

                if (line[p] == '×')
                {
                    p++;
                    continue;
                }

                if (line[p] == '÷')
                {
                    breakPoints.Add(codePoints.Count);
                    p++;
                    continue;
                }

                int codePointPos = p;
                while (p < line.Length && IsHexDigit(line[p]))
                {
                    p++;
                }

                string codePointStr = line[codePointPos..p];
                uint codePoint = Convert.ToUInt32(codePointStr, 16);
                codePoints.Add(codePoint);
            }

            // Create test
            tests.Add(new Test(lineNumber, [.. codePoints], [.. breakPoints]));
        }

        List<int> foundBreaks = new()
        {
            Capacity = 100
        };

        for (int testNumber = 0; testNumber < tests.Count; testNumber++)
        {
            Test t = tests[testNumber];

            foundBreaks.Clear();

            string text = Encoding.UTF32.GetString(MemoryMarshal.Cast<uint, byte>(t.CodePoints).ToArray());

            // Always a leading boundary
            int boundary = 0;
            foundBreaks.Add(boundary);

            // Run the algorithm
            foreach (GraphemeCluster grapheme in text.AsSpan().EnumerateGraphemes())
            {
                boundary += grapheme.CodePointCount;
                foundBreaks.Add(boundary);
            }

            // Check the same
            bool pass = true;
            if (foundBreaks.Count != t.BreakPoints.Length)
            {
                pass = false;
            }
            else
            {
                for (int i = 0; i < foundBreaks.Count; i++)
                {
                    if (foundBreaks[i] != t.BreakPoints[i])
                    {
                        pass = false;
                    }
                }
            }

            if (!pass)
            {
                this.output.WriteLine($"Failed test on line {t.LineNumber}");
                this.output.WriteLine($"    Code Points: {string.Join(" ", t.CodePoints)}");
                this.output.WriteLine($"Expected Breaks: {string.Join(" ", t.BreakPoints)}");
                this.output.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
                this.output.WriteLine($"     Char Props: {string.Join(" ", t.CodePoints.Select(x => UnicodeData.GetGraphemeClusterClass(x)))}");
                return false;
            }
        }

        return true;
    }

    private static bool IsHexDigit(char ch)
        => char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');

    public static TheoryData<string, int, int[]> TerminalWidthData()
        => new()
        {
            { string.Empty, 0, [] },

            // U+0000 NULL contributes no cells inside otherwise printable text.
            { "hello\0world", 10, [1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1] },

            // Katakana characters are East_Asian_Width=W and occupy two cells each.
            { "コンニチハ, セカイ!", 19, [2, 2, 2, 2, 2, 1, 1, 2, 2, 2, 1] },

            // U+0000 NULL contributes no cells inside otherwise printable text.
            { "abc\0def", 6, [1, 1, 1, 0, 1, 1, 1] },

            // U+001B ESCAPE is a non-printable control character.
            { "\x1b[0m", -1, [-1] },

            // U+05BF HEBREW POINT RAFE is combining and contributes no terminal cell.
            { "--\u05bf--", 4, [1, 1, 1, 1] },

            // U+0301 COMBINING ACUTE ACCENT belongs to the preceding 'e' cluster.
            { "cafe\u0301", 4, [1, 1, 1, 1] },

            // U+0410 CYRILLIC CAPITAL LETTER A with U+0488 COMBINING CYRILLIC
            // HUNDRED THOUSANDS SIGN.
            { "\u0410\u0488", 1, [1] },

            // U+1B13 BALINESE LETTER KA, U+1B28 BALINESE LETTER PA KAPAL,
            // U+1B2E BALINESE LETTER LA, U+1B44 BALINESE ADEG ADEG.
            { "\u1B13\u1B28\u1B2E\u1B44", 3, [1, 1, 1] },

            // U+1100 HANGUL CHOSEONG KIYEOK with U+1161 HANGUL JUNGSEONG A.
            { "\u1100\u1161", 2, [2] },

            // U+1100 HANGUL CHOSEONG KIYEOK with U+1160 HANGUL JUNGSEONG FILLER.
            { "\u1100\u1160", 2, [2] },

            // U+0915 DEVANAGARI LETTER KA, U+094D DEVANAGARI SIGN VIRAMA,
            // U+0937 DEVANAGARI LETTER SSA, U+093F DEVANAGARI VOWEL SIGN I.
            { "\u0915\u094D\u0937\u093F", 1, [1] },

            // U+0B95 TAMIL LETTER KA, U+0BCD TAMIL SIGN VIRAMA,
            // U+0BB7 TAMIL LETTER SSA, U+0BCC TAMIL VOWEL SIGN AU.
            { "\u0b95\u0bcd\u0bb7\u0bcc", 2, [1, 1] },

            // U+0CB0 KANNADA LETTER RA, U+0CCD KANNADA SIGN VIRAMA,
            // U+0C9D KANNADA LETTER JHA, U+0CC8 KANNADA VOWEL SIGN AI.
            { "\u0cb0\u0ccd\u0c9d\u0cc8", 2, [1, 1] },

            // U+0CB0 KANNADA LETTER RA, U+0CBC KANNADA SIGN NUKTA,
            // U+0CCD KANNADA SIGN VIRAMA, U+0C9A KANNADA LETTER CA.
            { "\u0cb0\u0cbc\u0ccd\u0c9a", 2, [1, 1] },

            // U+3029 HANGZHOU NUMERAL NINE remains wide; U+302A IDEOGRAPHIC
            // LEVEL TONE MARK is combining and contributes no terminal cell.
            { "\u3029", 2, [2] },
            { "\u302A", 0, [0] },

            // U+3099 and U+309A are combining Katakana-Hiragana sound marks;
            // U+309B is the spacing voiced sound mark and remains wide.
            { "\u3099", 0, [0] },
            { "\u309A", 0, [0] },
            { "\u309B", 2, [2] },

            // U+00AD SOFT HYPHEN is format data, but terminals commonly reserve
            // one cell for it rather than treating it as a combining mark.
            { "\u00AD", 1, [1] },

            // U+1F469 WOMAN, U+1F3FB EMOJI MODIFIER FITZPATRICK TYPE-1-2,
            // U+200D ZERO WIDTH JOINER, U+1F4BB PERSONAL COMPUTER.
            { "\U0001f469\U0001f3fb\u200d\U0001f4bb", 2, [2] },

            // U+1F469 WOMAN, U+1F3FB EMOJI MODIFIER FITZPATRICK TYPE-1-2,
            // U+200D ZERO WIDTH JOINER. Terminals generally keep the started
            // emoji cluster wide even when the ZWJ sequence is unfinished.
            { "\U0001f469\U0001f3fb\u200d", 2, [2] },

            // U+26F9 PERSON WITH BALL, U+1F3FB EMOJI MODIFIER FITZPATRICK TYPE-1-2,
            // U+200D ZERO WIDTH JOINER, U+2640 FEMALE SIGN,
            // U+FE0F VARIATION SELECTOR-16.
            { "\u26F9\U0001F3FB\u200D\u2640\uFE0F", 2, [2] },

            // U+2640 FEMALE SIGN followed by U+FE0F VARIATION SELECTOR-16 requests
            // emoji presentation and is treated as wide by terminal policy.
            { "\u2640\uFE0F", 2, [2] },

            // U+2640 FEMALE SIGN followed by U+FE0E VARIATION SELECTOR-15 requests
            // text presentation and suppresses emoji-wide terminal handling.
            { "\u2640\uFE0E", 1, [1] },

            // U+0023 NUMBER SIGN, U+FE0F VARIATION SELECTOR-16,
            // U+20E3 COMBINING ENCLOSING KEYCAP.
            { "#\uFE0F\u20E3", 2, [2] }
        };

    private readonly struct Test
    {
        public Test(int lineNumber, uint[] codePoints, int[] breakPoints)
        {
            this.LineNumber = lineNumber;
            this.CodePoints = codePoints;
            this.BreakPoints = breakPoints;
        }

        public int LineNumber { get; }

        public uint[] CodePoints { get; }

        public int[] BreakPoints { get; }
    }
}
