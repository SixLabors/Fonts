using Xunit;

using SixLabors.Fonts.Tests.Fakes;
using System.Globalization;

namespace SixLabors.Fonts.Tests
{
    public class FakeFont
    {
        [Fact]
        public void TestFontMetricProperties()
        {
            Font fakeFont = CreateFont("A");
            Assert.Equal(30, fakeFont.EmSize);
            Assert.Equal(35, fakeFont.Ascender);
            Assert.Equal(8, fakeFont.Descender);
            Assert.Equal(12, fakeFont.LineGap);
            Assert.Equal(35 - 8 + 12, fakeFont.LineHeight);
        }

        public static Font CreateFont(string text)
        {
            return CreateFontWithInstance(text, out _);
        }

        internal static Font CreateFontWithInstance(string text, out FakeFontInstance instance)
        {
            var fc = new FontCollection();
            instance = FakeFontInstance.CreateFontWithVaryingVerticalFontMetrics(text);
            Font d = fc.Install(instance, CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
