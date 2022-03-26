// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TextRunTests
    {
        private const string Text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";

        [Theory]
        [InlineData(-1, -1)]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(1, 1)]
        [InlineData(3, 2)]
        public void ThrowsZeroValue(int start, int end)
        {
            Exception ex = Record.Exception(() => new TextRun() { Start = start, End = end }.Slice(Text.AsSpan()));
            Assert.NotNull(ex);
        }

        [Fact]
        public void SlicesTextCorrectly()
        {
            ReadOnlySpan<char> slice = new TextRun() { Start = 19, End = 46 }.Slice(Text.AsSpan());
            Assert.True("Honorificabilitudinitatibus".AsSpan().SequenceEqual(slice));
        }
    }
}
