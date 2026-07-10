// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_405
{
    private static List<DecorationSegment> RenderDecorations(
        string text,
        float tracking,
        TextDecorations decorations,
        LayoutMode layoutMode = LayoutMode.HorizontalTopBottom)
    {
        Font font = TestFonts.GetFont(TestFonts.OpenSansFile, 64);
        TextOptions options = new(font)
        {
            Dpi = 72,
            Tracking = tracking,
            LayoutMode = layoutMode,
            TextRuns =
            [
                new()
                {
                    Start = 0,
                    End = text.GetGraphemeCount(),
                    TextDecorations = decorations
                }
            ]
        };

        DecorationRecordingRenderer renderer = new();
        TextRenderer.RenderTextTo(renderer, text, options);
        return renderer.Decorations;
    }

    [Fact]
    public void Underline_WithTracking_IsContinuousAcrossGlyphs()
    {
        List<DecorationSegment> segments = RenderDecorations("AB", 2, TextDecorations.Underline);

        Assert.Equal(2, segments.Count);
        Assert.All(segments, s => Assert.Equal(TextDecorations.Underline, s.Decoration));

        // The underline beneath the first glyph must extend all the way to the start of the
        // underline beneath the second glyph so that no gap appears across the tracking space.
        Assert.Equal(segments[0].End.X, segments[1].Start.X, 0.5F);
    }

    [Fact]
    public void Underline_WithoutTracking_IsContinuousAcrossGlyphs()
    {
        List<DecorationSegment> segments = RenderDecorations("AB", 0, TextDecorations.Underline);

        Assert.Equal(2, segments.Count);
        Assert.Equal(segments[0].End.X, segments[1].Start.X, 0.5F);
    }

    [Fact]
    public void Underline_TrackingExtendsDecorationByTrackingAdvance()
    {
        const float tracking = 2;
        List<DecorationSegment> withoutTracking = RenderDecorations("AB", 0, TextDecorations.Underline);
        List<DecorationSegment> withTracking = RenderDecorations("AB", tracking, TextDecorations.Underline);

        float lengthWithout = withoutTracking[0].End.X - withoutTracking[0].Start.X;
        float lengthWith = withTracking[0].End.X - withTracking[0].Start.X;

        // Tracking adds (tracking * pointSize * dpi / 72) device pixels of advance to each glyph.
        // With a DPI of 72 this equals tracking * pointSize.
        const float expectedExtra = tracking * 64;
        Assert.Equal(expectedExtra, lengthWith - lengthWithout, 1F);
    }

    [Fact]
    public void StrikeoutAndOverline_WithTracking_AreContinuousAcrossGlyphs()
    {
        List<DecorationSegment> segments = RenderDecorations(
            "AB",
            2,
            TextDecorations.Strikeout | TextDecorations.Overline);

        List<DecorationSegment> strikeout = segments.FindAll(s => s.Decoration == TextDecorations.Strikeout);
        List<DecorationSegment> overline = segments.FindAll(s => s.Decoration == TextDecorations.Overline);

        Assert.Equal(2, strikeout.Count);
        Assert.Equal(2, overline.Count);
        Assert.Equal(strikeout[0].End.X, strikeout[1].Start.X, 0.5F);
        Assert.Equal(overline[0].End.X, overline[1].Start.X, 0.5F);
    }

    [Fact]
    public void Decorations_WithTracking_AreContinuousInVerticalLayout()
    {
        List<DecorationSegment> segments = RenderDecorations(
            "AB",
            2,
            TextDecorations.Underline | TextDecorations.Strikeout | TextDecorations.Overline,
            LayoutMode.VerticalLeftRight);

        // Vertical decorations run along the Y axis; each type must reach the next glyph without a gap.
        foreach (TextDecorations decoration in new[] { TextDecorations.Underline, TextDecorations.Strikeout, TextDecorations.Overline })
        {
            List<DecorationSegment> ofType = segments.FindAll(s => s.Decoration == decoration);
            Assert.Equal(2, ofType.Count);
            Assert.Equal(ofType[0].End.Y, ofType[1].Start.Y, 0.5F);
        }
    }

    private readonly record struct DecorationSegment(
        TextDecorations Decoration,
        Vector2 Start,
        Vector2 End,
        float Thickness);

    private sealed class DecorationRecordingRenderer : GlyphRenderer
    {
        public List<DecorationSegment> Decorations { get; } = [];

        public override void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
            => this.Decorations.Add(new DecorationSegment(textDecorations, start, end, thickness));
    }
}
