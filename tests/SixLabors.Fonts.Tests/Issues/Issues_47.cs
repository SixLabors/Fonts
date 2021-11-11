// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_47
    {
        [Theory]
        [InlineData("hello world hello world hello world hello world")]
        public void LeftAlignedTextNewLineShouldNotStartWithWhiteSpace(string text)
        {
            Font font = CreateFont("\t x");

            var r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text.AsSpan(), new TextOptions(new Font(font, 30), 72)
            {
                WrappingLength = 350,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            float lineYPos = layout[0].Location.Y;
            foreach (GlyphLayout glyph in layout)
            {
                if (lineYPos != glyph.Location.Y)
                {
                    Assert.False(glyph.IsWhiteSpace());
                    lineYPos = glyph.Location.Y;
                }
            }
        }

        [Theory]
        [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Left)]
        [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Right)]
        [InlineData("hello world hello world hello world hello world", HorizontalAlignment.Center)]
        [InlineData("hello   world   hello   world   hello   hello   world", HorizontalAlignment.Left)]
        public void NewWrappedLinesShouldNotStartOrEndWithWhiteSpace(string text, HorizontalAlignment horizontalAlignment)
        {
            Font font = CreateFont("\t x");

            var r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text.AsSpan(), new TextOptions(new Font(font, 30), 72)
            {
                WrappingLength = 350,
                HorizontalAlignment = horizontalAlignment
            });

            float lineYPos = layout[0].Location.Y;
            for (int i = 0; i < layout.Count; i++)
            {
                GlyphLayout glyph = layout[i];
                if (lineYPos != glyph.Location.Y)
                {
                    Assert.False(glyph.IsWhiteSpace());
                    Assert.False(layout[i - 1].IsWhiteSpace());
                    lineYPos = glyph.Location.Y;
                }
            }
        }

        [Fact]
        public void WhiteSpaceAtStartOfTextShouldNotBeTrimmed()
        {
            Font font = CreateFont("\t x");
            string text = "   hello world hello world hello world";

            var r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text.AsSpan(), new TextOptions(new Font(font, 30), 72)
            {
                WrappingLength = 350
            });

            Assert.True(layout[0].IsWhiteSpace());
            Assert.True(layout[1].IsWhiteSpace());
            Assert.True(layout[2].IsWhiteSpace());
        }

        [Fact]
        public void WhiteSpaceAtTheEndOfTextShouldBeTrimmed()
        {
            Font font = CreateFont("\t x");
            string text = "hello world hello world hello world   ";

            var r = new GlyphRenderer();

            IReadOnlyList<GlyphLayout> layout = new TextLayout().GenerateLayout(text.AsSpan(), new TextOptions(new Font(font, 30), 72)
            {
                WrappingLength = 350
            });

            Assert.False(layout[layout.Count - 1].IsWhiteSpace());
            Assert.False(layout[layout.Count - 2].IsWhiteSpace());
            Assert.False(layout[layout.Count - 3].IsWhiteSpace());
        }

        public static Font CreateFont(string text)
        {
            var fc = (IFontMetricsCollection)new FontCollection();
            Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }
    }
}
