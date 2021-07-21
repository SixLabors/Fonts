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
            Assert.Equal(30, fakeFont.UnitsPerEm);
            Assert.Equal(35, fakeFont.Ascender);
            Assert.Equal(8, fakeFont.Descender);
            Assert.Equal(12, fakeFont.LineGap);
            Assert.Equal(35 - 8 + 12, fakeFont.LineHeight);
        }

        public static Font CreateFont(string text)
            => CreateFontWithInstance(text, out _);

        internal static Font CreateFontWithInstance(string text, out FakeFontInstance instance)
        {
            var fc = new FontCollection();
            instance = FakeFontInstance.CreateFontWithVaryingVerticalFontMetrics(text);
            Font d = fc.Install(instance, CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
