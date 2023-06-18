// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

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

            // TODO: We should probably drop the 32bit test runner.
#if NET472
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(32, size.Width, 1);
            }
            else
            {
                Assert.Equal(33, size.Width, 1);
            }
#else
            Assert.Equal(32, size.Width, 1);
#endif

            Assert.Equal(27, size.Height, 1);
        }
    }
}
