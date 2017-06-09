using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_32
    {
        [Fact]
        public void TabWidth0CausesBadTabRendering()
        {
            var text = "Hello\tworld";
            var font  = CreateFont(text);
            SizeF size = TextMeasurer.Measure(text, new FontSpan(font, (72 * font.EmSize))
            {
                TabWidth = 0
            });

            // tab width of 0 should make tabs not render at all
            Assert.Equal(20, size.Height, 4);
            Assert.Equal(300, size.Width, 4);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text));
            return new Font(d, 1);
        }
    }
}
