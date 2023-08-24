// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;
using System.Linq;
using UnicodeTrieGenerator.StateAutomation;
using Xunit;

namespace SixLabors.Fonts.Tests.Unicode
{
    public class DfaTests
    {
        [Fact]
        public void CanCompileWithSingleLiteral()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 0 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 0, EndIndex = 0 },
                new StateMatch() { StartIndex = 1, EndIndex = 1 },
                new StateMatch() { StartIndex = 3, EndIndex = 3 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithConcatenation()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a b;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 1, 0, 1, 0 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 1, EndIndex = 2 },
                new StateMatch() { StartIndex = 4, EndIndex = 5 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithAlternation()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = (a b) | (b a);");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 1, 0, 1, 0 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 1, EndIndex = 2 },
                new StateMatch() { StartIndex = 3, EndIndex = 4 },
                new StateMatch() { StartIndex = 5, EndIndex = 6 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithRepeat()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = (a b)+;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 0, 1, 1, 0, 1 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 1, EndIndex = 4 },
                new StateMatch() { StartIndex = 6, EndIndex = 7 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithOptionalRepeat()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = b a (a b)*;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 0, 0, 1, 0, 1, 1, 1, 0 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 2, EndIndex = 7 },
                new StateMatch() { StartIndex = 9, EndIndex = 10 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithExactRepetition()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{3} b;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 3, EndIndex = 6 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithMinimumRepetition()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{3,} b;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 3, EndIndex = 6 },
                new StateMatch() { StartIndex = 7, EndIndex = 11 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithMaximumRepetition()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{,3} b;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 0, EndIndex = 2 },
                new StateMatch() { StartIndex = 3, EndIndex = 6 },
                new StateMatch() { StartIndex = 10, EndIndex = 11 },
                new StateMatch() { StartIndex = 12, EndIndex = 12 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithMinimumAndMaximumRepetition()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = a{3,5} b;");
            StateMatch[] matches = stateMachine.Match(new[] { 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 3, EndIndex = 6 },
                new StateMatch() { StartIndex = 7, EndIndex = 11 }
            };

            Assert.True(expected.SequenceEqual(matches));
        }

        [Fact]
        public void CanCompileWithTags()
        {
            StateMachine stateMachine = Compile.Build("a = 0; b = 1; Main = x:(b a) | y:(a b);");

            int[] input = { 1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0 };
            StateMatch[] matches = stateMachine.Match(new[] { 1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 2, EndIndex = 3, Tags = new string[] { "x" } },
                new StateMatch() { StartIndex = 4, EndIndex = 5, Tags = new string[] { "y" } },
                new StateMatch() { StartIndex = 6, EndIndex = 7, Tags = new string[] { "y" } },
                new StateMatch() { StartIndex = 9, EndIndex = 10, Tags = new string[] { "x" } },
            };

            Assert.True(expected.SequenceEqual(matches));

            List<(string Tag, int Start, int End, ArraySlice<int> Slice)> applied = new();
            Dictionary<string, Action<int, int, ArraySlice<int>>> actions = new()
            {
                { "x", (start, end, slice) => applied.Add(("x", start, end, slice)) },
                { "y", (start, end, slice) => applied.Add(("y", start, end, slice)) }
            };

            stateMachine.Apply(input, actions);

            Assert.True(applied.Count == 4);

            List<(string Tag, int Start, int End, ArraySlice<int> Slice)> expectedApply = new()
            {
                ("x", 2, 3, new int[] { 1, 0 }),
                ("y", 4, 5, new int[] { 0, 1 }),
                ("y", 6, 7, new int[] { 0, 1 }),
                ("x", 9, 10, new int[] { 1, 0 }),
            };

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
            var externalSymbols = new Dictionary<string, int>() { { "a", 0 }, { "b", 1 } };
            StateMachine stateMachine = Compile.Build("Main = a b;", externalSymbols);
            int[] input = { 0, 0, 1, 1, 0, 1, 0 };
            StateMatch[] matches = stateMachine.Match(input).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 1, EndIndex = 2 },
                new StateMatch() { StartIndex = 4, EndIndex = 5 },
            };

            Assert.True(expected.SequenceEqual(matches));
        }
    }
}
