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
            SizeF size = TextMeasurer.Measure(text, new RendererOptions(font, (72 * font.EmSize))
            {
                TabWidth = 0
            });

            // tab width of 0 should make tabs not render at all
            Assert.Equal(30, size.Height, 4);
            Assert.Equal(300, size.Width, 4);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
