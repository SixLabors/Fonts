using System;
using System.Collections.Generic;
using System.Text;
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
            FontFamily arial = FontCollection.SystemFonts.Find("Arial");
            Font font = new Font(arial, 1f, FontStyle.Regular);

            Size size = new TextMeasurer().MeasureText(c.ToString(), font, 72);
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
            FontFamily arial = FontCollection.SystemFonts.Find("Arial");
            Font font = new Font(arial, 1f, FontStyle.Regular);

            Size size = new TextMeasurer().MeasureText($"abc{c}def", font, 72);
        }
    }
}
