// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class ConstructorGuards
    {
        [Fact]
        public void FontClass_NullFontFamilyThrowsException()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException
                >(
                () => new Font((FontFamily)null, 0f, FontStyle.Regular));
            Assert.Equal("family", ex.ParamName);
        }

        [Fact]
        public void FontClass_NullFontThrowsException()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
                () => new Font((Font)null, 0f, FontStyle.Regular));

            Assert.Equal("prototype", ex.ParamName);
        }
    }
}
