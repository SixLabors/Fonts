// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;
using System.Text;
using SixLabors.Fonts.Unicode;
using Xunit.Abstractions;

namespace SixLabors.Fonts.Tests.Unicode;

public class WordBreakEnumeratorTests
{
    private readonly ITestOutputHelper output;

    public WordBreakEnumeratorTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void EnumerateWordSegments_ReturnsDefaultWordBoundarySegments()
    {
        string text = "can't stop";
        List<string> segments = [];

        // The apostrophe stays inside the word because UAX #29 treats it as
        // mid-letter punctuation when letters appear on both sides.
        foreach (WordSegment segment in text.AsSpan().EnumerateWordSegments())
        {
            segments.Add(segment.Span.ToString());
        }

        Assert.Equal(["can't", " ", "stop"], segments);
    }

    [Fact]
    public void EnumerateWordSegments_KeepsEmojiZwjSequenceTogether()
    {
        string text = "a👩🏽‍🚒b";
        List<string> segments = [];

        // WB3c keeps the emoji ZWJ sequence together as one word-boundary segment.
        foreach (WordSegment segment in text.AsSpan().EnumerateWordSegments())
        {
            segments.Add(segment.Span.ToString());
        }

        Assert.Equal(["a", "👩🏽‍🚒", "b"], segments);
    }

    [Fact]
    public void ICUTests() => Assert.True(this.ICUTestsImpl());

    public bool ICUTestsImpl()
    {
        this.output.WriteLine("Word Break Tests");
        this.output.WriteLine("----------------");

        string[] lines = File.ReadAllLines(Path.Combine(TestEnvironment.UnicodeTestDataFullPath, "WordBreakTest.txt"));

        List<Test> tests = [];
        for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
        {
            string line = lines[lineNumber - 1].Split('#')[0].Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            List<uint> codePoints = [];
            List<int> breakPoints = [];

            // The Unicode test file represents expected boundaries with ÷ and
            // non-boundaries with ×, so the parser records only scalar values
            // and the code point offsets where a break must be produced.
            int p = 0;
            while (p < line.Length)
            {
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

            Test test = new(lineNumber, [.. codePoints], [.. breakPoints]);
            tests.Add(test);
        }

        List<int> foundBreaks = [];

        for (int testNumber = 0; testNumber < tests.Count; testNumber++)
        {
            Test t = tests[testNumber];

            foundBreaks.Clear();

            string text = Encoding.UTF32.GetString(MemoryMarshal.Cast<uint, byte>(t.CodePoints).ToArray());

            int boundary = 0;
            foundBreaks.Add(boundary);

            foreach (WordSegment segment in text.AsSpan().EnumerateWordSegments())
            {
                boundary += segment.CodePointCount;
                foundBreaks.Add(boundary);
            }

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
                WordBreakClass[] classes = [.. t.CodePoints.Select(UnicodeData.GetWordBreakClass)];

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

    private sealed class Test
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
