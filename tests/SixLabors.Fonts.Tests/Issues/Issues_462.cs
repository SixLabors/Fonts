// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_462
{
    private readonly FontFamily emoji = TestFonts.GetFontFamily(TestFonts.NotoColorEmojiRegular);
    private readonly FontFamily noto = TestFonts.GetFontFamily(TestFonts.NotoSansRegular);

    [Fact]
    public void CanRenderEmojiFont_With_COLRv1()
    {
        Font font = this.emoji.CreateFont(100);
        const string text = "a😨 b😅\r\nc🥲 d🤩";

        TextOptions options = new(font)
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
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);
        Assert.Equal(10, renderer.GlyphKeys.Count);

        // There are too many metrics to validate here so we just ensure no exceptions are thrown
        // and the rendering looks correct by inspecting the snapshot.
        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: true,
            customDecorations: true);
    }

    [Fact]
    public void CanRenderEmojiFont_With_SVG()
    {
        Font font = this.emoji.CreateFont(100);
        const string text = "a😨 b😅\r\nc🥲 d🤩";

        TextOptions options = new(font)
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
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);
        Assert.Equal(10, renderer.GlyphKeys.Count);

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: true,
            customDecorations: true);
    }

    [Theory]
    [InlineData("robot", "🤖")]
    [InlineData("clown", "🤡")]
    [InlineData("leg", "🦿")]
    [InlineData("mending-heart", "❤️‍🩹")]
    [InlineData("heart-on-fire", "❤️‍🔥")]
    public void CanRenderProblemEmojiTransforms_With_COLRv1(string name, string text)
        => this.AssertCanRenderProblemEmojiTransforms(name, text, ColorFontSupport.ColrV1);

    [Theory]
    [InlineData("robot", "🤖")]
    [InlineData("clown", "🤡")]
    [InlineData("leg", "🦿")]
    [InlineData("mending-heart", "❤️‍🩹")]
    [InlineData("heart-on-fire", "❤️‍🔥")]
    public void CanRenderProblemEmojiTransforms_With_SVG(string name, string text)
        => this.AssertCanRenderProblemEmojiTransforms(name, text, ColorFontSupport.Svg);

    [Fact]
    public void CanRenderEmojiSanityMatrix_With_COLRv1()
        => this.AssertCanRenderEmojiSanityMatrix(ColorFontSupport.ColrV1);

    [Fact(Skip = "Local Only, Parsing the full font is slow.")]
    public void CanRenderEmojiSanityMatrix_With_SVG()
        => this.AssertCanRenderEmojiSanityMatrix(ColorFontSupport.Svg);

    [Fact]
    public void Svg_UsesDefaultBlackFillForUnspecifiedCatFaceDetails()
    {
        Font font = this.emoji.CreateFont(256);

        TextOptions options = new(font)
        {
            ColorFontSupport = ColorFontSupport.Svg,
            FallbackFontFamilies = new[] { this.noto },
        };

        LayerCaptureRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "😸", options);

        Assert.Single(renderer.GlyphKeys);
        Assert.True(renderer.SolidLayers.Count(x => x.Color == GlyphColor.Black && Math.Abs(x.Opacity - 1F) < 0.001F) >= 9);
    }

    [Fact]
    public void Svg_PropagatesUseOpacityToReferencedGeometry()
    {
        Font font = this.emoji.CreateFont(256);

        TextOptions options = new(font)
        {
            ColorFontSupport = ColorFontSupport.Svg,
            FallbackFontFamilies = new[] { this.noto },
        };

        LayerCaptureRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, "🧐", options);

        Assert.Single(renderer.GlyphKeys);
        Assert.True(GlyphColor.TryParseHex("#CCCCCC", out GlyphColor monocleColor));
        Assert.Contains(renderer.SolidLayers, x => x.Color == monocleColor && Math.Abs(x.Opacity - 0.5F) < 0.001F);
    }

    private void AssertCanRenderProblemEmojiTransforms(
        string name,
        string text,
        ColorFontSupport support,
        [CallerMemberName] string test = "")
    {
        Font font = this.emoji.CreateFont(256);

        TextOptions options = new(font)
        {
            ColorFontSupport = support,
            FallbackFontFamilies = new[] { this.noto },
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);
        Assert.Single(renderer.GlyphKeys);

        TextLayoutTestUtilities.TestLayout(text, options, test: test, properties: name);
    }

    private void AssertCanRenderEmojiSanityMatrix(
        ColorFontSupport support,
        [CallerMemberName] string test = "")
    {
        Font font = this.emoji.CreateFont(64);
        const string text =
            "😀😃😄😁😆😅😂🤣😭😉😗😙\n" +
            "😚😘🥰😍🤩🥳🙃🙂🥲🥹😋😛\n" +
            "😝😜🤪😇😊☺️😏😌😔😑😐😶\n" +
            "🫡🤔🤫🫢🤭🥱🤗🫣😱🤨🧐😒\n" +
            "🙄😮‍💨😤😠😡🤬🥺😟😥😢☹️🙁\n" +
            "🫤😕🤐😰😨😧😦😮😯😲😳🤯\n" +
            "😬😓😞😖😣😩😫😵😵‍💫🫥😴😪\n" +
            "🤤🌛🌜🌚🌝🌞🫠😶‍🌫️🥴🥵🥶🤢\n" +
            "🤮🤧🤒🤕😷🤠🤑😎🤓🥸🤥🤡\n" +
            "👻💩👽🤖🎃😈👿👹👺🔥💫⭐\n" +
            "🌟✨💥💯💢💨💦🫧💤🕳️🎉🎊\n" +
            "🙈🙉🙊😺😸😹😻😼😽🙀😿😾\n" +
            "❤️🧡💛💚💙💜🤎🖤🤍♥️💘💝\n" +
            "💖💗💓💞💕💌💟❣️❤️‍🩹💔❤️‍🔥💋\n" +
            "🫂👥👤🗣️👣🧠🫀🫁🩸🦠🦷🦴\n" +
            "☠️💀👀👁️👄🫦👅👃👂🦻🦶🦵\n" +
            "🦿🦾💪👍👎👏🫶🙌👐🤲🤝🤜\n" +
            "🤛✊👊🫳🫴🫱🫲🤚👋🖐️✋🖖\n" +
            "🤟🤘✌️🤞🫰🤙🤌🤏👌🖕☝️👆\n" +
            "👇👉👈🫵✍️🤳🙏💅";

        TextOptions options = new(font)
        {
            ColorFontSupport = support,
            FallbackFontFamilies = new[] { this.noto },
            LineSpacing = 1.15F,
        };

        GlyphRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);
        Assert.NotEmpty(renderer.GlyphKeys);

        TextLayoutTestUtilities.TestLayout(text, options, test: test, properties: "full-string");
    }

    private sealed class LayerCaptureRenderer : GlyphRenderer
    {
        public List<(GlyphColor Color, float Opacity)> SolidLayers { get; } = [];

        public override void BeginLayer(Paint paint, FillRule fillRule, ClipQuad? clipBounds)
        {
            if (paint is SolidPaint solidPaint)
            {
                this.SolidLayers.Add((solidPaint.Color, solidPaint.Opacity));
            }

            base.BeginLayer(paint, fillRule, clipBounds);
        }
    }
}
