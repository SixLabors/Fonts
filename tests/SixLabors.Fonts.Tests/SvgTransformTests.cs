// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests;

public class SvgTransformTests
{
    private const string Triangle = "points=\"0,0 10,0 10,10\"";

    [Theory]

    // A transform list applies right to left: scale by 2, then translate.
    [InlineData("<polygon points=\"0,0 10,0 10,10\" transform=\"translate(100 0) scale(2)\"/>", "100,0 120,0 120,20")]

    // Rotation about a centre point: (x, y) -> (100 - y, x).
    [InlineData("<polygon points=\"0,0 10,0 10,10\" transform=\"rotate(90 50 50)\"/>", "100,0 100,10 90,10")]

    // A rotation about a centre combines with the rest of the list in order.
    [InlineData("<polygon points=\"0,0 10,0 10,10\" transform=\"translate(0 20) rotate(90 50 50)\"/>", "100,20 100,30 90,30")]

    // Nested elements: the child's transform applies first, then the parent's.
    [InlineData("<g transform=\"translate(100 0)\"><polygon points=\"0,0 10,0 10,10\" transform=\"scale(2)\"/></g>", "100,0 120,0 120,20")]
    public void TransformsComposeInDocumentOrder(string body, string expectedPoints)
    {
        Vector2[] actual = RenderPoints(body);
        Vector2[] expected = RenderPoints($"<polygon points=\"{expectedPoints}\"/>");

        AssertPoints(expected, actual);
    }

    [Fact]
    public void UseTranslatesBeforeItsTransform()
    {
        // <use x y transform> places its content with translate(x, y) inside the element's own transform.
        Vector2[] actual = RenderPoints(
            "<use xlink:href=\"#shape\" x=\"5\" y=\"0\" transform=\"scale(2)\"/>",
            $"<defs><polygon id=\"shape\" {Triangle}/></defs>");

        Vector2[] expected = RenderPoints("<polygon points=\"10,0 30,0 30,20\"/>");

        AssertPoints(expected, actual);
    }

    [Fact]
    public void UseAppliesParentTransformLast()
    {
        Vector2[] actual = RenderPoints(
            "<g transform=\"scale(2)\"><use xlink:href=\"#shape\" x=\"5\" y=\"0\" transform=\"translate(0 10)\"/></g>",
            $"<defs><polygon id=\"shape\" {Triangle}/></defs>");

        Vector2[] expected = RenderPoints("<polygon points=\"10,20 30,20 30,40\"/>");

        AssertPoints(expected, actual);
    }

    /// <summary>
    /// Asserts that two point sequences match within rendering tolerance.
    /// </summary>
    /// <param name="expected">The expected points.</param>
    /// <param name="actual">The actual points.</param>
    private static void AssertPoints(Vector2[] expected, Vector2[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].X, actual[i].X, 1e-3F);
            Assert.Equal(expected[i].Y, actual[i].Y, 1e-3F);
        }
    }

    /// <summary>
    /// Renders an SVG glyph and returns the device-space points it moves and draws lines to.
    /// </summary>
    /// <param name="body">The SVG markup to place inside the glyph's group element.</param>
    /// <param name="defs">Optional SVG markup to place outside the glyph's group element.</param>
    /// <returns>The recorded points, in the order they were received.</returns>
    private static Vector2[] RenderPoints(string body, string defs = "")
    {
        Font font = SvgTestFonts.CreateFont(body, defs);
        TextOptions options = new(font) { ColorFontSupport = ColorFontSupport.Svg };
        PointRecordingRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "A", options);

        Assert.NotEmpty(renderer.Points);
        return [.. renderer.Points];
    }

    /// <summary>
    /// A glyph renderer that records every point it moves or draws a line to.
    /// </summary>
    private sealed class PointRecordingRenderer : GlyphRenderer, IGlyphRenderer
    {
        /// <summary>
        /// Gets the recorded points, in the order they were received.
        /// </summary>
        public List<Vector2> Points { get; } = [];

        /// <inheritdoc/>
        void IGlyphRenderer.MoveTo(Vector2 point)
        {
            this.Points.Add(point);
            this.MoveTo(point);
        }

        /// <inheritdoc/>
        void IGlyphRenderer.LineTo(Vector2 point)
        {
            this.Points.Add(point);
            this.LineTo(point);
        }
    }
}
