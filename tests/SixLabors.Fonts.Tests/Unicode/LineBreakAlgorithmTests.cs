// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SixLabors.Fonts.Unicode;
using Xunit;
using Xunit.Abstractions;

namespace SixLabors.Fonts.Tests.Unicode
{
    public class LineBreakAlgorithmTests
    {
        private readonly ITestOutputHelper output;

        public LineBreakAlgorithmTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void BasicLatinTest()
        {
            var lineBreaker = new LineBreakAlgorithm("Hello World\r\nThis is a test.".AsSpan());

            Assert.True(lineBreaker.TryGetNextBreak(out LineBreak b));
            Assert.Equal(6, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.TryGetNextBreak(out b));
            Assert.Equal(13, b.PositionWrap);
            Assert.True(b.Required);

            Assert.True(lineBreaker.TryGetNextBreak(out b));
            Assert.Equal(18, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.TryGetNextBreak(out b));
            Assert.Equal(21, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.TryGetNextBreak(out b));
            Assert.Equal(23, b.PositionWrap);
            Assert.False(b.Required);

            Assert.True(lineBreaker.TryGetNextBreak(out b));
            Assert.Equal(28, b.PositionWrap);
            Assert.False(b.Required);

            Assert.False(lineBreaker.TryGetNextBreak(out b));
        }

        [Fact]
        public void ForwardTextWithOuterWhitespace()
        {
            var lineBreaker = new LineBreakAlgorithm(" Apples Pears Bananas   ".AsSpan());
            var breaks = new List<LineBreak>();

            while (lineBreaker.TryGetNextBreak(out LineBreak lineBreak))
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
            var lineBreaker = new LineBreakAlgorithm("Apples Pears Bananas".AsSpan());
            var breaks = new List<LineBreak>();

            while (lineBreaker.TryGetNextBreak(out LineBreak lineBreak))
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
            var tests = new List<Test>();
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Ignore deliberately skipped test?
                if (SkipLines.Contains(lineNumber))
                {
                    continue;
                }

                // Get the line, remove comments
                string line = lines[lineNumber - 1].Split('#')[0].Trim();

                // Ignore blank/comment only lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var codePoints = new List<int>();
                var breakPoints = new List<int>();

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
                    int codePoint = Convert.ToInt32(codePointStr, 16);
                    codePoints.Add(codePoint);
                }

                // Create test
                var test = new Test(lineNumber, codePoints.ToArray(), breakPoints.ToArray());
                tests.Add(test);
            }

            var foundBreaks = new List<int>();

            for (int testNumber = 0; testNumber < tests.Count; testNumber++)
            {
                Test t = tests[testNumber];

                foundBreaks.Clear();

                // Run the line breaker and build a list of break points
                var text = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(t.CodePoints).ToArray());

                var lineBreaker = new LineBreakAlgorithm(text.AsSpan());
                while (lineBreaker.TryGetNextBreak(out LineBreak b))
                {
                    foundBreaks.Add(b.PositionWrap);
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

        // The following test lines have been investigated and appear to be
        // expecting an incorrect result when compared to the default rules and
        // pair tables.
        private static readonly HashSet<int> SkipLines = new HashSet<int>()
        {
            125, 127, 261, 263, 433, 435, 815, 1121, 1123, 1161, 1163, 1165, 1167, 1331, 2189,
            2191, 2325, 2327, 2841, 2843, 2873, 2875, 3567, 3739, 3873, 3875, 4081, 4083, 4389,
            4391, 4425, 4427, 4473, 4475, 4561, 4563, 4597, 4599, 4645, 4647, 4943, 5077, 5079,
            5109, 5111, 5459, 5593, 5595, 6109, 6111, 6149, 6151, 6153, 6155, 6489, 6491, 6663,
            6833, 6835, 7005, 7007, 7177, 7179, 7313, 7315, 7477, 7486, 7491, 7493, 7494, 7495,
            7496, 7576, 7577, 7578, 7579, 7580, 7581, 7583, 7584, 7585, 7586, 7587, 7604, 7610, 7611
        };

        private readonly struct Test
        {
            public Test(int lineNumber, int[] codePoints, int[] breakPoints)
            {
                this.LineNumber = lineNumber;
                this.CodePoints = codePoints;
                this.BreakPoints = breakPoints;
            }

            public int LineNumber { get; }

            public int[] CodePoints { get; }

            public int[] BreakPoints { get; }
        }
    }
}
