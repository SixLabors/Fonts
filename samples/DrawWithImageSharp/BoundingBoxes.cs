// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TextBuilder = SixLabors.Shapes.Temp.TextBuilder;

namespace DrawWithImageSharp
{
    public static class BoundingBoxes
    {
        public static void Generate(string text, Font font)
        {
            using (var img = new Image<Rgba32>(1000, 1000))
            {
                img.Mutate(x => x.Fill(Color.White));

                FontRectangle box = TextMeasurer.MeasureBounds(text, new RendererOptions(font));
                (IPathCollection paths, IPathCollection boxes, IPath textBox) = TextBuilder.GenerateGlyphsWithBox(text, new RendererOptions(font));

                Rgba32 f = Color.Fuchsia;
                f.A = 128;

                img.Mutate(x => x.Fill(Color.Black, paths)
                                .Draw(f, 1, boxes)
                                .Draw(Color.Lime, 1, new RectangularPolygon(box.Location, box.Size)));

                img.Save("Output/Boxed.png");
            }
        }
    }
}
