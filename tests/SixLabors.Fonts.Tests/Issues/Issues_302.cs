// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_302
    {
#if OS_WINDOWS
        [Fact]
        public void DoesNotThrowOutOfBounds()
        {
            const string content = "فِيلْمٌ";
            FontFamily fontFamily = SystemFonts.Get("Arial");
            Font font = fontFamily.CreateFont(16, FontStyle.Regular);
            TextOptions renderOptions = new(font);

            Assert.True(TextMeasurer.TryMeasureCharacterBounds(content, renderOptions, out ReadOnlySpan<GlyphBounds> _));
        }
#endif
    }
}
