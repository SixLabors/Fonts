// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_35
    {
        [Fact]
        public void RenderingTabAtStartOrLineTooShort()
        {
            Font font = CreateFont("\t x");
            FontRectangle xWidth = TextMeasurer.MeasureBounds("x", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle tabWidth = TextMeasurer.MeasureBounds("\t", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle doublTabWidth = TextMeasurer.MeasureBounds("\t\t", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle tabWithXWidth = TextMeasurer.MeasureBounds("\tx", new RendererOptions(font, font.FontMetrics.ScaleFactor));

            Assert.Equal(tabWidth.Width + xWidth.Width, tabWithXWidth.Width, 2);
        }

        [Fact]
        public void Rendering2TabsAtStartOfLineTooShort()
        {
            Font font = CreateFont("\t x");
            FontRectangle xWidth = TextMeasurer.MeasureBounds("x", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle tabWidth = TextMeasurer.MeasureBounds("\t\t", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle tabWithXWidth = TextMeasurer.MeasureBounds("\t\tx", new RendererOptions(font, font.FontMetrics.ScaleFactor));

            Assert.Equal(tabWidth.Width + xWidth.Width, tabWithXWidth.Width, 2);
        }

        [Fact]
        public void TwoTabsAreDoubleWidthOfOneTab()
        {
            Font font = CreateFont("\t x");
            FontRectangle xWidth = TextMeasurer.MeasureBounds("x", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle tabWidth = TextMeasurer.MeasureBounds("\t", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle twoTabWidth = TextMeasurer.MeasureBounds("\t\t", new RendererOptions(font, font.FontMetrics.ScaleFactor));

            Assert.Equal(twoTabWidth.Width, tabWidth.Width * 2, 2);
        }

        [Fact]
        public void TwoTabsAreDoubleWidthOfOneTabMinusXWidth()
        {
            Font font = CreateFont("\t x");
            FontRectangle xWidth = TextMeasurer.MeasureBounds("x", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle tabWidth = TextMeasurer.MeasureBounds("\tx", new RendererOptions(font, font.FontMetrics.ScaleFactor));
            FontRectangle twoTabWidth = TextMeasurer.MeasureBounds("\t\tx", new RendererOptions(font, font.FontMetrics.ScaleFactor));

            Assert.Equal(twoTabWidth.Width - xWidth.Width, (tabWidth.Width - xWidth.Width) * 2, 2);
        }

        public static Font CreateFont(string text)
        {
            var fc = new FontCollection();
            Font d = fc.Add(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
