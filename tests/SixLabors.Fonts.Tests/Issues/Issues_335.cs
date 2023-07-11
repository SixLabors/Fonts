// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_335
    {
        [Theory]
        [InlineData(TextAlignment.Start)]
        [InlineData(TextAlignment.Center)]
        [InlineData(TextAlignment.End)]
        public void HorizontalAlignmentWorksForSingleLineText(TextAlignment alignment)
        {
            FontFamily family = new FontCollection().Add(TestFonts.OpenSansFile);
            family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

            TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
            {
                LineSpacing = 1.5F,
                WrappingLength = 10000,
                TextAlignment = alignment,
            };

            string text = "abc123";

            FontRectangle singleBounds = TextMeasurer.MeasureBounds(text, options);

            text = "abc123\nabc123";

            FontRectangle multipleBounds = TextMeasurer.MeasureBounds(text, options);

            Assert.Equal(multipleBounds.Location, singleBounds.Location);
        }

        [Theory]
        [InlineData(TextAlignment.Start)]
        [InlineData(TextAlignment.Center)]
        [InlineData(TextAlignment.End)]
        public void VerticalAlignmentWorksForSingleLineText(TextAlignment alignment)
        {
            FontFamily family = new FontCollection().Add(TestFonts.OpenSansFile);
            family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

            TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
            {
                LineSpacing = 1.5F,
                WrappingLength = 10000,
                TextAlignment = alignment,
                LayoutMode = LayoutMode.VerticalLeftRight
            };

            string text = "abc123";

            FontRectangle singleBounds = TextMeasurer.MeasureBounds(text, options);

            text = "abc123\nabc123";

            FontRectangle multipleBounds = TextMeasurer.MeasureBounds(text, options);

            Assert.Equal(multipleBounds.Location, singleBounds.Location);
        }
    }
}
