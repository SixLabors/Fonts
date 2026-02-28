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
        FontCollection fontCollection = new();
        string name = fontCollection.Add(TestFonts.EbGaramond).Name;
        FontFamily family = fontCollection.Get(name);
        Font font = family.CreateFont(30, FontStyle.Regular);

        string text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec a diam lectus. Sed sit amet ipsum mauris.";
        TextOptions options = new(font)
        {
            WrappingLength = 400,
            LineSpacing = 1.6f
        };

        LineMetrics[] l = TextMeasurer.GetLineMetrics(text, options);

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

            // Horizontal metrics should describe a valid span.
            Assert.True(m.Extent > m.Start, "Extent should be greater than Start.");
        }

        void DrawLines(Image<Rgba32> image)
        {
            // Draw four separate lines for ascender(orange), baseline (red), descender (blue),
            // and line bottom (green).
            //
            // `offset` represents the Y coordinate of the top of the current line box.
            // It is advanced by `m.LineHeight` after each iteration.
            float offset = 0;
            for (int i = 0; i < l.Length; i++)
            {
                LineMetrics m = l[i];

                float ascent = offset + m.Ascender;
                float baseline = offset + m.Baseline;
                float descender = offset + m.Descender;
                float lineBottom = offset + m.LineHeight;

                image.Mutate(x => x.DrawLine(Color.Orange, 1, new(m.Start, ascent), new(m.Start + m.Extent, ascent)));
                image.Mutate(x => x.DrawLine(Color.Red, 1, new(m.Start, baseline), new(m.Start + m.Extent, baseline)));
                image.Mutate(x => x.DrawLine(Color.Blue, 1, new(m.Start, descender), new(m.Start + m.Extent, descender)));
                image.Mutate(x => x.DrawLine(Color.Green, 1, new(m.Start, lineBottom), new(m.Start + m.Extent, lineBottom)));

                // Advance to the next line's top-of-line-box.
                offset += m.LineHeight;
            }
        }

        TextLayoutTestUtilities.TestLayout(
            text,
            options,
            includeGeometry: false,
            beforeAction: DrawLines);
    }
}
