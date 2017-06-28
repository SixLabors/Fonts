using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_35
    {
        [Fact]
        public void RenderingTabAtStartOrLineTooShort()
        {
            var font = CreateFont("\t x");
            SizeF xWidth = TextMeasurer.Measure("x", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF tabWidth = TextMeasurer.Measure("\t", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF doublTabWidth = TextMeasurer.Measure("\t\t", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF tabWithXWidth = TextMeasurer.Measure("\tx", new RendererOptions(font, (72 * font.EmSize))).Size;

            Assert.Equal(tabWidth.Width + xWidth.Width, tabWithXWidth.Width, 2);
        }


        [Fact]
        public void Rendering2TabsAtStartOfLineTooShort()
        {
            var font = CreateFont("\t x");
            SizeF xWidth = TextMeasurer.Measure("x", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF tabWidth = TextMeasurer.Measure("\t\t", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF tabWithXWidth = TextMeasurer.Measure("\t\tx", new RendererOptions(font, (72 * font.EmSize))).Size;

            Assert.Equal(tabWidth.Width + xWidth.Width, tabWithXWidth.Width, 2);
        }

        [Fact]
        public void TwoTabsAreDoubleWidthOfOneTab()
        {
            var font = CreateFont("\t x");
            SizeF xWidth = TextMeasurer.Measure("x", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF tabWidth = TextMeasurer.Measure("\t", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF twoTabWidth = TextMeasurer.Measure("\t\t", new RendererOptions(font, (72 * font.EmSize))).Size;

            Assert.Equal(twoTabWidth.Width, tabWidth.Width * 2, 2);
        }


        [Fact]
        public void TwoTabsAreDoubleWidthOfOneTabMinusXWidth()
        {
            var font = CreateFont("\t x");
            SizeF xWidth = TextMeasurer.Measure("x", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF tabWidth = TextMeasurer.Measure("\tx", new RendererOptions(font, (72 * font.EmSize))).Size;
            SizeF twoTabWidth = TextMeasurer.Measure("\t\tx", new RendererOptions(font, (72 * font.EmSize))).Size;

            Assert.Equal(twoTabWidth.Width - xWidth.Width, (tabWidth.Width - xWidth.Width) * 2, 2);
        }

        public static Font CreateFont(string text)
        {
            FontCollection fc = new FontCollection();
            Font d = fc.Install(new FakeFontInstance(text)).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
