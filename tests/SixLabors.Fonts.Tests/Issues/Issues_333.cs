// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_333
    {
        [Fact]
        public void DoesNotThrowMissingTableException()
        {
            const string text = "文字測試文字測試文字測試文字測試文字測試";
            Font font = new FontCollection().Add(TestFonts.PMINGLIUFile).CreateFont(1024);
            TextMeasurer.MeasureSize(text, new TextOptions(font));
        }
    }
}
