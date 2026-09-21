// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

#if SUPPORTS_DRAWING
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Tests.TestUtilities;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing.Text;
using SixLabors.ImageSharp.Processing;
#endif

namespace SixLabors.Fonts.Tests;

internal static class TextLayoutTestUtilities
{
    public static void TestImage(
        int imageWidth,
        int imageHeight,
        Action<Image<Rgba32>> renderAction,
        float percentageTolerance = 0.05F,
        [CallerMemberName] string test = "",
        params object[] properties)
    {
#if SUPPORTS_DRAWING
        using Image<Rgba32> image = new(Configuration.Default, imageWidth, imageHeight, Color.White.ToPixel<Rgba32>());
        renderAction(image);
        image.DebugSave("png", test, properties: properties);
        image.CompareToReference(percentageTolerance: percentageTolerance, test: test, properties: properties);
#endif
    }

    public static void TestLayout(
        string text,
        TextOptions options,
        float percentageTolerance = 0.05F,
        bool includeGeometry = false,
        bool customDecorations = false,
        [CallerMemberName] string test = "",
        Action<Image<Rgba32>> beforeAction = null,
        Action<Image<Rgba32>> afterAction = null,
        params object[] properties)
    {
#if SUPPORTS_DRAWING
        // The advance is intentionally origin-less here: the sizing formulas below
        // add the origin exactly once, and every pinned reference image encodes
        // these dimensions. MeasureRenderableBounds anchors the advance at the
        // origin before unioning, so switching to it would resize every canvas
        // with a non-zero origin and orphan the references.
        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
        FontRectangle renderBounds = FontRectangle.Union(advance, bounds);

        // A bitmap has no negative pixel coordinates, so ink measured left of or
        // above the origin (rotated mark stacks, sideways descenders, italic
        // overhang) must translate the draw origin: without the shift the overhang
        // rasterizes off the image edge while the space the sizing already reserves
        // for it sits blank on the opposite side. The shift is dimension-neutral,
        // so only previously clipped outputs change. The origin is restored
        // afterward because the options instance belongs to the caller.
        Vector2 originalOrigin = options.Origin;
        options.Origin = originalOrigin + new Vector2(-MathF.Min(0, renderBounds.Left), -MathF.Min(0, renderBounds.Top));

        try
        {
            int width = Math.Max(1, (int)Math.Ceiling(originalOrigin.X + renderBounds.Right - Math.Min(0, renderBounds.Left)));
            int height = Math.Max(1, (int)Math.Ceiling(originalOrigin.Y + renderBounds.Bottom - Math.Min(0, renderBounds.Top)));

            bool isVertical = !options.LayoutMode.IsHorizontal();
            int wrappingLength = isVertical
                ? (int)(Math.Ceiling(options.WrappingLength) + Math.Ceiling(options.Origin.Y))
                : (int)(Math.Ceiling(options.WrappingLength) + Math.Ceiling(options.Origin.X));

            int imageWidth = isVertical ? width : Math.Max(width, wrappingLength + 1);
            int imageHeight = isVertical ? Math.Max(height, wrappingLength + 1) : height;

            List<object> extended = properties?.ToList() ?? [];
            if (options.WrappingLength > 0)
            {
                extended.Insert(0, options.WrappingLength);
            }

            // First render the text using the rich text renderer.
            using Image<Rgba32> img = new(Configuration.Default, imageWidth, imageHeight, Color.White.ToPixel<Rgba32>());

            beforeAction?.Invoke(img);

            img.Mutate(ctx => ctx.Paint(canvas =>
            {
                canvas.DrawText(
                    FromTextOptions(options, customDecorations),
                    text,
                    Brushes.Solid(Color.Black),
                    pen: null);

                if (options.WrappingLength > 0)
                {
                    if (!options.LayoutMode.IsHorizontal())
                    {
                        canvas.DrawLine(Pens.Solid(Color.Red, 1), new PointF(0, wrappingLength), new PointF(width, wrappingLength));
                    }
                    else
                    {
                        canvas.DrawLine(Pens.Solid(Color.Red, 1), new PointF(wrappingLength, 0), new PointF(wrappingLength, height));
                    }
                }
            }));

            afterAction?.Invoke(img);

            img.DebugSave("png", test, properties: [.. extended]);

            // Every visual test must be pinned independently. Missing references and
            // changed output are failures rather than an opt-out path hidden at a call site.
            img.CompareToReference(percentageTolerance: percentageTolerance, test: test, properties: [.. extended]);

            if (!includeGeometry)
            {
                return;
            }

            // Now render the text using geometry-only renderer.
            extended.Insert(0, "G");
            using Image<Rgba32> img2 = new(Configuration.Default, imageWidth, imageHeight, Color.White.ToPixel<Rgba32>());

            IReadOnlyList<GlyphPathCollection> glyphs = TextBuilder.GenerateGlyphs(text, options);

            img2.Mutate(ctx => ctx.Paint(canvas =>
            {
                canvas.DrawGlyphs(Brushes.Solid(Color.Black), Pens.Solid(Color.Black, 1F), glyphs);

                if (options.WrappingLength > 0)
                {
                    if (!options.LayoutMode.IsHorizontal())
                    {
                        canvas.DrawLine(Pens.Solid(Color.Red, 1), new PointF(0, wrappingLength), new PointF(width, wrappingLength));
                    }
                    else
                    {
                        canvas.DrawLine(Pens.Solid(Color.Red, 1), new PointF(wrappingLength, 0), new PointF(wrappingLength, height));
                    }
                }
            }));

            img2.DebugSave("png", test, properties: [.. extended]);
            img2.CompareToReference(percentageTolerance: percentageTolerance, test: test, properties: [.. extended]);
        }
        finally
        {
            options.Origin = originalOrigin;
        }
#endif
    }

#if SUPPORTS_DRAWING
    private static RichTextOptions FromTextOptions(TextOptions options, bool customDecorations)
    {
        RichTextOptions result = new(options.Font)
        {
            FontWeight = options.FontWeight,
            FallbackFontFamilies = new List<FontFamily>(options.FallbackFontFamilies),
            FontFallbackResolver = options.FontFallbackResolver,
            TabWidth = options.TabWidth,
            HintingMode = options.HintingMode,
            Dpi = options.Dpi,
            LineSpacing = options.LineSpacing,
            Origin = options.Origin,
            WrappingLength = options.WrappingLength,
            VisibleBounds = options.VisibleBounds,
            TextBaseline = options.TextBaseline,
            BaselineOffset = options.BaselineOffset,
            MaxLines = options.MaxLines,
            WordBreaking = options.WordBreaking,
            TextHyphenation = options.TextHyphenation,
            CustomHyphen = options.CustomHyphen,
            TextEllipsis = options.TextEllipsis,
            CustomEllipsis = options.CustomEllipsis,
            TextDirection = options.TextDirection,
            TextBidiMode = options.TextBidiMode,
            Script = options.Script,
            TextInteractionMode = options.TextInteractionMode,
            TextAlignment = options.TextAlignment,
            TextJustification = options.TextJustification,
            HorizontalAlignment = options.HorizontalAlignment,
            VerticalAlignment = options.VerticalAlignment,
            LayoutMode = options.LayoutMode,
            KerningMode = options.KerningMode,
            DecorationPositioningMode = options.DecorationPositioningMode,
            Tracking = options.Tracking,
            ColorFontSupport = options.ColorFontSupport,
            FontPalette = options.FontPalette,
            FeatureTags = new List<Tag>(options.FeatureTags),
            Culture = options.Culture,
        };

        if (options.TextRuns.Count > 0)
        {
            List<RichTextRun> runs = new(options.TextRuns.Count);
            foreach (TextRun run in options.TextRuns)
            {
                RichTextRun richRun = new()
                {
                    Font = run.Font,
                    FontWeight = run.FontWeight,
                    Start = run.Start,
                    End = run.End,
                    Script = run.Script,
                    Culture = run.Culture,
                    FeatureTags = run.FeatureTags is null ? null : new List<Tag>(run.FeatureTags),
                    TextAttributes = run.TextAttributes,
                    TextDecorations = run.TextDecorations,
                    ColorFontSupport = run.ColorFontSupport,
                    FontPalette = run.FontPalette,
                };

                if (customDecorations && run.TextDecorations != TextDecorations.None)
                {
                    richRun.StrikeoutPen = new SolidPen(Color.Green, 11.3334F);
                    richRun.UnderlinePen = new SolidPen(Color.Blue, 15.5555F);
                    richRun.OverlinePen = new SolidPen(Color.Purple, 13.7777F);
                }

                runs.Add(richRun);
            }

            result.TextRuns = runs;
        }

        return result;
    }
#endif
}
