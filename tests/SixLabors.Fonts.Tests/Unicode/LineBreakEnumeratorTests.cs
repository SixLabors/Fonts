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
        List<LineBreak> breaks1 = [];

        foreach (LineBreak lineBreak in new LineBreakEnumerator(text1.AsSpan()))
        {
            breaks1.Add(lineBreak);
        }

        Assert.Equal(5, breaks1[0].PositionMeasure);
        Assert.Equal(6, breaks1[0].PositionWrap);
        Assert.Equal(11, breaks1[1].PositionMeasure);
        Assert.Equal(12, breaks1[1].PositionWrap);
        Assert.Equal(16, breaks1[2].PositionMeasure);
        Assert.Equal(17, breaks1[2].PositionWrap);
        Assert.Equal(23, breaks1[3].PositionMeasure);
        Assert.Equal(23, breaks1[3].PositionWrap);

        List<LineBreak> breaks2 = [];
        foreach (LineBreak lineBreak in new LineBreakEnumerator(text2.AsSpan()))
        {
            breaks2.Add(lineBreak);
        }

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
    public void ForwardTextWithOuterWhitespace()
    {
        string text = " Apples Pears Bananas   ";
        List<LineBreak> breaks = [];

        foreach (LineBreak lineBreak in new LineBreakEnumerator(text.AsSpan()))
        {
            breaks.Add(lineBreak);
        }

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
        List<LineBreak> breaks = [];

        foreach (LineBreak lineBreak in new LineBreakEnumerator(text.AsSpan()))
        {
            breaks.Add(lineBreak);
        }

        Assert.Equal(7, breaks[0].PositionWrap);
        Assert.Equal(6, breaks[0].PositionMeasure);
        Assert.Equal(13, breaks[1].PositionWrap);
        Assert.Equal(12, breaks[1].PositionMeasure);
        Assert.Equal(20, breaks[2].PositionWrap);
        Assert.Equal(20, breaks[2].PositionMeasure);
    }

    [Fact]
    public void ICUTests() => Assert.True(this.ICUTestsImpl());

    // Contains over 7000 tests
    // https://www.unicode.org/Public/13.0.0/ucd/auxiliary/LineBreakTest.html
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

                string codePointStr = line.Substring(codePointPos, p - codePointPos);
                uint codePoint = Convert.ToUInt32(codePointStr, 16);
                codePoints.Add(codePoint);
            }

            // Create test
            Test test = new(lineNumber, codePoints.ToArray(), breakPoints.ToArray());
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
                LineBreakClass[] classes = t.CodePoints.Select(x => UnicodeData.GetLineBreakClass(x)).ToArray();

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
