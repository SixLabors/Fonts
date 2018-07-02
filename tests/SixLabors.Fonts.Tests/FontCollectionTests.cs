using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontCollectionTests
    {
        [Fact]
        public void InstallViaPathReturnsDecription()
        {
            FontCollection sut = new FontCollection();

            FontFamily family = sut.Install(TestFonts.CarterOneFile, out FontDescription description);
            Assert.NotNull(description);
            Assert.Equal("Carter One", description.FontFamily);
            Assert.Equal("Regular", description.FontSubFamilyName);
            Assert.Equal(FontStyle.Regular, description.Style);
        }

        [Fact]
        public void InstallViaStreamReturnsDecription()
        {
            FontCollection sut = new FontCollection();
            using (System.IO.Stream s = TestFonts.CarterOneFileData())
            {
                FontFamily family = sut.Install(s, out FontDescription description);
                Assert.NotNull(description);
                Assert.Equal("Carter One", description.FontFamily);
                Assert.Equal("Regular", description.FontSubFamilyName);
                Assert.Equal(FontStyle.Regular, description.Style);
            }
        }
    }
}
