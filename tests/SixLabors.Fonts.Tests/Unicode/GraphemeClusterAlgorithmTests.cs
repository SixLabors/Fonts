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
    public class GraphemeClusterAlgorithmTests
    {
        private readonly ITestOutputHelper output;

        public GraphemeClusterAlgorithmTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void Should_Enumerate_Other()
        {
            const string text = "ABCDEFGHIJ";

            int count = 0;
            foreach (Grapheme grapheme in GraphemeClusterAlgorithm.GetGraphemes(text))
            {
                Assert.Equal(1, grapheme.Text.Length);

                count++;
            }

            Assert.Equal(10, count);
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
            var tests = new List<Test>();
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Get the line, remove comments
                var line = lines[lineNumber - 1].Split('#')[0].Trim();

                // Ignore blank/comment only lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var codePoints = new List<int>();
                var breakPoints = new List<int>();

                // Parse the test
                var p = 0;
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

                    var codePointStr = line.Substring(codePointPos, p - codePointPos);
                    var codePoint = Convert.ToInt32(codePointStr, 16);
                    codePoints.Add(codePoint);
                }

                // Create test
                tests.Add(new Test(lineNumber, codePoints.ToArray(), breakPoints.ToArray()));
            }

            var foundBreaks = new List<int>
            {
                Capacity = 100
            };

            for (int testNumber = 0; testNumber < tests.Count; testNumber++)
            {
                Test t = tests[testNumber];

                foundBreaks.Clear();

                var text = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(t.CodePoints).ToArray());

                // Run the algorithm
                foreach (int boundary in GraphemeClusterAlgorithm.GetBoundaries(text))
                {
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
