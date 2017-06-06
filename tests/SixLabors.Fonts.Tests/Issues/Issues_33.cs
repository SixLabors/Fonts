using SixLabors.Fonts.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_33
    {
        [Theory]
        [InlineData("\tHelloworld", 390, 20)]
        [InlineData("  Helloworld", 360, 20)]
        [InlineData("Hell owor ld\t", 450, 20)]
        [InlineData("Helloworld  ", 360, 20)]
        public void WhiteSpaceAtStartOfLineNotMeasured(string text, float width, float height )
        {
            var font  = CreateFont(text);
            Size size = TextMeasurer.Measure(text, new FontSpan(font, (72 * font.EmSize))
            {
            });

            Assert.Equal(height, size.Height, 2);
            Assert.Equal(width, size.Width, 2);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text));
            return new Font(d, 1);
        }
    }
}
