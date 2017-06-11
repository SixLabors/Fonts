using SixLabors.Primitives;
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
            FontFamily arial = SystemFonts.Find("Arial");
            Font font = new Font(arial, 1f, FontStyle.Regular);

            SizeF size = TextMeasurer.Measure(c.ToString(), font, 72);
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
            FontFamily arial = SystemFonts.Find("Arial");
            Font font = new Font(arial, 1f, FontStyle.Regular);

            SizeF size = TextMeasurer.Measure($"abc{c}def", font, 72);
        }
    }
}
