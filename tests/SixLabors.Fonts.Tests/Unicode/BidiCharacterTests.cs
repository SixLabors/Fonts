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
    public class BidiCharacterTests
    {
        private readonly ITestOutputHelper output;

        public BidiCharacterTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void ICUTests() => Assert.True(this.ICUTestsImpl());

        private bool ICUTestsImpl()
        {
            this.output.WriteLine("Bidi Character Tests");
            this.output.WriteLine("--------------------");

            // Read the test file
            string[] lines = File.ReadAllLines(Path.Combine(TestEnvironment.UnicodeTestDataFullPath, "BidiCharacterTest.txt"));

            // Parse lines
            var tests = new List<Test>();
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Get the line, remove comments
                string line = lines[lineNumber - 1].Split('#')[0].Trim();

                // Ignore blank/comment only lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Split into fields
                string[] fields = line.Split(';');

                // Parse field 0 - code points
                int[] codePoints = fields[0].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToInt32(x, 16)).ToArray();

                // Parse field 1 - paragraph level
                sbyte paragraphLevel = sbyte.Parse(fields[1]);

                // Parse field 2 - resolved paragraph level
                sbyte resolvedParagraphLevel = sbyte.Parse(fields[2]);

                // Parse field 3 - resolved levels
                sbyte[] resolvedLevels = fields[3].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => x == "x" ? (sbyte)-1 : Convert.ToSByte(x)).ToArray();

                // Parse field 4 - resolved levels
                int[] resolvedOrder = fields[4].Split(' ').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).Select(x => Convert.ToInt32(x)).ToArray();

                var test = new Test(lineNumber, codePoints, paragraphLevel, resolvedParagraphLevel, resolvedLevels, resolvedOrder);
                tests.Add(test);
            }

            this.output.WriteLine($"Test data loaded: {tests.Count} test cases");

            var bidi = new BidiAlgorithm();
            var bidiData = new BidiData();

            // Run tests...
            for (int testNumber = 0; testNumber < tests.Count; testNumber++)
            {
                Test t = tests[testNumber];

                string text = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(t.CodePoints).ToArray());

                // Arrange
                bidiData.Init(text.AsSpan(), t.ParagraphLevel);

                // Act
                for (int i = 0; i < 10; i++)
                {
                    bidi.Process(bidiData);
                }

                ReadOnlyArraySlice<sbyte> resultLevels = bidi.ResolvedLevels;
                int resultParagraphLevel = bidi.ResolvedParagraphEmbeddingLevel;

                // Assert
                bool passed = true;
                if (t.ResolvedParagraphLevel != resultParagraphLevel)
                {
                    passed = false;
                }

                for (int i = 0; i < t.ResolvedLevels.Length; i++)
                {
                    if (t.ResolvedLevels[i] == -1)
                    {
                        continue;
                    }

                    if (t.ResolvedLevels[i] != resultLevels[i])
                    {
                        passed = false;
                        break;
                    }
                }

                if (!passed)
                {
                    this.output.WriteLine($"Failed line {t.LineNumber}");
                    this.output.WriteLine($"             Code Points: {string.Join(" ", t.CodePoints.Select(x => x.ToString("X4")))}");
                    this.output.WriteLine($"      Pair Bracket Types: {string.Join(" ", bidiData.PairedBracketTypes.Select(x => "   " + x.ToString()))}");
                    this.output.WriteLine($"     Pair Bracket Values: {string.Join(" ", bidiData.PairedBracketValues.Select(x => x.ToString("X4")))}");
                    this.output.WriteLine($"             Embed Level: {t.ParagraphLevel}");
                    this.output.WriteLine($"    Expected Embed Level: {t.ResolvedParagraphLevel}");
                    this.output.WriteLine($"      Actual Embed Level: {resultParagraphLevel}");
                    this.output.WriteLine($"          Directionality: {string.Join(" ", bidiData.Types)}");
                    this.output.WriteLine($"         Expected Levels: {string.Join(" ", t.ResolvedLevels)}");
                    this.output.WriteLine($"           Actual Levels: {string.Join(" ", resultLevels)}");
                    return false;
                }
            }

            return true;
        }

        private readonly struct Test
        {
            public Test(
                int lineNumber,
                int[] codePoints,
                sbyte paragraphLevel,
                sbyte resolvedParagraphLevel,
                sbyte[] resolvedLevels,
                int[] resolvedOrder)
            {
                this.LineNumber = lineNumber;
                this.CodePoints = codePoints;
                this.ParagraphLevel = paragraphLevel;
                this.ResolvedParagraphLevel = resolvedParagraphLevel;
                this.ResolvedLevels = resolvedLevels;
                this.ResolvedOrder = resolvedOrder;
            }

            public int LineNumber { get; }

            public int[] CodePoints { get; }

            public sbyte ParagraphLevel { get; }

            public sbyte ResolvedParagraphLevel { get; }

            public sbyte[] ResolvedLevels { get; }

            public int[] ResolvedOrder { get; }
        }
    }
}
