// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issue_298
    {
        [Fact]
        public void DoesNotThrowOutOfBounds()
        {
            const string content = "Please enter the text";

            FontCollection fontFamilies = new();

            FontFamily fontFamily = fontFamilies.Add(TestFonts.Issues.Issue298File);

            Font font = fontFamily.CreateFont(16, FontStyle.Regular);

            TextOptions renderOptions = new(font)
            {
                Dpi = 96,
                WrappingLength = 0f,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                KerningMode = KerningMode.Auto
            };

            FontRectangle bounds = TextMeasurer.Measure(content.AsSpan(), renderOptions);
            Assert.NotEqual(default, bounds);
        }
    }
}
