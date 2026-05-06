// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_431
{
    [Fact]
    public void ShouldNotInsertExtraLineBreaks()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            const string text = "- Lorem ipsullll\ndolor sit amet\n-consectetur elit";

            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            TextLayoutTestUtilities.TestLayout(text, options);

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(4, lineCount);

            TextMetrics metrics = TextMeasurer.Measure(text, options);

            // The collection contains laid-out glyph entries. These fixtures shape one glyph per source
            // code point, so the preserved line-edge whitespace raises the count from the old trimmed 46.
            Assert.Equal(49, metrics.MeasureGlyphAdvances().Length);
            AssertLineBreakCount(metrics.MeasureGlyphAdvances().Span, 2);

            // Hard breaks are preserved at their original UTF-16 source indices.
            AssertPreservedLineBreakAdvance(metrics.MeasureGlyphAdvances().Span, 16);
            AssertPreservedLineBreakAdvance(metrics.MeasureGlyphAdvances().Span, 31);
        }
    }

    [Fact]
    public void ShouldNotInsertExtraLineBreaks_2()
    {
        if (SystemFonts.TryGet("Arial", out FontFamily family))
        {
            Font font = family.CreateFont(60);
            const string text = "- Lorem ipsullll dolor sit amet\n-consectetur elit";

            TextOptions options = new(font)
            {
                Origin = new Vector2(50, 20),
                WrappingLength = 400,
            };

            TextLayoutTestUtilities.TestLayout(text, options);

            int lineCount = TextMeasurer.CountLines(text, options);
            Assert.Equal(4, lineCount);

            TextMetrics metrics = TextMeasurer.Measure(text, options);

            // The collection contains laid-out glyph entries. These fixtures shape one glyph per source
            // code point, so the preserved line-edge whitespace raises the count from the old trimmed 46.
            Assert.Equal(49, metrics.MeasureGlyphAdvances().Length);
            AssertLineBreakCount(metrics.MeasureGlyphAdvances().Span, 1);

            // Hard breaks are preserved at their original UTF-16 source indices.
            AssertPreservedLineBreakAdvance(metrics.MeasureGlyphAdvances().Span, 31);
        }
    }

    private static void AssertLineBreakCount(ReadOnlySpan<GlyphBounds> advances, int expected)
    {
        int count = 0;
        for (int i = 0; i < advances.Length; i++)
        {
            if (CodePoint.IsNewLine(advances[i].Codepoint))
            {
                count++;
            }
        }

        Assert.Equal(expected, count);
    }

    private static void AssertPreservedLineBreakAdvance(ReadOnlySpan<GlyphBounds> advances, int stringIndex)
    {
        for (int i = 0; i < advances.Length; i++)
        {
            GlyphBounds advance = advances[i];
            if (advance.StringIndex != stringIndex)
            {
                continue;
            }

            Assert.True(CodePoint.IsNewLine(advance.Codepoint));
            Assert.True(advance.Bounds.Width > 0 || advance.Bounds.Height > 0);
            return;
        }

        Assert.Fail($"No preserved line break glyph found at string index {stringIndex}.");
    }
}
