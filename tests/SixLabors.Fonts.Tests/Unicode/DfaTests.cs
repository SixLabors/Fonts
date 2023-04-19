// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using SixLabors.Fonts.Unicode.Dfa;
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
        public void CanCompileWithConcatination()
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
            StateMatch[] matches = stateMachine.Match(new[] { 1, 1, 1, 0, 0, 1, 0, 1, 1, 1, 0 }).ToArray();

            var expected = new StateMatch[]
            {
                new StateMatch() { StartIndex = 2, EndIndex = 3, Tags = new string[] { "x" } },
                new StateMatch() { StartIndex = 4, EndIndex = 5, Tags = new string[] { "y" } },
                new StateMatch() { StartIndex = 6, EndIndex = 7, Tags = new string[] { "y" } },
                new StateMatch() { StartIndex = 9, EndIndex = 10, Tags = new string[] { "x" } },
            };

            Assert.True(expected.SequenceEqual(matches));

            // TODO: Test Apply.
        }
    }
}
