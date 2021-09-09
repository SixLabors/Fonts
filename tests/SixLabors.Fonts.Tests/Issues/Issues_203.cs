// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_203
    {
        [Fact]
        public void CanParseVersion1Font()
        {
            var font = FontDescription.LoadDescription(TestFonts.Version1Font);
            Assert.NotNull(font);
        }
    }
}
