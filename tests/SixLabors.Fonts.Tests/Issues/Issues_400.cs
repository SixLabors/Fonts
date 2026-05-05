// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;

#if SUPPORTS_DRAWING
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
#endif

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_400
{
    [Fact]
    public void RenderingTextIncludesAllGlyphs()
    {
        TextOptions options = new(TestFonts.GetFont(TestFonts.Arial, 16 * 2))
        {
            WrappingLength = 1900
        };

        StringBuilder stringBuilder = new();
        string text = stringBuilder
            .AppendLine()
            .AppendLine("                NEWS_CATEGORY=EWF&NEWS_HASH=4b298ff9277ef9fdf515356be95ea3caf57cd36&OFFSET=0&SEARCH_VALUE=CA88105E1088&ID_NEWS")
            .Append("          ")
            .ToString();

        int lineCount = TextMeasurer.CountLines(text, options);
        Assert.Equal(4, lineCount);

#if SUPPORTS_DRAWING
        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            afterAction: image => DrawBoundsOverlay(image, text, options));
#else
        TextLayoutTestUtilities.TestLayout(text, options);
#endif
    }

#if SUPPORTS_DRAWING
    private static void DrawBoundsOverlay(Image<Rgba32> image, string text, TextOptions options)
    {
        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
        FontRectangle renderableBounds = TextMeasurer.MeasureRenderableBounds(text, options);

        image.Mutate(x =>
        {
            DrawRectangle(x, renderableBounds, Color.Magenta, 3);
            DrawRectangle(x, advance, Color.DeepSkyBlue, 2);
            DrawRectangle(x, bounds, Color.Lime, 2);
        });

        ReadOnlySpan<GlyphBounds> measuredGlyphBounds = TextMeasurer.MeasureGlyphBounds(text, options);
        GlyphBounds[] glyphBounds = measuredGlyphBounds.ToArray();

        image.Mutate(x =>
        {
            for (int i = 0; i < glyphBounds.Length; i++)
            {
                DrawRectangle(x, glyphBounds[i].Bounds, Color.Orange, 1);
            }
        });
    }

    private static void DrawRectangle(IImageProcessingContext context, FontRectangle rectangle, Color color, float thickness)
    {
        if (rectangle.IsEmpty)
        {
            return;
        }

        context.Draw(
            color,
            thickness,
            new RectangularPolygon(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
    }
#endif
}
