// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tests.Fakes;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_47
{
    [Theory]
    [InlineData("hello world hello world hello world hello world")]
    public void LeftAlignedTextNewLineShouldNotStartWithWhiteSpace(string text)
    {
        Font font = CreateFont("\t x");

        TextMetrics metrics = TextMeasurer.Measure(text, new TextOptions(new Font(font, 30))
        {
            WrappingLength = 350,
            HorizontalAlignment = HorizontalAlignment.Left
        });

        int glyphIndex = 0;
        for (int lineIndex = 0; lineIndex < metrics.Lines.Count; lineIndex++)
        {
            int startGlyphIndex = glyphIndex;
            glyphIndex = AdvanceGlyphIndex(metrics.CharacterAdvances, glyphIndex, metrics.Lines[lineIndex].GraphemeCount);
            if (lineIndex > 0)
            {
                Assert.False(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[startGlyphIndex].Codepoint));
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

        TextMetrics metrics = TextMeasurer.Measure(text, new TextOptions(new Font(font, 30))
        {
            WrappingLength = 350,
            HorizontalAlignment = horizontalAlignment
        });

        int glyphIndex = 0;
        for (int lineIndex = 0; lineIndex < metrics.Lines.Count; lineIndex++)
        {
            int startGlyphIndex = glyphIndex;
            glyphIndex = AdvanceGlyphIndex(metrics.CharacterAdvances, glyphIndex, metrics.Lines[lineIndex].GraphemeCount);
            if (lineIndex > 0)
            {
                Assert.False(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[startGlyphIndex].Codepoint));
                Assert.False(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[startGlyphIndex - 1].Codepoint));
            }
        }
    }

    [Fact]
    public void WhiteSpaceAtStartOfTextShouldNotBeTrimmed()
    {
        Font font = CreateFont("\t x");
        string text = "   hello world hello world hello world";

        TextMetrics metrics = TextMeasurer.Measure(text, new TextOptions(new Font(font, 30))
        {
            WrappingLength = 350
        });

        Assert.True(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[0].Codepoint));
        Assert.True(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[1].Codepoint));
        Assert.True(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[2].Codepoint));
    }

    [Fact]
    public void WhiteSpaceAtTheEndOfTextShouldBeTrimmed()
    {
        Font font = CreateFont("\t x");
        string text = "hello world hello world hello world   ";

        TextMetrics metrics = TextMeasurer.Measure(text, new TextOptions(new Font(font, 30))
        {
            WrappingLength = 350
        });

        Assert.False(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[^1].Codepoint));
        Assert.False(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[^2].Codepoint));
        Assert.False(CodePoint.IsWhiteSpace(metrics.CharacterAdvances[^3].Codepoint));
    }

    private static int AdvanceGlyphIndex(IReadOnlyList<GlyphBounds> glyphs, int glyphIndex, int graphemeCount)
    {
        int consumed = 0;
        int lastGraphemeIndex = -1;
        while (glyphIndex < glyphs.Count && consumed < graphemeCount)
        {
            int graphemeIndex = glyphs[glyphIndex].GraphemeIndex;
            if (graphemeIndex != lastGraphemeIndex)
            {
                consumed++;
                lastGraphemeIndex = graphemeIndex;
            }

            glyphIndex++;
        }

        while (glyphIndex < glyphs.Count && glyphs[glyphIndex].GraphemeIndex == lastGraphemeIndex)
        {
            glyphIndex++;
        }

        return glyphIndex;
    }

    public static Font CreateFont(string text)
    {
        IFontMetricsCollection fc = (IFontMetricsCollection)new FontCollection();
        Font d = fc.AddMetrics(new FakeFontInstance(text), CultureInfo.InvariantCulture).CreateFont(12);
        return new Font(d, 1F);
    }
}
