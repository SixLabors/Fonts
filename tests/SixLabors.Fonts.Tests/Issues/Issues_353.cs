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

        void DrawLines(Image<Rgba32> image)
        {
            // Draw four separate lines for acender(orange), baseline (red), descender (blue),
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

                image.Mutate(x => x.DrawLine(Color.Orange, 1, new(m.Start, ascent), new(m.Extent, ascent)));
                image.Mutate(x => x.DrawLine(Color.Red, 1, new(m.Start, baseline), new(m.Extent, baseline)));
                image.Mutate(x => x.DrawLine(Color.Blue, 1, new(m.Start, descender), new(m.Extent, descender)));
                image.Mutate(x => x.DrawLine(Color.Green, 1, new(m.Start, lineBottom), new(m.Extent, lineBottom)));

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
