// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#if NET472
using System;
#endif
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_269
    {
        [Fact]
        public void CorrectlySetsMetricsForFontsNotAdheringToSpec()
        {
            // AliceFrancesHMK has invalid subtables.
            Font font = new FontCollection().Add(TestFonts.AliceFrancesHMKRegularFile).CreateFont(25);

            FontRectangle size = TextMeasurer.MeasureSize("H", new TextOptions(font));
            Assert.Equal(32, size.Width, 1);
            Assert.Equal(27, size.Height, 1);
        }
    }
}
