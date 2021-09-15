// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.Fonts.Unicode;
using Xunit;
using Xunit.Abstractions;

namespace SixLabors.Fonts.Tests.Unicode
{
    public class BidiAlgorithmTests
    {
        private readonly ITestOutputHelper output;

        public BidiAlgorithmTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void RendersArabicNumbersFromLeftToRight()
        {
            // arrange
            Font arabicFont = new FontCollection().Add(TestFonts.ArabicFontFile).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "٠١٢٣٤٥٦٧٨٩";
            int[] expectedGlyphIndices = { 403, 405, 407, 409, 411, 413, 415, 417, 419, 421 };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(arabicFont) { ApplyKerning = true });

            // assert
            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphIndex);
            }
        }

        [Fact]
        public void ICUTests() => Assert.True(this.ICUTestsImpl());

        private bool ICUTestsImpl()
        {
            this.output.WriteLine("Bidi Class Tests");
            this.output.WriteLine("----------------");

            // Read the test file
            string[] lines = File.ReadAllLines(Path.Combine(TestEnvironment.UnicodeTestDataFullPath, "BidiTest.txt"));

            var bidi = new BidiAlgorithm();

            var tests = new List<Test>();

            // Process each line
            int[] levels = null;
            for (int lineNumber = 1; lineNumber < lines.Length + 1; lineNumber++)
            {
                // Get the line, remove comments
                string line = lines[lineNumber - 1].Split('#')[0].Trim();

                // Ignore blank/comment only lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Directive?
                if (line.StartsWith("@"))
                {
                    if (line.StartsWith("@Levels:"))
                    {
                        levels = line.Substring(8).Trim().Split(' ').Where(x => x.Length > 0).Select(x =>
                        {
                            if (x == "x")
                            {
                                return -1;
                            }
                            else
                            {
                                return int.Parse(x);
                            }
                        }).ToArray();
                    }

                    continue;
                }

                // Split data line
                string[] parts = line.Split(';');

                // Get the directions
                BidiCharacterType[] directions = parts[0].Split(' ').Select(x =>
                {
                    UnicodeTypeMaps.BidiCharacterTypeMap.TryGetValue(x, out BidiCharacterType cls);
                    return cls;
                }).ToArray();

                // Get the bit set
                int bitset = Convert.ToInt32(parts[1].Trim(), 16);

                BidiPairedBracketType[] pairTypes = Enumerable.Repeat(BidiPairedBracketType.None, directions.Length).ToArray();
                int[] pairValues = Enumerable.Repeat(0, directions.Length).ToArray();

                for (int bit = 1; bit < 8; bit <<= 1)
                {
                    if ((bitset & bit) == 0)
                    {
                        continue;
                    }

                    sbyte paragraphEmbeddingLevel;
                    switch (bit)
                    {
                        case 1:
                            paragraphEmbeddingLevel = 2;        // Auto
                            break;

                        case 2:
                            paragraphEmbeddingLevel = 0;        // LTR
                            break;

                        case 4:
                            paragraphEmbeddingLevel = 1;        // RTL
                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    tests.Add(new Test(directions, paragraphEmbeddingLevel, levels, lineNumber));
                }
            }

            this.output.WriteLine($"Test data loaded: {tests.Count} test cases");

            for (int testNumber = 0; testNumber < tests.Count; testNumber++)
            {
                Test t = tests[testNumber];

                // Run the algorithm...
                ReadOnlyArraySlice<sbyte> resultLevels;

                bidi.Process(
                    t.Types,
                    ArraySlice<BidiPairedBracketType>.Empty,
                    ArraySlice<int>.Empty,
                    t.ParagraphEmbeddingLevel,
                    false,
                    null,
                    null,
                    null);

                resultLevels = bidi.ResolvedLevels;

                // Check the results match
                bool pass = true;
                if (resultLevels.Length == t.ExpectedLevels.Length)
                {
                    for (int i = 0; i < t.ExpectedLevels.Length; i++)
                    {
                        if (t.ExpectedLevels[i] == -1)
                        {
                            continue;
                        }

                        if (resultLevels[i] != t.ExpectedLevels[i])
                        {
                            pass = false;
                            break;
                        }
                    }
                }
                else
                {
                    pass = false;
                }

                if (!pass)
                {
                    this.output.WriteLine($"Failed line {t.LineNumber}");
                    this.output.WriteLine($"        Data: {string.Join(" ", t.Types)}");
                    this.output.WriteLine($" Embed Level: {t.ParagraphEmbeddingLevel}");
                    this.output.WriteLine($"    Expected: {string.Join(" ", t.ExpectedLevels)}");
                    this.output.WriteLine($"      Actual: {string.Join(" ", resultLevels)}");
                    return false;
                }
            }

            return true;
        }

        private readonly struct Test
        {
            public Test(
                BidiCharacterType[] types,
                sbyte paragraphEmbeddingLevel,
                int[] expectedLevels,
                int lineNumber)
            {
                this.Types = types;
                this.ParagraphEmbeddingLevel = paragraphEmbeddingLevel;
                this.ExpectedLevels = expectedLevels;
                this.LineNumber = lineNumber;
            }

            public BidiCharacterType[] Types { get; }

            public sbyte ParagraphEmbeddingLevel { get; }

            public int[] ExpectedLevels { get; }

            public int LineNumber { get; }
        }
    }
}
