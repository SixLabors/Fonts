// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FakeFont
    {
        [Fact]
        public void TestFontMetricProperties()
        {
            Font fakeFont = CreateFont("A");
            FontMetrics metrics = fakeFont.FontMetrics;
            Assert.Equal(30, metrics.UnitsPerEm);
            Assert.Equal(35, metrics.HorizontalMetrics.Ascender);
            Assert.Equal(8, metrics.HorizontalMetrics.Descender);
            Assert.Equal(12, metrics.HorizontalMetrics.LineGap);
            Assert.Equal(35 - 8 + 12, metrics.HorizontalMetrics.LineHeight);

            // Vertical metrics are all ones. Descender is always negative due to the grid orientation.
            Assert.Equal(1, metrics.VerticalMetrics.Ascender);
            Assert.Equal(-1, metrics.VerticalMetrics.Descender);
            Assert.Equal(1, metrics.VerticalMetrics.LineGap);
            Assert.Equal(1 - (-1) + 1, metrics.VerticalMetrics.LineHeight);
        }

        public static Font CreateFont(string text, string name = "name")
            => CreateFontWithInstance(text, name, out _);

        internal static Font CreateFontWithInstance(string text, string name, out FakeFontInstance instance)
        {
            var fc = (IFontMetricsCollection)new FontCollection();
            instance = FakeFontInstance.CreateFontWithVaryingVerticalFontMetrics(text, name);
            Font d = fc.AddMetrics(instance, CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
