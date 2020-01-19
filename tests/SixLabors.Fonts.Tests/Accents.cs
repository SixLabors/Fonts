using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class Accents
    {
        [Theory]
        [InlineData('á')]
        [InlineData('é')]
        [InlineData('í')]
        [InlineData('ó')]
        [InlineData('ú')]
        [InlineData('ç')]
        [InlineData('ã')]
        [InlineData('õ')]
        public void MeasuringAccentedCharacterDoesNotThrow(char c)
        {
            FontFamily arial = new FontCollection().Install(TestFonts.OpenSansFile);
            var font = new Font(arial, 1f, FontStyle.Regular);

            FontRectangle size = TextMeasurer.Measure(c.ToString(), new RendererOptions(font, 72));
        }

        [Theory]
        [InlineData('á')]
        [InlineData('é')]
        [InlineData('í')]
        [InlineData('ó')]
        [InlineData('ú')]
        [InlineData('ç')]
        [InlineData('ã')]
        [InlineData('õ')]
        public void MeasuringWordWithAccentedCharacterDoesNotThrow(char c)
        {
            FontFamily arial = new FontCollection().Install(TestFonts.OpenSansFile);
            var font = new Font(arial, 1f, FontStyle.Regular);

            FontRectangle size = TextMeasurer.Measure($"abc{c}def", new RendererOptions(font, 72));
        }
    }
}
