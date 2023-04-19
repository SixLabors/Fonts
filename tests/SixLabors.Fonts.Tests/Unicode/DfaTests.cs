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
        public void CanCompileSingleLiteral()
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
    }
}
