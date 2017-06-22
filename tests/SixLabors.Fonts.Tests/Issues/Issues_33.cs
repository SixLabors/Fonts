using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_33
    {
        [Theory]
        [InlineData("\naaaabbbbccccddddeeee\n\t\t\t3 tabs\n\t\t\t\t\t5 tabs", 780, 120)]
        [InlineData("\n\tHelloworld", 420, 60)]
        [InlineData("\tHelloworld", 420, 30)]
        [InlineData("  Helloworld", 360, 30)]
        [InlineData("Hell owor ld\t", 480, 30)]
        [InlineData("Helloworld  ", 360, 30)]
        public void WhiteSpaceAtStartOfLineNotMeasured(string text, float width, float height )
        {
            var font  = CreateFont(text);
            SizeF size = TextMeasurer.Measure(text, new RendererOptions(font, (72 * font.EmSize))
            {
            });

            Assert.Equal(height, size.Height, 2);
            Assert.Equal(width, size.Width, 2);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
