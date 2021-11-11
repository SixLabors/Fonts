// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_27
    {
        [Fact]
        public void ThrowsMeasuringWhitespace()
        {
            // wendy one returns wrong points for 'o'
            Font font = new FontCollection().Add(TestFonts.WendyOneFile).CreateFont(12);
            FontRectangle size = TextMeasurer.MeasureBounds("          ", new TextOptions(new Font(font, 30), 72));

            Assert.Equal(60, size.Width, 1);
            Assert.Equal(31.6, size.Height, 1);
        }
    }
}
