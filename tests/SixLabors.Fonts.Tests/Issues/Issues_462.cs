// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_462
{
     private readonly FontFamily emoji = new FontCollection().Add(TestFonts.NotoColorEmojiRegular);

    //private readonly FontFamily emoji = new FontCollection().Add(TestFonts.SegoeuiEmojiFile);

    private readonly FontFamily noto = new FontCollection().Add(TestFonts.NotoSansRegular);

    [Fact]
    public void CanRenderEmojiFont()
    {
        // "😀 😃 😄 😁 😆 😅 😂 🤣 🥲 ☺️ 😊 😇 🙂 🙃 😉 😌 😍 🥰 😘 😗 😙 😚 😋 😛 😝 😜 🤪 🤨 🧐 🤓 😎 🥸 🤩 🥳",
        Font font = this.emoji.CreateFont(100);
        const string text = "T🙂E🙂S🤣T😨";

        TextLayoutTestUtilities.TestLayout(
            text,
            new TextOptions(font)
            {
                Origin = new Vector2(0, 200),
                // WrappingLength = 100,
                LineSpacing = 1.8F,
                FallbackFontFamilies = new[] { this.noto },
                TextRuns = new List<TextRun> { new() { Start = 0, End = text.GetGraphemeCount(), TextDecorations = TextDecorations.Overline | TextDecorations.Strikeout | TextDecorations.Overline } },
                // ColorFontSupport = ColorFontSupport.None
            });
    }
}
