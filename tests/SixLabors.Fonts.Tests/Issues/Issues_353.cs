// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_353
{
    [Fact]
    public void Test_Issue_353()
    {
        Font font = TestFonts.GetFont(TestFonts.EbGaramond, 30);

        string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec a diam lectus. Sed sit amet ipsum mauris.";
        TextOptions options = new(font)
        {
            WrappingLength = 400,
            LineSpacing = 1.6f
        };

        LineMetrics[] l = TextMeasurer.GetLineMetrics(text, options).ToArray();

        // Numeric assertions for line metrics to complement the visual test.
        Assert.NotEmpty(l);
        float expectedLineHeight = l[0].LineHeight;
        foreach (LineMetrics m in l)
        {
            // Line height must be positive and consistent across lines.
            Assert.True(m.LineHeight > 0, "LineHeight should be positive.");
            Assert.Equal(expectedLineHeight, m.LineHeight, 3);

            // Ascender/Baseline/Descender should be ordered within the line box.
            Assert.InRange(m.Ascender, 0, m.LineHeight);
            Assert.InRange(m.Baseline, m.Ascender, m.LineHeight);
            Assert.InRange(m.Descender, m.Baseline, m.LineHeight);

            // Horizontal metrics should describe a valid line box.
            Assert.True(m.Extent.X > 0, "Extent.X should be positive.");
            Assert.True(m.Extent.Y > 0, "Extent.Y should be positive.");
        }

        void DrawLineMetrics(Image<Rgba32> image)
        {
            // Draw four separate lines for ascender(orange), baseline (red), descender (blue),
            // and line bottom (green).
            for (int i = 0; i < l.Length; i++)
            {
                LineMetrics m = l[i];

                float ascent = m.Start.Y + m.Ascender;
                float baseline = m.Start.Y + m.Baseline;
                float descender = m.Start.Y + m.Descender;
                float lineBottom = m.Start.Y + m.LineHeight;
                float start = m.Start.X;
                float end = m.Start.X + m.Extent.X;

                image.Mutate(x => x.DrawLine(Color.Orange, 1, new(start, ascent), new(end, ascent)));
                image.Mutate(x => x.DrawLine(Color.Red, 1, new(start, baseline), new(end, baseline)));
                image.Mutate(x => x.DrawLine(Color.Blue, 1, new(start, descender), new(end, descender)));
                image.Mutate(x => x.DrawLine(Color.Green, 1, new(start, lineBottom), new(end, lineBottom)));
            }
        }

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            beforeAction: DrawLineMetrics);
    }
}
