// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class ConstructorGuards
    {
        [Fact]
        public void FontClass_DefaultFontFamilyThrowsException()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => new Font(default(FontFamily), 0F));

            Assert.Equal("family", ex.ParamName);
        }

        [Fact]
        public void FontClass_DefaultFontFamilyWithSizeThrowsException()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => new Font(default(FontFamily), 0F, FontStyle.Regular));

            Assert.Equal("family", ex.ParamName);
        }

        [Fact]
        public void FontClass_NullFontThrowsException()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
                () => new Font(null, FontStyle.Regular));

            Assert.Equal("prototype", ex.ParamName);
        }

        [Fact]
        public void FontClass_NullWithSizeFontThrowsException()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
                () => new Font(null, 0f, FontStyle.Regular));

            Assert.Equal("prototype", ex.ParamName);
        }
    }
}
