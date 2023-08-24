// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

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
            FontFamily sans = new FontCollection().Add(TestFonts.OpenSansFile);
            var font = new Font(sans, 1f, FontStyle.Regular);

            FontRectangle size = TextMeasurer.MeasureSize(c.ToString(), new TextOptions(font));
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
            FontFamily sans = new FontCollection().Add(TestFonts.OpenSansFile);
            var font = new Font(sans, 1f, FontStyle.Regular);

            FontRectangle size = TextMeasurer.MeasureSize($"abc{c}def", new TextOptions(font));
        }
    }
}
