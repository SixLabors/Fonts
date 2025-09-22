// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_462
{
    private readonly FontFamily emoji = new FontCollection().Add(TestFonts.NotoColorEmojiRegular);

    [Fact]
    public void CanRenderEmojiFont()
    {
        // "😀 😃 😄 😁 😆 😅 😂 🤣 🥲 ☺️ 😊 😇 🙂 🙃 😉 😌 😍 🥰 😘 😗 😙 😚 😋 😛 😝 😜 🤪 🤨 🧐 🤓 😎 🥸 🤩 🥳",
        Font font = this.emoji.CreateFont(100);
        TextLayoutTestUtilities.TestLayout(
            "🙂🤣😨",
            new TextOptions(font)
            {
               // Origin = new Vector2(0, 200),
                // WrappingLength = 100,
                LineSpacing = 1.2F
            });
    }
}
