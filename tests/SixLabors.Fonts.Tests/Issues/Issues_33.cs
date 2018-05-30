using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Primitives;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_33
    {
        [Theory]
        [InlineData("\naaaabbbbccccddddeeee\n\t\t\t3 tabs\n\t\t\t\t\t5 tabs", 760, 70)] // newlines arn't directly measured but it is used for offseting
        [InlineData("\n\tHelloworld", 400, 10)]
        [InlineData("\tHelloworld", 400, 10)]
        [InlineData("  Helloworld", 340, 10)]
        [InlineData("Hell owor ld\t", 480, 10)]
        [InlineData("Helloworld  ", 360, 10)]
        public void WhiteSpaceAtStartOfLineNotMeasured(string text, float width, float height )
        {
            Font font  = CreateFont(text);
            SizeF size = TextMeasurer.MeasureBounds(text, new RendererOptions(font, (72 * font.EmSize))
            {
            }).Size;

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
