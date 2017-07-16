using ImageSharp;
using SixLabors.Fonts;
using SixLabors.Shapes.Temp;

namespace DrawWithImageSharp
{
    public static class BoundingBoxes
    {
        public static void Generate(string text, Font font)
        {
            using (var img = new Image<Rgba32>(1000, 1000))
            {
                img.Fill(Rgba32.White);

                var box = TextMeasurer.MeasureBounds(text, new RendererOptions(font));
                var data = TextBuilder.GenerateGlyphsWithBox(text, new RendererOptions(font));

                var f = Rgba32.Fuchsia;
                f.A = 128;
                img.Fill(Rgba32.Black, data.paths);
                img.Draw(f, 1, data.boxes);
                img.Draw(Rgba32.Lime, 1, new SixLabors.Shapes.RectangularePolygon(box));

                img.Save("Output/Boxed.png");
            }
        }
    }
}
