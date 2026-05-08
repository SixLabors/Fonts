// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;
using System.Text;
using SixLabors.Fonts.Unicode;
using Xunit.Abstractions;

namespace SixLabors.Fonts.Tests.Unicode;

public class LineBreakEnumeratorTests
{
    private readonly ITestOutputHelper output;

    public LineBreakEnumeratorTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void BasicLatinTest()
    {
        LineBreakEnumerator lineBreaker = new("Hello World\r\nThis is a test.".AsSpan());

        Assert.True(lineBreaker.MoveNext());
        Assert.Equal(6, lineBreaker.Current.PositionWrap);
        Assert.False(lineBreaker.Current.Required);

        Assert.True(lineBreaker.MoveNext());
        Assert.Equal(13, lineBreaker.Current.PositionWrap);
        Assert.True(lineBreaker.Current.Required);

        Assert.True(lineBreaker.MoveNext());
        Assert.Equal(18, lineBreaker.Current.PositionWrap);
        Assert.False(lineBreaker.Current.Required);

        Assert.True(lineBreaker.MoveNext());
        Assert.Equal(21, lineBreaker.Current.PositionWrap);
        Assert.False(lineBreaker.Current.Required);

        Assert.True(lineBreaker.MoveNext());
        Assert.Equal(23, lineBreaker.Current.PositionWrap);
        Assert.False(lineBreaker.Current.Required);

        Assert.True(lineBreaker.MoveNext());
        Assert.Equal(28, lineBreaker.Current.PositionWrap);
        Assert.False(lineBreaker.Current.Required);

        Assert.False(lineBreaker.MoveNext());
    }

    [Fact]
    public void NumericTests()
    {
        const string text1 = "Super Smash Bros (1999)";
        const string text2 = "Super, Smash Bros (1999)";
        List<LineBreak> breaks1 = [.. new LineBreakEnumerator(text1.AsSpan())];

        Assert.Equal(5, breaks1[0].PositionMeasure);
        Assert.Equal(6, breaks1[0].PositionWrap);
        Assert.Equal(11, breaks1[1].PositionMeasure);
        Assert.Equal(12, breaks1[1].PositionWrap);
        Assert.Equal(16, breaks1[2].PositionMeasure);
        Assert.Equal(17, breaks1[2].PositionWrap);
        Assert.Equal(23, breaks1[3].PositionMeasure);
        Assert.Equal(23, breaks1[3].PositionWrap);

        List<LineBreak> breaks2 = [.. new LineBreakEnumerator(text2.AsSpan())];

        Assert.Equal(6, breaks2[0].PositionMeasure);
        Assert.Equal(7, breaks2[0].PositionWrap);
        Assert.Equal(12, breaks2[1].PositionMeasure);
        Assert.Equal(13, breaks2[1].PositionWrap);
        Assert.Equal(17, breaks2[2].PositionMeasure);
        Assert.Equal(18, breaks2[2].PositionWrap);
        Assert.Equal(24, breaks2[3].PositionMeasure);
        Assert.Equal(24, breaks2[3].PositionWrap);
    }

    [Fact]
    public void NumericFractionDoesNotBreakAfterSolidus()
    {
        List<LineBreak> breaks = [.. new LineBreakEnumerator("1/2".AsSpan())];

        Assert.DoesNotContain(breaks, x => x.PositionWrap == 2);
        Assert.Equal(3, breaks[^1].PositionWrap);
    }

    [Fact]
    public void NonNumericSolidusKeepsDefaultBreakAfter()
    {
        List<LineBreak> breaks = [.. new LineBreakEnumerator("a/2".AsSpan())];

        Assert.Contains(breaks, x => x.PositionWrap == 2);
    }

    [Fact]
    public void SoftHyphenBreakIsMarkedForLayoutTailoring()
    {
        const string text = "extra\u00ADordinary";
        List<LineBreak> breaks = [.. new LineBreakEnumerator(text.AsSpan())];

        int hyphenationBreaks = 0;
        LineBreak hyphenationBreak = default;
        for (int i = 0; i < breaks.Count; i++)
        {
            if (breaks[i].IsHyphenationBreak)
            {
                hyphenationBreaks++;
                hyphenationBreak = breaks[i];
            }
        }

        // UAX #14 exposes U+00AD as a discretionary break, but layout needs to
        // distinguish it from ordinary opportunities so it can decide whether to
        // materialize a visible marker for the chosen break.
        Assert.Equal(1, hyphenationBreaks);
        Assert.False(hyphenationBreak.Required);
        Assert.Equal(6, hyphenationBreak.PositionMeasure);
        Assert.Equal(6, hyphenationBreak.PositionWrap);
    }

    [Fact]
    public void ForwardTextWithOuterWhitespace()
    {
        string text = " Apples Pears Bananas   ";
        List<LineBreak> breaks = [.. new LineBreakEnumerator(text.AsSpan())];

        Assert.Equal(1, breaks[0].PositionWrap);
        Assert.Equal(0, breaks[0].PositionMeasure);
        Assert.Equal(8, breaks[1].PositionWrap);
        Assert.Equal(7, breaks[1].PositionMeasure);
        Assert.Equal(14, breaks[2].PositionWrap);
        Assert.Equal(13, breaks[2].PositionMeasure);
        Assert.Equal(24, breaks[3].PositionWrap);
        Assert.Equal(21, breaks[3].PositionMeasure);
    }

    [Fact]
    public void ForwardTest()
    {
        string text = "Apples Pears Bananas";
        List<LineBreak> breaks = [.. new LineBreakEnumerator(text.AsSpan())];

        Assert.Equal(7, breaks[0].PositionWrap);
        Assert.Equal(6, breaks[0].PositionMeasure);
        Assert.Equal(13, breaks[1].PositionWrap);
        Assert.Equal(12, breaks[1].PositionMeasure);
        Assert.Equal(20, breaks[2].PositionWrap);
        Assert.Equal(20, breaks[2].PositionMeasure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MoveNextDoesNotAllocate(bool tailorUrls)
    {
        const string text = "Alpha beta\r\n\u200Bgamma \U0001F1FA\U0001F1F8 https://a/2024/05 123,456.";

        ConsumeLineBreaks(text.AsSpan(), tailorUrls);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int total = ConsumeLineBreaks(text.AsSpan(), tailorUrls);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.True(total > 0);
        Assert.Equal(before, after);
    }

    [Theory]
    [InlineData("http://a/2024/05")]
    [InlineData("https://a/2024/05")]
    [InlineData("www.example.com/2024/05")]
    [InlineData("(https://a/2024/05)")]
    [InlineData("<https://a/2024/05>")]
    public void UrlTailoringAllowsNumericPathSegmentBreak(string text)
    {
        List<LineBreak> breaks = [.. new LineBreakEnumerator(text.AsSpan(), tailorUrls: true)];

        Assert.Contains(breaks, x => x.PositionWrap == GetAsciiBreakAfterLastSolidus(text));
    }

    [Fact]
    public void UrlTailoringIsOptIn()
    {
        const string text = "https://a/2024/05";
        List<LineBreak> breaks = [.. new LineBreakEnumerator(text.AsSpan())];

        Assert.DoesNotContain(breaks, x => x.PositionWrap == GetAsciiBreakAfterLastSolidus(text));
    }

    [Theory]
    [InlineData("1http://a/2024/05")]
    [InlineData("awww.example.com/2024/05")]
    [InlineData("x-www.example.com/2024/05")]
    [InlineData("C:/2024/05")]
    [InlineData("/2024/05")]
    [InlineData("2024/05/06")]
    [InlineData("1/2/3")]
    [InlineData("ISO/IEC 14496/12")]
    public void UrlTailoringDoesNotCreateNumericPathSegmentBreakForNonUrls(string text)
    {
        List<LineBreak> breaks = [.. new LineBreakEnumerator(text.AsSpan(), tailorUrls: true)];

        for (int i = 0; i < text.Length - 1; i++)
        {
            if (text[i] == '/' && text[i + 1] is >= '0' and <= '9')
            {
                Assert.DoesNotContain(breaks, x => x.PositionWrap == i + 1);
            }
        }
    }

    [Fact]
    public void UrlTailoringSuppressesNonUrlSolidusBreak()
    {
        List<LineBreak> breaks = [.. new LineBreakEnumerator("a/2".AsSpan(), tailorUrls: true)];

        Assert.DoesNotContain(breaks, x => x.PositionWrap == 2);
    }

    [Fact]
    public void ICUTests() => Assert.True(this.ICUTestsImpl());

    // Unicode line break conformance tests
    // https://www.unicode.org/Public/17.0.0/ucd/auxiliary/LineBreakTest.txt
    public bool ICUTestsImpl()
    {
        this.output.WriteLine("Line Breaker Tests");
        this.output.WriteLine("------------------");

        // Read the test file
        string[] lines = File.ReadAllLines(Path.Combine(TestEnvironment.UnicodeTestDataFullPath, "LineBreakTest.txt"));

        // Process each line
        List<Test> tests = [];
        for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
        {
            // Get the line, remove comments
            string line = lines[lineNumber - 1].Split('#')[0].Trim();

            // Ignore blank/comment only lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            List<uint> codePoints = [];
            List<int> breakPoints = [];

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
            Test test = new(lineNumber, [.. codePoints], [.. breakPoints]);
            tests.Add(test);
        }

        List<int> foundBreaks = [];

        for (int testNumber = 0; testNumber < tests.Count; testNumber++)
        {
            Test t = tests[testNumber];

            foundBreaks.Clear();

            // Run the line breaker and build a list of break points
            string text = Encoding.UTF32.GetString(MemoryMarshal.Cast<uint, byte>(t.CodePoints).ToArray());

            LineBreakEnumerator enumerator = new(text.AsSpan());
            while (enumerator.MoveNext())
            {
                foundBreaks.Add(enumerator.Current.PositionWrap);
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
                LineBreakClass[] classes = [.. t.CodePoints.Select(UnicodeData.GetLineBreakClass)];

                this.output.WriteLine($"Failed test on line {t.LineNumber}");
                this.output.WriteLine($"    Code Points: {string.Join(" ", t.CodePoints)}");
                this.output.WriteLine($"Expected Breaks: {string.Join(" ", t.BreakPoints)}");
                this.output.WriteLine($"  Actual Breaks: {string.Join(" ", foundBreaks)}");
                this.output.WriteLine($"     Char Props: {string.Join(" ", classes)}");

                return false;
            }
        }

        return true;
    }

    private static bool IsHexDigit(char ch) => char.IsDigit(ch) || (ch >= 'A' && ch <= 'F') || (ch >= 'a' && ch <= 'f');

    private static int GetAsciiBreakAfterLastSolidus(string text)
    {
        int solidusIndex = text.LastIndexOf('/');
        Assert.True(solidusIndex >= 0);
        return solidusIndex + 1;
    }

    private static int ConsumeLineBreaks(ReadOnlySpan<char> text, bool tailorUrls)
    {
        int total = 0;
        LineBreakEnumerator enumerator = new(text, tailorUrls);
        while (enumerator.MoveNext())
        {
            LineBreak lineBreak = enumerator.Current;
            total += lineBreak.PositionMeasure + lineBreak.PositionWrap + (lineBreak.Required ? 1 : 0);
        }

        return total;
    }

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
