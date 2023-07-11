// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TextLayoutTests
    {
        [Theory]
        [InlineData(LayoutMode.HorizontalTopBottom, true)]
        [InlineData(LayoutMode.HorizontalBottomTop, true)]
        [InlineData(LayoutMode.VerticalLeftRight, false)]
        [InlineData(LayoutMode.VerticalRightLeft, false)]
        [InlineData(LayoutMode.VerticalMixedLeftRight, false)]
        [InlineData(LayoutMode.VerticalMixedRightLeft, false)]
        public void CanDetectHorizontalLayoutMode(LayoutMode mode, bool vertical)
            => Assert.Equal(vertical, mode.IsHorizontal());

        [Theory]
        [InlineData(LayoutMode.HorizontalTopBottom, false)]
        [InlineData(LayoutMode.HorizontalBottomTop, false)]
        [InlineData(LayoutMode.VerticalLeftRight, true)]
        [InlineData(LayoutMode.VerticalRightLeft, true)]
        [InlineData(LayoutMode.VerticalMixedLeftRight, false)]
        [InlineData(LayoutMode.VerticalMixedRightLeft, false)]
        public void CanDetectVerticalLayoutMode(LayoutMode mode, bool vertical)
            => Assert.Equal(vertical, mode.IsVertical());

        [Theory]
        [InlineData(LayoutMode.HorizontalTopBottom, false)]
        [InlineData(LayoutMode.HorizontalBottomTop, false)]
        [InlineData(LayoutMode.VerticalLeftRight, false)]
        [InlineData(LayoutMode.VerticalRightLeft, false)]
        [InlineData(LayoutMode.VerticalMixedLeftRight, true)]
        [InlineData(LayoutMode.VerticalMixedRightLeft, true)]
        public void CanDetectVerticalMixedLayoutMode(LayoutMode mode, bool vertical)
            => Assert.Equal(vertical, mode.IsVerticalMixed());

        [Fact]
        public void FakeFontGetGlyph()
        {
            Font font = CreateFont("hello world");

            Assert.True(font.TryGetGlyphs(new CodePoint('h'), ColorFontSupport.None, out IReadOnlyList<Glyph> glyphs));
            Glyph glyph = glyphs[0];
            Assert.NotEqual(default, glyph);
        }

        [Theory]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Left,
            0,
            10)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Right,
            0,
            -320)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Center,
            0,
            -155)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Left,
            -60,
            10)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Right,
            -60,
            -320)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Center,
            -60,
            -155)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Left,
            -30,
            10)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Right,
            -30,
            -320)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Center,
            -30,
            -155)]
        public void CanAlignText(
            VerticalAlignment vertical,
            HorizontalAlignment horizontal,
            float top,
            float left)
        {
            const string text = "hello world\nhello";
            Font font = CreateFont(text);

            TextOptions options = new(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical
            };

            IReadOnlyList<GlyphLayout> glyphsToRender = TextLayout.GenerateLayout(text.AsSpan(), options);
            FontRectangle bound = TextMeasurer.GetBounds(glyphsToRender, options.Dpi);

            Assert.Equal(310, bound.Width, 3);
            Assert.Equal(40, bound.Height, 3);
            Assert.Equal(left, bound.Left, 3);
            Assert.Equal(top, bound.Top, 3);
        }

        [Theory]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Left,
            0,
            180)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Right,
            0,
            -320)]
        [InlineData(
            VerticalAlignment.Top,
            HorizontalAlignment.Center,
            0,
            -70)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Left,
            -60,
            180)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Right,
            -60,
            -320)]
        [InlineData(
            VerticalAlignment.Bottom,
            HorizontalAlignment.Center,
            -60,
            -70)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Left,
            -30,
            180)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Right,
            -30,
            -320)]
        [InlineData(
            VerticalAlignment.Center,
            HorizontalAlignment.Center,
            -30,
            -70)]
        public void CanAlignWithWrapping(
            VerticalAlignment vertical,
            HorizontalAlignment horizontal,
            float top,
            float left)
        {
            // Using a string with a forced line break shorter than the wrapping
            // width covers cases where the offset should be expanded for both single and multiple lines.
            const string text = "hello world\nhello";
            Font font = CreateFont(text);

            TextOptions options = new(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical,
                WrappingLength = 500,
                TextAlignment = TextAlignment.End
            };

            IReadOnlyList<GlyphLayout> glyphsToRender = TextLayout.GenerateLayout(text.AsSpan(), options);
            FontRectangle bound = TextMeasurer.GetBounds(glyphsToRender, options.Dpi);

            Assert.Equal(310, bound.Width, 3);
            Assert.Equal(40, bound.Height, 3);
            Assert.Equal(left, bound.Left, 3);
            Assert.Equal(top, bound.Top, 3);
        }

        [Fact]
        public unsafe void MeasureTextWithSpan()
        {
            Font font = CreateFont("hello");

            Span<char> text = stackalloc char[]
            {
                'h',
                'e',
                'l',
                'l',
                'o'
            };

            // 72 * emSize means 1pt = 1px
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(10, size.Height, 4);
            Assert.Equal(130, size.Width, 4);
        }

        [Theory]
        [InlineData("h", 10, 10)]
        [InlineData("he", 10, 40)]
        [InlineData("hel", 10, 70)]
        [InlineData("hello", 10, 130)]
        [InlineData("hello world", 10, 310)]
        [InlineData("hello world\nhello world", 40, 310)]
        [InlineData("hello\nworld", 40, 130)]
        public void MeasureText(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Fact]
        public void TryMeasureCharacterBounds()
        {
            const string text = "a b\nc";
            GlyphBounds[] expectedGlyphMetrics =
            {
                new(new CodePoint('a'), new FontRectangle(10, 0, 10, 10)),
                new(new CodePoint(' '), new FontRectangle(40, 0, 30, 10)),
                new(new CodePoint('b'), new FontRectangle(70, 0, 10, 10)),
                new(new CodePoint('c'), new FontRectangle(10, 30, 10, 10)),
            };
            Font font = CreateFont(text);

            Assert.True(TextMeasurer.TryMeasureCharacterBounds(
                text.AsSpan(),
                new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor },
                out ReadOnlySpan<GlyphBounds> bounds));

            // Newline should not be returned.
            Assert.Equal(text.Length - 1, bounds.Length);
            for (int i = 0; i < bounds.Length; i++)
            {
                GlyphBounds expected = expectedGlyphMetrics[i];
                GlyphBounds actual = bounds[i];
                Assert.Equal(expected.Codepoint, actual.Codepoint);

                // 4 dp as there is minor offset difference in the float values
                Assert.Equal(expected.Bounds.X, actual.Bounds.X, 4);
                Assert.Equal(expected.Bounds.Y, actual.Bounds.Y, 4);
                Assert.Equal(expected.Bounds.Height, actual.Bounds.Height, 4);
                Assert.Equal(expected.Bounds.Width, actual.Bounds.Width, 4);
            }
        }

        [Theory]
        [InlineData("hello world", 10, 310)]
        [InlineData(
            "hello world hello world hello world",
            70, // 30 actual line height * 2 + 10 actual height
            310)]
        [InlineData(// issue https://github.com/SixLabors/ImageSharp.Drawing/issues/115
            "这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。",
            160, // 30 actual line height * 2 + 10 actual height
            310)]
        public void MeasureTextWordWrappingHorizontalTopBottom(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.HorizontalTopBottom
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("hello world", 10, 310)]
        [InlineData(
            "hello world hello world hello world",
            70, // 30 actual line height * 2 + 10 actual height
            310)]
        [InlineData(// issue https://github.com/SixLabors/ImageSharp.Drawing/issues/115
            "这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。",
            160, // 30 actual line height * 2 + 10 actual height
            310)]
        public void MeasureTextWordWrappingHorizontalBottomTop(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.HorizontalBottomTop
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("hello world", 310, 10)]
        [InlineData("hello world hello world hello world", 310, 70)]
        [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 310, 160)]
        public void MeasureTextWordWrappingVerticalLeftRight(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalLeftRight
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("hello world", 310, 10)]
        [InlineData("hello world hello world hello world", 310, 70)]
        [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 310, 160)]
        public void MeasureTextWordWrappingVerticalRightLeft(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalRightLeft
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

        [Theory]
        [InlineData("hello world", 310, 10)]
        [InlineData("hello world hello world hello world", 310, 70)]
        [InlineData("这是一段长度超出设定的换行宽度的文本，但是没有在设定的宽度处换行。这段文本用于演示问题。希望可以修复。如果有需要可以联系我。", 310, 160)]
        public void MeasureTextWordWrappingVerticalMixedLeftRight(string text, float height, float width)
        {
            Font font = CreateFont(text);
            FontRectangle size = TextMeasurer.MeasureBounds(text, new TextOptions(font)
            {
                Dpi = font.FontMetrics.ScaleFactor,
                WrappingLength = 350,
                LayoutMode = LayoutMode.VerticalMixedLeftRight
            });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }

#if OS_WINDOWS
        [Theory]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.Standard, 100, 870)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.BreakAll, 120, 399)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.BreakWord, 120, 400)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalTopBottom, WordBreaking.KeepAll, 60, 699)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.Standard, 101, 870)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.BreakAll, 121, 399)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious Taumatawhakatangihangakoauauotamateaturipukakapikimaungahoronukupokaiwhenuakitanatahu グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.BreakWord, 121, 400)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious グレートブリテンおよび北アイルランド連合王国という言葉は本当に長い言葉", LayoutMode.HorizontalBottomTop, WordBreaking.KeepAll, 61, 699)]
        public void MeasureTextWordBreak(string text, LayoutMode layoutMode, WordBreaking wordBreaking, float height, float width)
        {
            // Testing using Windows only to ensure that actual glyphs are rendered
            // against known physically tested values.
            FontFamily arial = SystemFonts.Get("Arial");
            FontFamily jhengHei = SystemFonts.Get("Microsoft JhengHei");

            Font font = arial.CreateFont(20);
            FontRectangle size = TextMeasurer.MeasureSize(
                text,
                new TextOptions(font)
                {
                    WrappingLength = 400,
                    LayoutMode = layoutMode,
                    WordBreaking = wordBreaking,
                    FallbackFontFamilies = new[] { jhengHei }
                });

            Assert.Equal(width, size.Width, 4);
            Assert.Equal(height, size.Height, 4);
        }
#endif

        [Theory]
        [InlineData("ab", 477, 1081, false)] // no kerning rules defined for lowercase ab so widths should stay the same
        [InlineData("ab", 477, 1081, true)]
        [InlineData("AB", 465, 1033, false)] // width changes between kerning enabled or not
        [InlineData("AB", 465, 654, true)]
        public void MeasureTextWithKerning(string text, float height, float width, bool applyKerning)
        {
            var c = new FontCollection();
            Font font = c.Add(TestFonts.SimpleFontFileData()).CreateFont(12);
            FontRectangle size = TextMeasurer.MeasureBounds(
                text,
                new TextOptions(new Font(font, 1))
                {
                    Dpi = font.FontMetrics.ScaleFactor,
                    KerningMode = applyKerning ? KerningMode.Standard : KerningMode.None,
                });

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        [Theory]
        [InlineData("a", 100, 100, 125, 396)]
        public void LayoutWithLocation(string text, float x, float y, float expectedX, float expectedY)
        {
            var c = new FontCollection();
            Font font = c.Add(TestFonts.SimpleFontFileData()).CreateFont(12);

            var glyphRenderer = new GlyphRenderer();
            var renderer = new TextRenderer(glyphRenderer);
            renderer.RenderText(
                text,
                new TextOptions(new Font(font, 1))
                {
                    Dpi = font.FontMetrics.ScaleFactor,
                    Origin = new Vector2(x, y)
                });

            Assert.Equal(expectedX, glyphRenderer.GlyphRects[0].Location.X, 2);
            Assert.Equal(expectedY, glyphRenderer.GlyphRects[0].Location.Y, 2);
        }

        // https://github.com/SixLabors/Fonts/issues/244
        [Fact]
        public void MeasureTextLeadingFraction()
        {
            FontCollection c = new();
            Font font = c.Add(TestFonts.SimpleFontFileData()).CreateFont(12);
            TextOptions textOptions = new(font);
            FontRectangle measurement = TextMeasurer.MeasureSize("/ This will fail", textOptions);

            Assert.NotEqual(FontRectangle.Empty, measurement);
        }

        [Theory]
        [InlineData("hello world", 1)]
        [InlineData("hello world\nhello world", 2)]
        [InlineData("hello world\nhello world\nhello world", 3)]
        public void CountLines(string text, int usedLines)
        {
            Font font = CreateFont(text);
            int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(usedLines, count);
        }

        [Fact]
        public void CountLinesWithSpan()
        {
            Font font = CreateFont("hello\n!");

            Span<char> text = stackalloc char[]
            {
                'h',
                'e',
                'l',
                'l',
                'o',
                '\n',
                '!'
            };
            int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });

            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 25, 7)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 50, 7)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 100, 6)]
        [InlineData("This is a long and Honorificabilitudinitatibus califragilisticexpialidocious", 200, 6)]
        public void CountLinesWrappingLength(string text, int wrappingLength, int usedLines)
        {
            Font font = CreateFont(text);
            int count = TextMeasurer.CountLines(text, new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor, WrappingLength = wrappingLength });

            Assert.Equal(usedLines, count);
        }

        [Fact]
        public void BuildTextRuns_EmptyReturnsDefaultRun()
        {
            const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
            Font font = CreateFont(text);
            TextOptions options = new(font);

            IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

            Assert.True(runs.Count == 1);
            Assert.Equal(font, runs[0].Font);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(CodePoint.GetCodePointCount(text.AsSpan()), runs[0].End);
        }

        [Fact]
        public void BuildTextRuns_ReturnsCreatesInterimRuns()
        {
            const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
            Font font = CreateFont(text);
            Font font2 = CreateFont(text, 16);
            TextOptions options = new(font)
            {
                TextRuns = new List<TextRun>()
                {
                    new TextRun() { Start = 9, End = 23, Font = font2 },
                    new TextRun() { Start = 35, End = 54, Font = font2 },
                    new TextRun() { Start = 68, End = 70, Font = font2 },
                }
            };

            IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

            Assert.True(runs.Count == 7);

            Assert.Equal(0, runs[0].Start);
            Assert.Equal(9, runs[0].End);
            Assert.Equal(font, runs[0].Font);
            Assert.Equal(9, runs[0].Slice(text.AsSpan()).Length);

            Assert.Equal(9, runs[1].Start);
            Assert.Equal(23, runs[1].End);
            Assert.Equal(font2, runs[1].Font);
            Assert.Equal(14, runs[1].Slice(text.AsSpan()).Length);

            Assert.Equal(23, runs[2].Start);
            Assert.Equal(35, runs[2].End);
            Assert.Equal(font, runs[2].Font);
            Assert.Equal(12, runs[2].Slice(text.AsSpan()).Length);

            Assert.Equal(35, runs[3].Start);
            Assert.Equal(54, runs[3].End);
            Assert.Equal(font2, runs[3].Font);
            Assert.Equal(19, runs[3].Slice(text.AsSpan()).Length);

            Assert.Equal(54, runs[4].Start);
            Assert.Equal(68, runs[4].End);
            Assert.Equal(font, runs[4].Font);
            Assert.Equal(14, runs[4].Slice(text.AsSpan()).Length);

            Assert.Equal(68, runs[5].Start);
            Assert.Equal(70, runs[5].End);
            Assert.Equal(font2, runs[5].Font);
            Assert.Equal(2, runs[5].Slice(text.AsSpan()).Length);

            Assert.Equal(70, runs[6].Start);
            Assert.Equal(76, runs[6].End);
            Assert.Equal(font, runs[6].Font);
            Assert.Equal(6, runs[6].Slice(text.AsSpan()).Length);
        }

        [Fact]
        public void BuildTextRuns_PreventsOverlappingRun()
        {
            const string text = "This is a long and Honorificabilitudinitatibus califragilisticexpialidocious";
            Font font = CreateFont(text);
            TextOptions options = new(font)
            {
                TextRuns = new List<TextRun>()
                {
                    new TextRun() { Start = 0, End = 23 },
                    new TextRun() { Start = 1, End = 76 },
                }
            };

            IReadOnlyList<TextRun> runs = TextLayout.BuildTextRuns(text.AsSpan(), options);

            Assert.Equal(2, runs.Count);
            Assert.Equal(font, runs[0].Font);
            Assert.Equal(0, runs[0].Start);
            Assert.Equal(1, runs[0].End);

            Assert.Equal(font, runs[1].Font);
            Assert.Equal(1, runs[1].Start);
            Assert.Equal(76, runs[1].End);
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterCharacter_Horizontal(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterCharacter
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterAdvances(justifiedLine, options.Dpi, out ReadOnlySpan<GlyphBounds> justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.ToArray().Sum(x => x.Bounds.Width), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterAdvances(line, options.Dpi, out ReadOnlySpan<GlyphBounds> characterBounds);

            // All but the last justified character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (i == characterBounds.Length - 1)
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Width, characterBounds[i].Bounds.Width);
                }
                else
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Width > characterBounds[i].Bounds.Width);
                }
            }
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterWord_Horizontal(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterWord
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterAdvances(justifiedLine, options.Dpi, out ReadOnlySpan<GlyphBounds> justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.ToArray().Sum(x => x.Bounds.Width), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterAdvances(line, options.Dpi, out ReadOnlySpan<GlyphBounds> characterBounds);

            // All but the last justified whitespace character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (CodePoint.IsWhiteSpace(characterBounds[i].Codepoint) && i != characterBounds.Length - 1)
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Width > characterBounds[i].Bounds.Width);
                }
                else
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Width, characterBounds[i].Bounds.Width);
                }
            }
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterCharacter_Vertical(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                LayoutMode = LayoutMode.VerticalLeftRight,
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterCharacter
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterAdvances(justifiedLine, options.Dpi, out ReadOnlySpan<GlyphBounds> justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.ToArray().Sum(x => x.Bounds.Height), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterAdvances(line, options.Dpi, out ReadOnlySpan<GlyphBounds> characterBounds);

            // All but the last justified character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (i == characterBounds.Length - 1)
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Height, characterBounds[i].Bounds.Height);
                }
                else
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Height > characterBounds[i].Bounds.Height);
                }
            }
        }

        [Theory]
        [InlineData(TextDirection.LeftToRight)]
        [InlineData(TextDirection.RightToLeft)]
        public void TextJustification_InterWord_Vertical(TextDirection direction)
        {
            const string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ornare maximus vehicula. Duis nisi velit, dictum id mauris vitae, lobortis pretium quam. Quisque sed nisi pulvinar, consequat justo id, feugiat leo. Cras eu elementum dui.";
            const float wrappingLength = 400;
            const float pointSize = 20;
            Font font = CreateFont(text, pointSize);
            TextOptions options = new(font)
            {
                LayoutMode = LayoutMode.VerticalLeftRight,
                TextDirection = direction,
                WrappingLength = wrappingLength,
                TextJustification = TextJustification.InterWord
            };

            // Collect the first line so we can compare it to the target wrapping length.
            IReadOnlyList<GlyphLayout> justifiedGlyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> justifiedLine = CollectFirstLine(justifiedGlyphs);
            TextMeasurer.TryGetCharacterAdvances(justifiedLine, options.Dpi, out ReadOnlySpan<GlyphBounds> justifiedCharacterBounds);

            Assert.Equal(wrappingLength, justifiedCharacterBounds.ToArray().Sum(x => x.Bounds.Height), 4);

            // Now compare character widths.
            options.TextJustification = TextJustification.None;
            IReadOnlyList<GlyphLayout> glyphs = TextLayout.GenerateLayout(text.AsSpan(), options);
            IReadOnlyList<GlyphLayout> line = CollectFirstLine(glyphs);
            TextMeasurer.TryGetCharacterAdvances(line, options.Dpi, out ReadOnlySpan<GlyphBounds> characterBounds);

            // All but the last justified whitespace character advance should be greater than the
            // corresponding character advance.
            for (int i = 0; i < characterBounds.Length; i++)
            {
                if (CodePoint.IsWhiteSpace(characterBounds[i].Codepoint) && i != characterBounds.Length - 1)
                {
                    Assert.True(justifiedCharacterBounds[i].Bounds.Height > characterBounds[i].Bounds.Height);
                }
                else
                {
                    Assert.Equal(justifiedCharacterBounds[i].Bounds.Height, characterBounds[i].Bounds.Height);
                }
            }
        }

        public static TheoryData<char, FontRectangle> OpenSans_Data
            = new()
            {
                { '!', new(0, 0, 2, 10) },
                { '"', new(0, 0, 4, 5) },
                { '#', new(0, 0, 7, 9) },
                { '$', new(0, 0, 6, 10) },
                { '%', new(0, 0, 8, 9) },
                { '&', new(0, 0, 8, 9) },
                { '\'', new(0, 0, 2, 5) },
                { '(', new(0, 0, 3, 11) },
                { ')', new(0, 0, 3, 11) },
                { '*', new(0, 0, 6, 6) },
                { '+', new(0, 0, 6, 8) },
                { ',', new(0, 0, 2, 11) },
                { '-', new(0, 0, 3, 7) },
                { '.', new(0, 0, 2, 10) },
                { '/', new(0, 0, 4, 9) },
                { '0', new(0, 0, 6, 9) },
                { '1', new(0, 0, 4, 9) },
                { '2', new(0, 0, 6, 9) },
                { '3', new(0, 0, 6, 9) },
                { '4', new(0, 0, 6, 9) },
                { '5', new(0, 0, 6, 9) },
                { '6', new(0, 0, 6, 9) },
                { '7', new(0, 0, 6, 9) },
                { '8', new(0, 0, 6, 9) },
                { '9', new(0, 0, 6, 9) },
                { ':', new(0, 0, 2, 10) },
                { ';', new(0, 0, 2, 11) },
                { '<', new(0, 0, 6, 8) },
                { '=', new(0, 0, 6, 7) },
                { '>', new(0, 0, 6, 8) },
                { '?', new(0, 0, 5, 10) },
                { '@', new(0, 0, 9, 10) },
                { 'A', new(0, 0, 7, 9) },
                { 'B', new(0, 0, 6, 9) },
                { 'C', new(0, 0, 6, 9) },
                { 'D', new(0, 0, 7, 9) },
                { 'E', new(0, 0, 5, 9) },
                { 'F', new(0, 0, 5, 9) },
                { 'G', new(0, 0, 7, 9) },
                { 'H', new(0, 0, 7, 9) },
                { 'I', new(0, 0, 2, 9) },
                { 'J', new(0, 0, 2, 11) },
                { 'K', new(0, 0, 7, 9) },
                { 'L', new(0, 0, 5, 9) },
                { 'M', new(0, 0, 9, 9) },
                { 'N', new(0, 0, 7, 9) },
                { 'O', new(0, 0, 8, 9) },
                { 'P', new(0, 0, 6, 9) },
                { 'Q', new(0, 0, 8, 11) },
                { 'R', new(0, 0, 7, 9) },
                { 'S', new(0, 0, 6, 9) },
                { 'T', new(0, 0, 6, 9) },
                { 'U', new(0, 0, 7, 9) },
                { 'V', new(0, 0, 6, 9) },
                { 'W', new(0, 0, 10, 9) },
                { 'X', new(0, 0, 6, 9) },
                { 'Y', new(0, 0, 6, 9) },
                { 'Z', new(0, 0, 6, 9) },
                { '[', new(0, 0, 4, 11) },
                { '\\', new(0, 0, 4, 9) },
                { ']', new(0, 0, 3, 11) },
                { '^', new(0, 0, 6, 7) },
                { '_', new(0, 0, 5, 11) },
                { '`', new(0, 0, 3, 3) },
                { 'a', new(0, 0, 5, 9) },
                { 'b', new(0, 0, 6, 9) },
                { 'c', new(0, 0, 5, 9) },
                { 'd', new(0, 0, 6, 9) },
                { 'e', new(0, 0, 6, 9) },
                { 'f', new(0, 0, 4, 9) },
                { 'g', new(0, 0, 6, 12) },
                { 'h', new(0, 0, 6, 9) },
                { 'i', new(0, 0, 2, 9) },
                { 'j', new(0, 0, 2, 12) },
                { 'k', new(0, 0, 6, 9) },
                { 'l', new(0, 0, 2, 9) },
                { 'm', new(0, 0, 9, 9) },
                { 'n', new(0, 0, 6, 9) },
                { 'o', new(0, 0, 6, 9) },
                { 'p', new(0, 0, 6, 12) },
                { 'q', new(0, 0, 6, 12) },
                { 'r', new(0, 0, 4, 9) },
                { 's', new(0, 0, 5, 9) },
                { 't', new(0, 0, 4, 9) },
                { 'u', new(0, 0, 6, 9) },
                { 'v', new(0, 0, 5, 9) },
                { 'w', new(0, 0, 8, 9) },
                { 'x', new(0, 0, 6, 9) },
                { 'y', new(0, 0, 6, 12) },
                { 'z', new(0, 0, 5, 9) },
                { '{', new(0, 0, 4, 11) },
                { '|', new(0, 0, 4, 12) },
                { '}', new(0, 0, 4, 11) },
                { '~', new(0, 0, 6, 6) },
            };

        [Theory]
        [MemberData(nameof(OpenSans_Data))]
        public void TrueTypeHinting_CanHintSmallOpenSans(char c, FontRectangle expected)
        {
            TextOptions options = new(OpenSansTTF)
            {
                KerningMode = KerningMode.Standard,
                HintingMode = HintingMode.Standard
            };

            FontRectangle actual = TextMeasurer.MeasureSize(c.ToString(), options);
            Assert.Equal(expected, actual);

            options = new(OpenSansWoff)
            {
                KerningMode = KerningMode.Standard,
                HintingMode = HintingMode.Standard
            };

            actual = TextMeasurer.MeasureSize(c.ToString(), options);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanMeasureTextAdvance()
        {
            FontFamily family = new FontCollection().Add(TestFonts.OpenSansFile);
            family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

            TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
            {
                LineSpacing = 1F
            };

            const string text = "Hello World!";
            FontRectangle first = TextMeasurer.MeasureAdvance(text, options);

            // TODO: We should probably drop the 32bit test runner.
#if NET472
            if (Environment.Is64BitProcess)
            {
                Assert.Equal(new FontRectangle(0, 0, 11729, 2049), first);
            }
            else
            {
                Assert.Equal(new FontRectangle(0, 0, 11729, 2048), first);
            }
#else
            Assert.Equal(new FontRectangle(0, 0, 11729, 2049), first);
#endif

            options.LineSpacing = 2F;
            FontRectangle second = TextMeasurer.MeasureAdvance(text, options);
            Assert.Equal(new FontRectangle(0, 0, 11729, 4096), second);
        }

        [Fact]
        public void CanMeasureCharacterLayouts()
        {
            FontFamily family = new FontCollection().Add(TestFonts.OpenSansFile);
            family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

            TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
            {
                LineSpacing = 1.5F
            };

            const string text = "Hello World!";

            Assert.True(TextMeasurer.TryMeasureCharacterAdvances(text, options, out ReadOnlySpan<GlyphBounds> advances));
            Assert.True(TextMeasurer.TryMeasureCharacterSizes(text, options, out ReadOnlySpan<GlyphBounds> sizes));
            Assert.True(TextMeasurer.TryMeasureCharacterBounds(text, options, out ReadOnlySpan<GlyphBounds> bounds));

            Assert.Equal(advances.Length, sizes.Length);
            Assert.Equal(advances.Length, bounds.Length);

            for (int i = 0; i < advances.Length; i++)
            {
                GlyphBounds advance = advances[i];
                GlyphBounds size = sizes[i];
                GlyphBounds bound = bounds[i];

                Assert.Equal(advance.Codepoint, size.Codepoint);
                Assert.Equal(advance.Codepoint, bound.Codepoint);

                // Since this is a single line starting at 0,0 the following should be predictable.
                Assert.Equal(advance.Bounds.X, size.Bounds.X);
                Assert.Equal(size.Bounds.Bottom, bound.Bounds.Bottom);
            }
        }

        [Fact]
        public void CanMeasureMultilineCharacterLayouts()
        {
            FontFamily family = new FontCollection().Add(TestFonts.OpenSansFile);
            family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics);

            TextOptions options = new(family.CreateFont(metrics.UnitsPerEm))
            {
                LineSpacing = 1.5F
            };

            const string text = "A\nA\nA\nA";

            Assert.True(TextMeasurer.TryMeasureCharacterAdvances(text, options, out ReadOnlySpan<GlyphBounds> advances));
            Assert.True(TextMeasurer.TryMeasureCharacterSizes(text, options, out ReadOnlySpan<GlyphBounds> sizes));

            Assert.Equal(advances.Length, sizes.Length);

            GlyphBounds? lastAdvance = null;
            GlyphBounds? lastSize = null;
            for (int i = 0; i < advances.Length; i++)
            {
                GlyphBounds advance = advances[i];
                GlyphBounds size = sizes[i];

                Assert.Equal(advance.Codepoint, size.Codepoint);

                if (lastAdvance.HasValue)
                {
                    Assert.Equal(lastAdvance.Value.Bounds, advance.Bounds);
                    Assert.Equal(lastSize.Value.Bounds.Width, size.Bounds.Width, 2);
                    Assert.Equal(lastSize.Value.Bounds.Height, size.Bounds.Height, 2);
                }

                lastAdvance = advance;
                lastSize = size;
            }
        }

        private static readonly Font OpenSansTTF = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(10);
        private static readonly Font OpenSansWoff = new FontCollection().Add(TestFonts.OpenSansFile).CreateFont(10);

#if OS_WINDOWS
        public static TheoryData<char, FontRectangle> SegoeUi_Data
            = new()
            {
                { '!', new(0, 0, 2, 10) },
                { '"', new(0, 0, 4, 5) },
                { '#', new(0, 0, 6, 9) },
                { '$', new(0, 0, 5, 11) },
                { '%', new(0, 0, 8, 10) },
                { '&', new(0, 0, 8, 10) },
                { '\'', new(0, 0, 2, 5) },
                { '(', new(0, 0, 3, 11) },
                { ')', new(0, 0, 3, 11) },
                { '*', new(0, 0, 4, 6) },
                { '+', new(0, 0, 6, 9) },
                { ',', new(0, 0, 2, 11) },
                { '-', new(0, 0, 4, 7) },
                { '.', new(0, 0, 2, 10) },
                { '/', new(0, 0, 4, 11) },
                { '0', new(0, 0, 5, 10) },
                { '1', new(0, 0, 4, 10) },
                { '2', new(0, 0, 5, 10) },
                { '3', new(0, 0, 5, 10) },
                { '4', new(0, 0, 6, 10) },
                { '5', new(0, 0, 5, 10) },
                { '6', new(0, 0, 5, 10) },
                { '7', new(0, 0, 5, 10) },
                { '8', new(0, 0, 5, 10) },
                { '9', new(0, 0, 5, 10) },
                { ':', new(0, 0, 2, 10) },
                { ';', new(0, 0, 2, 11) },
                { '<', new(0, 0, 6, 9) },
                { '=', new(0, 0, 6, 8) },
                { '>', new(0, 0, 6, 9) },
                { '?', new(0, 0, 4, 10) },
                { '@', new(0, 0, 9, 11) },
                { 'A', new(0, 0, 7, 10) },
                { 'B', new(0, 0, 6, 10) },
                { 'C', new(0, 0, 6, 10) },
                { 'D', new(0, 0, 7, 10) },
                { 'E', new(0, 0, 5, 10) },
                { 'F', new(0, 0, 5, 10) },
                { 'G', new(0, 0, 7, 10) },
                { 'H', new(0, 0, 7, 10) },
                { 'I', new(0, 0, 2, 10) },
                { 'J', new(0, 0, 3, 10) },
                { 'K', new(0, 0, 6, 10) },
                { 'L', new(0, 0, 5, 10) },
                { 'M', new(0, 0, 9, 10) },
                { 'N', new(0, 0, 7, 10) },
                { 'O', new(0, 0, 8, 10) },
                { 'P', new(0, 0, 6, 10) },
                { 'Q', new(0, 0, 8, 11) },
                { 'R', new(0, 0, 6, 10) },
                { 'S', new(0, 0, 5, 10) },
                { 'T', new(0, 0, 6, 10) },
                { 'U', new(0, 0, 7, 10) },
                { 'V', new(0, 0, 7, 10) },
                { 'W', new(0, 0, 10, 10) },
                { 'X', new(0, 0, 6, 10) },
                { 'Y', new(0, 0, 6, 10) },
                { 'Z', new(0, 0, 6, 10) },
                { '[', new(0, 0, 3, 11) },
                { '\\', new(0, 0, 4, 11) },
                { ']', new(0, 0, 3, 11) },
                { '^', new(0, 0, 6, 7) },
                { '_', new(0, 0, 5, 11) },
                { '`', new(0, 0, 3, 4) },
                { 'a', new(0, 0, 5, 10) },
                { 'b', new(0, 0, 6, 10) },
                { 'c', new(0, 0, 5, 10) },
                { 'd', new(0, 0, 6, 10) },
                { 'e', new(0, 0, 5, 10) },
                { 'f', new(0, 0, 4, 10) },
                { 'g', new(0, 0, 6, 12) },
                { 'h', new(0, 0, 5, 10) },
                { 'i', new(0, 0, 2, 10) },
                { 'j', new(0, 0, 2, 12) },
                { 'k', new(0, 0, 5, 10) },
                { 'l', new(0, 0, 2, 10) },
                { 'm', new(0, 0, 8, 10) },
                { 'n', new(0, 0, 5, 10) },
                { 'o', new(0, 0, 6, 10) },
                { 'p', new(0, 0, 6, 12) },
                { 'q', new(0, 0, 6, 12) },
                { 'r', new(0, 0, 4, 10) },
                { 's', new(0, 0, 4, 10) },
                { 't', new(0, 0, 4, 10) },
                { 'u', new(0, 0, 5, 10) },
                { 'v', new(0, 0, 5, 10) },
                { 'w', new(0, 0, 8, 10) },
                { 'x', new(0, 0, 5, 10) },
                { 'y', new(0, 0, 5, 12) },
                { 'z', new(0, 0, 5, 10) },
                { '{', new(0, 0, 3, 11) },
                { '|', new(0, 0, 2, 12) },
                { '}', new(0, 0, 3, 11) },
                { '~', new(0, 0, 6, 7) }
            };

        [Theory]
        [MemberData(nameof(SegoeUi_Data))]
        public void TrueTypeHinting_CanHintSmallSegoeUi(char c, FontRectangle expected)
        {
            TextOptions options = new(SegoeUi)
            {
                KerningMode = KerningMode.Standard,
                HintingMode = HintingMode.Standard
            };

            FontRectangle actual = TextMeasurer.MeasureSize(c.ToString(), options);
            Assert.Equal(expected, actual);
        }

        private static readonly Font SegoeUi = SystemFonts.CreateFont("Segoe Ui", 10);
#endif

        private static IReadOnlyList<GlyphLayout> CollectFirstLine(IReadOnlyList<GlyphLayout> glyphs)
        {
            List<GlyphLayout> line = new()
            {
                glyphs[0]
            };

            for (int i = 1; i < glyphs.Count; i++)
            {
                if (glyphs[i].IsStartOfLine)
                {
                    break;
                }

                line.Add(glyphs[i]);
            }

            return line;
        }

#if OS_WINDOWS
        [Fact]
        public FontRectangle BenchmarkTest()
        {
            Font font = Arial;
            return TextMeasurer.MeasureSize("The quick brown fox jumped over the lazy dog", new TextOptions(font) { Dpi = font.FontMetrics.ScaleFactor });
        }

        private static readonly Font Arial = SystemFonts.CreateFont("Arial", 12);
#endif

        public static Font CreateFont(string text)
        {
            var fc = (IFontMetricsCollection)new FontCollection();
            Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, 1);
        }

        public static Font CreateFont(string text, float pointSize)
        {
            var fc = (IFontMetricsCollection)new FontCollection();
            Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
            return new Font(d, pointSize);
        }
    }
}
