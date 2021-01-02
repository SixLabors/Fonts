// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontFamilyTests
    {
        private readonly FontFamily[] families = new SystemFontCollection().Families.ToArray();

        [Fact]
        public void EqualNullTests()
        {
            FontFamily fontFamily = null;
            Assert.True(fontFamily == null);
            Assert.False(fontFamily != null);

            fontFamily = this.families[0];
            Assert.True(fontFamily != null);
            Assert.False(fontFamily == null);
            Assert.False(fontFamily.Equals(null));
        }

        [Fact]
        public void EqualTests()
        {
            FontFamily fontFamily = this.families[0];
            FontFamily fontFamily2 = this.families[0];

            Assert.True(fontFamily == fontFamily2);
            Assert.False(fontFamily != fontFamily2);
            Assert.True(fontFamily.Equals(fontFamily2));
        }

        [Fact]
        public void NotEqualTests()
        {
            FontFamily fontFamily = this.families[0];
            FontFamily fontFamily2 = this.families[1];

            Assert.False(fontFamily == fontFamily2);
            Assert.True(fontFamily != fontFamily2);
            Assert.False(fontFamily.Equals(fontFamily2));
        }
    }
}
