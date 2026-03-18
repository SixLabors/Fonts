// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SixLabors.Fonts.Tests;

#if SUPPORTS_DRAWING
public class TextAlignmentTests
{
    private const int Padding = 20;
    private const int UnwrappedCellSize = 320;
    private const int WrappedCellSize = 520;
    private static readonly HorizontalAlignment[] HorizontalAlignments = Enum.GetValues<HorizontalAlignment>();
    private static readonly VerticalAlignment[] VerticalAlignments = Enum.GetValues<VerticalAlignment>();

    [Fact]
    public void TextAlignmentSample_RendersReferenceImage()
        => TextLayoutTestUtilities.TestImage(
            1000,
            1000,
            image => RenderAlignmentSample(image, TestFonts.GetFont(TestFonts.OpenSansFile, 50)));

    [Fact]
    public void TextAlignmentWrapped_RendersReferenceImage()
        => TextLayoutTestUtilities.TestImage(
            1600,
            1600,
            image => RenderAlignmentWrapped(image, TestFonts.GetFont(TestFonts.OpenSansFile, 50)));

    private static void RenderAlignmentSample(Image<Rgba32> image, Font font)
    {
        for (int row = 0; row < VerticalAlignments.Length; row++)
        {
            for (int column = 0; column < HorizontalAlignments.Length; column++)
            {
                VerticalAlignment vertical = VerticalAlignments[row];
                HorizontalAlignment horizontal = HorizontalAlignments[column];

                using Image<Rgba32> cell = RenderCell(
                    UnwrappedCellSize,
                    UnwrappedCellSize,
                    font,
                    vertical,
                    horizontal,
                    0,
                    $"{horizontal} x y z\n{vertical} x y z");

                DrawCell(image, cell, row, column, UnwrappedCellSize);
            }
        }
    }

    private static void RenderAlignmentWrapped(Image<Rgba32> image, Font font)
    {
        const int wrappingWidth = 400;

        for (int row = 0; row < VerticalAlignments.Length; row++)
        {
            for (int column = 0; column < HorizontalAlignments.Length; column++)
            {
                VerticalAlignment vertical = VerticalAlignments[row];
                HorizontalAlignment horizontal = HorizontalAlignments[column];

                using Image<Rgba32> cell = RenderCell(
                    WrappedCellSize,
                    WrappedCellSize,
                    font,
                    vertical,
                    horizontal,
                    wrappingWidth,
                    $"    {horizontal}     {vertical}         {horizontal}     {vertical}         {horizontal}     {vertical}     ");

                DrawCell(image, cell, row, column, WrappedCellSize);
            }
        }
    }

    private static Image<Rgba32> RenderCell(
        int width,
        int height,
        Font font,
        VerticalAlignment vertical,
        HorizontalAlignment horizontal,
        float wrappingWidth,
        string text)
    {
        Image<Rgba32> image = new(width, height);
        image.Mutate(x => x.Fill(Color.White));

        Draw(image, font, vertical, horizontal, wrappingWidth, text);
        image.Mutate(x => x.Draw(Color.LightGray, 1, new RectangularPolygon(0, 0, width - 1, height - 1)));

        return image;
    }

    private static void DrawCell(Image<Rgba32> image, Image<Rgba32> cell, int row, int column, int cellSize)
    {
        Point location = new(column * (cellSize + Padding), row * (cellSize + Padding));
        image.Mutate(x => x.DrawImage(cell, location, 1F));
    }

    private static void Draw(
        Image<Rgba32> image,
        Font font,
        VerticalAlignment vertical,
        HorizontalAlignment horizontal,
        float wrappingWidth,
        string text)
    {
        float x = horizontal switch
        {
            HorizontalAlignment.Left => 0,
            HorizontalAlignment.Center => image.Width / 2F,
            HorizontalAlignment.Right => image.Width,
            _ => 0,
        };

        float y = vertical switch
        {
            VerticalAlignment.Top => 0,
            VerticalAlignment.Center => image.Height / 2F,
            VerticalAlignment.Bottom => image.Height,
            _ => 0,
        };

        Vector2 location = new(x, y);

        BoundsRenderer boundsRenderer = new();

        TextOptions textOptions = new(font)
        {
            TabWidth = 4,
            WrappingLength = wrappingWidth,
            HorizontalAlignment = horizontal,
            VerticalAlignment = vertical,
            Origin = location,
        };

        IReadOnlyList<GlyphPathCollection> glyphPaths = TextBuilder.GenerateGlyphs(text, textOptions);
        TextRenderer.RenderTextTo(boundsRenderer, text, textOptions);

        image.Mutate(x => x.Fill(Color.Black, glyphPaths));
        Color boundsColor = Color.Fuchsia.WithAlpha(.5F);
        image.Mutate(x => x.Draw(boundsColor, 1, boundsRenderer.Boxes));
        image.Mutate(x => x.Draw(Color.Lime, 1, boundsRenderer.TextBox));
    }

    private sealed class BoundsRenderer : IGlyphRenderer
    {
        private readonly List<FontRectangle> glyphBounds = [];

        public IPathCollection Boxes => new PathCollection(this.glyphBounds.Select(x => new RectangularPolygon(x.X, x.Y, x.Width, x.Height)));

        public IPath TextBox { get; private set; }

        public void BeginText(in FontRectangle bounds)
            => this.TextBox = new RectangularPolygon(bounds.X, bounds.Y, bounds.Width, bounds.Height);

        public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
        {
            this.glyphBounds.Add(bounds);
            return true;
        }

        public void BeginFigure()
        {
        }

        public void MoveTo(Vector2 point)
        {
        }

        public void ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
        {
        }

        public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        {
        }

        public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        {
        }

        public void LineTo(Vector2 point)
        {
        }

        public void EndFigure()
        {
        }

        public void EndGlyph()
        {
        }

        public void EndText()
        {
        }

        public TextDecorations EnabledDecorations() => TextDecorations.None;

        public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        {
        }

        public void BeginLayer(Paint paint, FillRule fillRule, ClipQuad? clipBounds)
        {
        }

        public void EndLayer()
        {
        }
    }
}
#endif
