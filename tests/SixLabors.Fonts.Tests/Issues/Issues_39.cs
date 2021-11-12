// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_39
    {
        [Fact]
        public void RenderingEmptyString_DoesNotThrow()
        {
            Font font = CreateFont("\t x");

            var r = new GlyphRenderer();

            new TextRenderer(r).RenderText(string.Empty, new TextOptions(new Font(font, 30)));
        }

        public static Font CreateFont(string text)
        {
            var fc = (IFontMetricsCollection)new FontCollection();
            Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
