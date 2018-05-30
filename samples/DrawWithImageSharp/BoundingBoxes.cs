using SixLabors.ImageSharp;
using SixLabors.Fonts;
using SixLabors.Shapes.Temp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;

namespace DrawWithImageSharp
{
    public static class BoundingBoxes
    {
        public static void Generate(string text, Font font)
        {
            using (Image<Rgba32> img = new Image<Rgba32>(1000, 1000))
            {
                img.Mutate(x=>x.Fill(Rgba32.White));

                SixLabors.Primitives.RectangleF box = TextMeasurer.MeasureBounds(text, new RendererOptions(font));
                (SixLabors.Shapes.IPathCollection paths, SixLabors.Shapes.IPathCollection boxes, SixLabors.Shapes.IPath textBox) = TextBuilder.GenerateGlyphsWithBox(text, new RendererOptions(font));

                Rgba32 f = Rgba32.Fuchsia;
                f.A = 128;

                img.Mutate(x => x.Fill(Rgba32.Black, paths)
                                .Draw(f, 1, boxes)
                                .Draw(Rgba32.Lime, 1, new SixLabors.Shapes.RectangularPolygon(box)));

                img.Save("Output/Boxed.png");
            }
        }
    }
}
