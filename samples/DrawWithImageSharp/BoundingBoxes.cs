// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts;
using SixLabors.Fonts.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using IOPath = System.IO.Path;

namespace DrawWithImageSharp;

public static class BoundingBoxes
{
    public static void Generate(string text, TextOptions options)
    {
        FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
        FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
        using Image<Rgba32> image = new((int)Math.Ceiling(options.Origin.X + (Math.Max(advance.Width, bounds.Width) + 1)), (int)Math.Ceiling(options.Origin.Y + (Math.Max(advance.Height, bounds.Height) + 1)));
        image.Mutate(x => x.Fill(Color.White));

        Vector2 origin = options.Origin;

        FontRectangle size = TextMeasurer.MeasureSize(text, options);

        (IPathCollection paths, IPathCollection boxes) = GenerateGlyphsWithBox(text, options);
        image.Mutate(
            x => x.Fill(Color.Black, paths)
            .Draw(Color.Yellow, 1, boxes)
            .Draw(Color.Purple, 1, new RectangularPolygon(bounds.X, bounds.Y, bounds.Width, bounds.Height))
            .Draw(Color.Green, 1, new RectangularPolygon(size.X + bounds.X, size.Y + bounds.Y, size.Width, size.Height))
            .Draw(Color.Red, 1, new RectangularPolygon(advance.X + origin.X, advance.Y + origin.Y, advance.Width, advance.Height)));

        string path = IOPath.GetInvalidFileNameChars().Aggregate(text, (x, c) => x.Replace($"{c}", "-"));
        string fullPath = IOPath.GetFullPath(IOPath.Combine($"Output/Boxed/{options.Font.Name}", IOPath.Combine(path)));
        Directory.CreateDirectory(IOPath.GetDirectoryName(fullPath));

        image.Save($"{fullPath}.png");
    }

    /// <summary>
    /// Generates the shapes corresponding the glyphs described by the font and settings.
    /// </summary>
    /// <param name="text">The text to generate glyphs for</param>
    /// <param name="options">The style and settings to use while rendering the glyphs</param>
    /// <returns>The paths, boxes, and text box.</returns>
    private static (IPathCollection Paths, IPathCollection Boxes) GenerateGlyphsWithBox(string text, TextOptions options)
    {
        CustomGlyphBuilder glyphBuilder = new();

        TextRenderer renderer = new(glyphBuilder);

        renderer.RenderText(text, options);

        return (glyphBuilder.Paths, glyphBuilder.Boxes);
    }
}
