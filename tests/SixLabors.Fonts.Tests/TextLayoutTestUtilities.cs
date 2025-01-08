// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

#if SUPPORTS_DRAWING
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tests.TestUtilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
#endif

namespace SixLabors.Fonts.Tests;

internal static class TextLayoutTestUtilities
{
    public static void TestLayout(
        string text,
        TextOptions options,
        float percentageTolerance = 0.05F,
        [CallerMemberName] string test = "",
        params object[] properties)
    {
#if SUPPORTS_DRAWING
        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        int width = (int)(Math.Ceiling(advance.Width) + Math.Ceiling(options.Origin.X));
        int height = (int)(Math.Ceiling(advance.Height) + Math.Ceiling(options.Origin.Y));

        bool isVertical = !options.LayoutMode.IsHorizontal();
        int wrappingLength = isVertical
            ? (int)(Math.Ceiling(options.WrappingLength) + Math.Ceiling(options.Origin.Y))
            : (int)(Math.Ceiling(options.WrappingLength) + Math.Ceiling(options.Origin.X));

        int imageWidth = isVertical ? width : Math.Max(width, wrappingLength + 1);
        int imageHeight = isVertical ? Math.Max(height, wrappingLength + 1) : height;

        using Image<Rgba32> img = new(imageWidth, imageHeight, Color.White);

        img.Mutate(ctx => ctx.DrawText(FromTextOptions(options), text, Color.Black));

        if (wrappingLength > 0)
        {
            if (!options.LayoutMode.IsHorizontal())
            {
                img.Mutate(x => x.DrawLine(Color.Red, 1, new(0, wrappingLength), new(width, wrappingLength)));
            }
            else
            {
                img.Mutate(x => x.DrawLine(Color.Red, 1, new(wrappingLength, 0), new(wrappingLength, height)));
            }

            if (properties.Any())
            {
                List<object> extended = properties.ToList();
                extended.Insert(0, options.WrappingLength);
                img.CompareToReference(percentageTolerance: percentageTolerance, test: test, properties: extended.ToArray());
            }
            else
            {
                img.CompareToReference(percentageTolerance: percentageTolerance, test: test, properties: new { options.WrappingLength });
            }
        }
        else
        {
            img.CompareToReference(percentageTolerance: percentageTolerance, test: test, properties: properties);
        }

#endif
    }

#if SUPPORTS_DRAWING
    private static RichTextOptions FromTextOptions(TextOptions options)
    {
        RichTextOptions result = new(options.Font)
        {
            FallbackFontFamilies = new List<FontFamily>(options.FallbackFontFamilies),
            TabWidth = options.TabWidth,
            HintingMode = options.HintingMode,
            Dpi = options.Dpi,
            LineSpacing = options.LineSpacing,
            Origin = options.Origin,
            WrappingLength = options.WrappingLength,
            WordBreaking = options.WordBreaking,
            TextDirection = options.TextDirection,
            TextAlignment = options.TextAlignment,
            TextJustification = options.TextJustification,
            HorizontalAlignment = options.HorizontalAlignment,
            VerticalAlignment = options.VerticalAlignment,
            LayoutMode = options.LayoutMode,
            KerningMode = options.KerningMode,
            ColorFontSupport = options.ColorFontSupport,
            FeatureTags = new List<Tag>(options.FeatureTags),
        };

        if (options.TextRuns.Count > 0)
        {
            List<RichTextRun> runs = new(options.TextRuns.Count);
            foreach (TextRun run in options.TextRuns)
            {
                runs.Add(new RichTextRun()
                {
                    Font = run.Font,
                    Start = run.Start,
                    End = run.End,
                    TextAttributes = run.TextAttributes,
                    TextDecorations = run.TextDecorations
                });
            }
        }

        return result;
    }
#endif
}
