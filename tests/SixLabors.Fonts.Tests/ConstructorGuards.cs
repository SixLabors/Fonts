using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class ConstructorGuards
    {
        [Fact]
        public void FontClass_NullFontFamilyThrowsException()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new Font((FontFamily)null, 0f, FontVariant.Regular));
            Assert.Equal("family", ex.ParamName);
        }

        [Fact]
        public void FontClass_NullFontThrowsException()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new Font((Font)null, 0f, FontVariant.Regular));
            Assert.Equal("prototype", ex.ParamName);
        }
    }
}
