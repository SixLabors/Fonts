// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;

namespace SixLabors.Fonts.Tests.Unicode;

public class DfaTests
{
    [Fact]
    public void UniversalMachineKeepsSinhalaConjunctTogether()
    {
        string[] categories = UniversalShapingData.Categories;
        int[] input =
        [
            Array.IndexOf(categories, "B"),
            Array.IndexOf(categories, "HVM"),
            Array.IndexOf(categories, "B")
        ];

        StateMachine stateMachine = new(UniversalShapingData.StateTable, UniversalShapingData.AcceptingStates, UniversalShapingData.Tags);
        StateMatch match = Assert.Single(stateMachine.Match(input));

        Assert.Equal(0, match.StartIndex);
        Assert.Equal(2, match.EndIndex);
        Assert.Contains("standard_cluster", match.Tags);
    }

    [Fact]
    public void UniversalMachineSeparatesWordJoinerFromBrokenCluster()
    {
        int[] input =
        [
            UnicodeData.GetUniversalShapingSymbolCount(0x2060),
            UnicodeData.GetUniversalShapingSymbolCount(0x11127)
        ];

        StateMachine stateMachine = new(UniversalShapingData.StateTable, UniversalShapingData.AcceptingStates, UniversalShapingData.Tags);
        StateMatch[] matches = [.. stateMachine.Match(input)];

        Assert.Equal(2, matches.Length);
        Assert.Contains("non_cluster", matches[0].Tags);
        Assert.Contains("broken_cluster", matches[1].Tags);
    }

    /// <summary>
    /// Verifies representative source-only rules in the Universal Shaping Engine category table.
    /// </summary>
    /// <param name="codePoint">The character to classify.</param>
    /// <param name="expected">The expected category name.</param>
    [Theory]
    [InlineData(0x0627, "O")]
    [InlineData(0x0DCA, "HVM")]
    [InlineData(0x200D, "CGJ")]
    [InlineData(0x2015, "O")]
    [InlineData(0xE0000, "WJ")]
    public void UniversalCategoriesMatchReferenceTable(int codePoint, string expected)
    {
        string[] categories = UniversalShapingData.Categories;
        int category = UnicodeData.GetUniversalShapingSymbolCount((uint)codePoint);

        Assert.Equal(expected, categories[category]);
    }

    [Fact]
    public void CanCompileWithSingleLiteral()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 0])];

        StateMatch[] expected =
        [
            new() { StartIndex = 0, EndIndex = 0 },
            new() { StartIndex = 1, EndIndex = 1 },
            new() { StartIndex = 3, EndIndex = 3 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithConcatenation()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a b;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 1, 0, 1, 0])];

        StateMatch[] expected =
        [
            new() { StartIndex = 1, EndIndex = 2 },
            new() { StartIndex = 4, EndIndex = 5 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithAlternation()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = (a b) | (b a);");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 1, 0, 1, 0])];

        StateMatch[] expected =
        [
            new() { StartIndex = 1, EndIndex = 2 },
            new() { StartIndex = 3, EndIndex = 4 },
            new() { StartIndex = 5, EndIndex = 6 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithRepeat()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = (a b)+;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 0, 1, 1, 0, 1])];

        StateMatch[] expected =
        [
            new() { StartIndex = 1, EndIndex = 4 },
            new() { StartIndex = 6, EndIndex = 7 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithOptionalRepeat()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = b a (a b)*;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 0, 0, 1, 0, 1, 1, 1, 0])];

        StateMatch[] expected =
        [
            new() { StartIndex = 2, EndIndex = 7 },
            new() { StartIndex = 9, EndIndex = 10 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithExactRepetition()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{3} b;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1])];

        StateMatch[] expected =
        [
            new() { StartIndex = 3, EndIndex = 6 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithMinimumRepetition()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{3,} b;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1])];

        StateMatch[] expected =
        [
            new() { StartIndex = 3, EndIndex = 6 },
            new() { StartIndex = 7, EndIndex = 11 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithMaximumRepetition()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{,3} b;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1])];

        StateMatch[] expected =
        [
            new() { StartIndex = 0, EndIndex = 2 },
            new() { StartIndex = 3, EndIndex = 6 },
            new() { StartIndex = 10, EndIndex = 11 },
            new() { StartIndex = 12, EndIndex = 12 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithMinimumAndMaximumRepetition()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{3,5} b;");
        StateMatch[] matches = [.. stateMachine.Match([0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1])];

        StateMatch[] expected =
        [
            new() { StartIndex = 3, EndIndex = 6 },
            new() { StartIndex = 7, EndIndex = 11 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }

    [Fact]
    public void CanCompileWithTags()
    {
        StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = x:(b a) | y:(a b);");

        int[] input = [1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0];
        StateMatch[] matches = [.. stateMachine.Match([1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0])];

        StateMatch[] expected =
        [
            new() { StartIndex = 2, EndIndex = 3, Tags = new string[] { "x" } },
            new() { StartIndex = 4, EndIndex = 5, Tags = new string[] { "y" } },
            new() { StartIndex = 6, EndIndex = 7, Tags = new string[] { "y" } },
            new() { StartIndex = 9, EndIndex = 10, Tags = new string[] { "x" } }
        ];

        Assert.True(expected.SequenceEqual(matches));

        List<(string Tag, int Start, int End, ArraySlice<int> Slice)> applied = [];
        Dictionary<string, Action<int, int, ArraySlice<int>>> actions = new()
        {
            { "x", (start, end, slice) => applied.Add(("x", start, end, slice)) },
            { "y", (start, end, slice) => applied.Add(("y", start, end, slice)) }
        };

        stateMachine.Apply(input, actions);

        Assert.True(applied.Count == 4);

        List<(string Tag, int Start, int End, ArraySlice<int> Slice)> expectedApply =
        [
            ("x", 2, 3, new int[] { 1, 0 }),
            ("y", 4, 5, new int[] { 0, 1 }),
            ("y", 6, 7, new int[] { 0, 1 }),
            ("x", 9, 10, new int[] { 1, 0 }),
        ];

        for (int i = 0; i < expectedApply.Count; i++)
        {
            (string Tag, int Start, int End, ArraySlice<int> Slice) e = expectedApply[i];
            (string Tag, int Start, int End, ArraySlice<int> Slice) a = applied[i];

            Assert.Equal(e.Tag, a.Tag);
            Assert.Equal(e.Start, a.Start);
            Assert.Equal(e.End, a.End);
            Assert.True(e.Slice.SequenceEqual(a.Slice));
        }
    }

    [Fact]
    public void CanCompileWithExternalSymbols()
    {
        Dictionary<string, int> externalSymbols = new() { { "a", 0 }, { "b", 1 } };
        StateMachine stateMachine = Compile.Build("Main = a b;", externalSymbols);
        int[] input = [0, 0, 1, 1, 0, 1, 0];
        StateMatch[] matches = [.. stateMachine.Match(input)];

        StateMatch[] expected =
        [
            new() { StartIndex = 1, EndIndex = 2 },
            new() { StartIndex = 4, EndIndex = 5 }
        ];

        Assert.True(expected.SequenceEqual(matches));
    }
}
