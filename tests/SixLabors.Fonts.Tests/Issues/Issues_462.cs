// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_462
{
    private readonly FontFamily emoji = new FontCollection().Add(TestFonts.NotoColorEmojiRegular);
    private readonly FontFamily noto = new FontCollection().Add(TestFonts.NotoSansRegular);

    [Fact]
    public void CanRenderEmojiFont_With_COLRv1()
    {
        Font font = this.emoji.CreateFont(100);
        const string text = "a😨 b😅\r\nc🥲 d🤩";

        // There are too many metrics to validate here so we just ensure no exceptions are thrown
        // and the rendering looks correct by inspecting the snapshot.
        TextLayoutTestUtilities.TestLayout(
            text,
            new TextOptions(font)
            {
                ColorFontSupport = ColorFontSupport.ColrV1,
                LineSpacing = 1.8F,
                FallbackFontFamilies = new[] { this.noto },
                TextRuns = new List<TextRun>
                {
                    new()
                    {
                        Start = 0,
                        End = text.GetGraphemeCount(),
                        TextDecorations = TextDecorations.Strikeout | TextDecorations.Underline | TextDecorations.Overline
                    }
                }
            },
            includeGeometry: true);
    }

    [Fact]
    public void CanRenderEmojiFont_With_SVG()
    {
        Font font = this.emoji.CreateFont(100);
        const string text = "a😨 b😅\r\nc🥲 d🤩";

        TextLayoutTestUtilities.TestLayout(
            text,
            new TextOptions(font)
            {
                ColorFontSupport = ColorFontSupport.Svg,
                LineSpacing = 1.8F,
                FallbackFontFamilies = new[] { this.noto },
                TextRuns = new List<TextRun>
                {
                    new()
                    {
                        Start = 0,
                        End = text.GetGraphemeCount(),
                        TextDecorations = TextDecorations.Strikeout | TextDecorations.Underline | TextDecorations.Overline
                    }
                }
            },
            includeGeometry: true);
    }
}
