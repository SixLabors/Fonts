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
            Assert.Equal(35, metrics.Ascender);
            Assert.Equal(8, metrics.Descender);
            Assert.Equal(12, metrics.LineGap);
            Assert.Equal(35 - 8 + 12, metrics.LineHeight);
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
