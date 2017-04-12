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
            var arial = FontCollection.SystemFonts.Find("Arial");
            var font = new Font(arial, 1f, FontStyle.Regular);

            var size = new TextMeasurer().MeasureText(c.ToString(), font, 72);
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
            var arial = FontCollection.SystemFonts.Find("Arial");
            var font = new Font(arial, 1f, FontStyle.Regular);

            var size = new TextMeasurer().MeasureText($"abc{c}def", font, 72);
        }
    }
}
