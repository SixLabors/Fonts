// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests;

public class SvgArcRadiusTests
{
    private const float Tolerance = 1e-3F;

    [Fact]
    public void CircleArcsKeepBothRadii()
    {
        (Arc[] arcs, float scale) = RenderSvgGlyph("<circle cx=\"600\" cy=\"-500\" r=\"400\"/>");

        Assert.Equal(2, arcs.Length);
        foreach (Arc arc in arcs)
        {
            Assert.Equal(400 * scale, arc.RadiusX, Tolerance);
            Assert.Equal(400 * scale, arc.RadiusY, Tolerance);
        }
    }

    [Theory]
    [InlineData("rotate(90)")]
    [InlineData("rotate(180)")]
    [InlineData("rotate(-37)")]
    [InlineData("scale(-1 1)")]
    public void CircleArcsKeepBothRadiiUnderRotationAndReflection(string transform)
    {
        (Arc[] arcs, float scale) = RenderSvgGlyph($"<circle cx=\"0\" cy=\"0\" r=\"400\" transform=\"{transform}\"/>");

        Assert.Equal(2, arcs.Length);
        foreach (Arc arc in arcs)
        {
            Assert.Equal(400 * scale, arc.RadiusX, Tolerance);
            Assert.Equal(400 * scale, arc.RadiusY, Tolerance);
        }
    }

    /// <summary>
    /// Checks that large finite ellipse radii stay finite when the renderer receives them.
    /// </summary>
    [Fact]
    public void EllipseArcsKeepLargeFiniteRadii()
    {
        (Arc[] arcs, float scale) = RenderSvgGlyph("<ellipse cx=\"0\" cy=\"0\" rx=\"200000000000\" ry=\"20\"/>");

        Assert.Equal(2, arcs.Length);
        foreach (Arc arc in arcs)
        {
            Assert.Equal(1F, arc.RadiusX / (2E11F * scale), 1E-6F);
            Assert.Equal(1F, arc.RadiusY / (20F * scale), 1E-6F);
        }
    }

    [Fact]
    public void ReflectionReversesSweep()
    {
        (Arc[] plain, _) = RenderSvgGlyph("<circle cx=\"600\" cy=\"-500\" r=\"400\"/>");
        (Arc[] mirrored, _) = RenderSvgGlyph("<circle cx=\"600\" cy=\"-500\" r=\"400\" transform=\"matrix(-1 0 0 1 1200 0)\"/>");

        Assert.Equal(plain.Length, mirrored.Length);
        for (int i = 0; i < plain.Length; i++)
        {
            Assert.NotEqual(plain[i].Sweep, mirrored[i].Sweep);
        }
    }

    [Fact]
    public void ReflectionReversesEllipseRotation()
    {
        // A 30 degree rotation followed by a horizontal mirror, written as one matrix.
        (Arc[] arcs, float scale) = RenderSvgGlyph(
            "<ellipse cx=\"0\" cy=\"0\" rx=\"400\" ry=\"200\" transform=\"matrix(-0.8660254 0.5 0.5 0.8660254 600 -500)\"/>");

        Assert.Equal(2, arcs.Length);
        foreach (Arc arc in arcs)
        {
            Assert.Equal(400 * scale, arc.RadiusX, Tolerance);
            Assert.Equal(200 * scale, arc.RadiusY, Tolerance);
            AssertAxisAngle(-30, arc.Rotation);
        }
    }

    [Fact]
    public void NonUniformScaleStretchesEllipseAxes()
    {
        (Arc[] arcs, float scale) = RenderSvgGlyph(
            "<ellipse cx=\"0\" cy=\"0\" rx=\"200\" ry=\"200\" transform=\"matrix(2 0 0 1 600 -500)\"/>");

        Assert.Equal(2, arcs.Length);
        foreach (Arc arc in arcs)
        {
            Assert.Equal(400 * scale, arc.RadiusX, Tolerance);
            Assert.Equal(200 * scale, arc.RadiusY, Tolerance);
            AssertAxisAngle(0, arc.Rotation);
        }
    }

    [Fact]
    public void ShearMapsCircleToEllipseOfEqualArea()
    {
        // A 45 degree x-skew maps the circle to an ellipse whose axes are the singular values of
        // [[1, 1], [0, 1]] times r: r·φ and r/φ, where φ is the golden ratio, with the major axis
        // at atan(1/φ) from x.
        (Arc[] arcs, float scale) = RenderSvgGlyph(
            "<circle cx=\"0\" cy=\"0\" r=\"200\" transform=\"matrix(1 0 1 1 600 -500)\"/>");

        float phi = (1 + MathF.Sqrt(5)) / 2;

        Assert.Equal(2, arcs.Length);
        foreach (Arc arc in arcs)
        {
            Assert.Equal(200 * phi * scale, arc.RadiusX, Tolerance);
            Assert.Equal(200 / phi * scale, arc.RadiusY, Tolerance);
            AssertAxisAngle(MathF.Atan(1 / phi) * 180 / MathF.PI, arc.Rotation);
        }
    }

    [Theory]
    [InlineData("matrix(-1 0 0 1 0 0)", -30F)]
    [InlineData("matrix(0 1 -1 0 0 0)", 120F)]
    public void PathArcRotationFollowsTransform(string transform, float expectedRotation)
    {
        // A path arc carries its own x-axis rotation, unlike the arcs emitted for <circle> and <ellipse>.
        (Arc[] arcs, float scale) = RenderSvgGlyph(
            $"<path d=\"M600 -500 A400 200 30 1 1 610 -500 Z\" transform=\"{transform}\"/>");

        Arc arc = Assert.Single(arcs);
        Assert.Equal(400 * scale, arc.RadiusX, Tolerance);
        Assert.Equal(200 * scale, arc.RadiusY, Tolerance);
        AssertAxisAngle(expectedRotation, arc.Rotation);
    }

    /// <summary>
    /// Asserts that two ellipse axis angles describe the same axis.
    /// </summary>
    /// <param name="expected">The expected angle, in degrees.</param>
    /// <param name="actual">The actual angle, in degrees.</param>
    private static void AssertAxisAngle(float expected, float actual)
    {
        // An ellipse axis is a line, so its angle is only defined modulo 180 degrees.
        float delta = (actual - expected) % 180F;
        if (delta > 90F)
        {
            delta -= 180F;
        }
        else if (delta < -90F)
        {
            delta += 180F;
        }

        Assert.Equal(0F, delta, Tolerance);
    }

    /// <summary>
    /// Renders the glyph for 'A' from a font whose SVG document for that glyph is <paramref name="body"/>.
    /// </summary>
    /// <param name="body">The SVG markup to place inside the glyph's group element.</param>
    /// <returns>The arcs streamed to the renderer, and the document-to-device scale.</returns>
    private static (Arc[] Arcs, float Scale) RenderSvgGlyph(string body)
    {
        Font font = SvgTestFonts.CreateFont(body);
        TextOptions options = new(font) { ColorFontSupport = ColorFontSupport.Svg };
        ArcRecordingRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "A", options);

        // The SVG document has no viewBox, so one document unit is one font unit.
        float scale = 100F * options.Dpi / 72F / font.FontMetrics.UnitsPerEm;
        return ([.. renderer.Arcs], scale);
    }

    /// <summary>
    /// An elliptical arc as streamed to <see cref="IGlyphRenderer.ArcTo"/>.
    /// </summary>
    private readonly struct Arc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Arc"/> struct.
        /// </summary>
        /// <param name="radiusX">The x-radius.</param>
        /// <param name="radiusY">The y-radius.</param>
        /// <param name="rotation">The rotation of the x-axis, in degrees.</param>
        /// <param name="sweep">The sweep flag.</param>
        public Arc(float radiusX, float radiusY, float rotation, bool sweep)
        {
            this.RadiusX = radiusX;
            this.RadiusY = radiusY;
            this.Rotation = rotation;
            this.Sweep = sweep;
        }

        /// <summary>
        /// Gets the x-radius.
        /// </summary>
        public float RadiusX { get; }

        /// <summary>
        /// Gets the y-radius.
        /// </summary>
        public float RadiusY { get; }

        /// <summary>
        /// Gets the rotation of the x-axis, in degrees.
        /// </summary>
        public float Rotation { get; }

        /// <summary>
        /// Gets a value indicating whether the sweep flag is set.
        /// </summary>
        public bool Sweep { get; }
    }

    /// <summary>
    /// A glyph renderer that records every arc it receives.
    /// </summary>
    private sealed class ArcRecordingRenderer : GlyphRenderer, IGlyphRenderer
    {
        /// <summary>
        /// Gets the recorded arcs, in the order they were received.
        /// </summary>
        public List<Arc> Arcs { get; } = [];

        /// <inheritdoc/>
        void IGlyphRenderer.ArcTo(float radiusX, float radiusY, float rotation, bool largeArc, bool sweep, Vector2 point)
        {
            this.Arcs.Add(new Arc(radiusX, radiusY, rotation, sweep));
            this.ArcTo(radiusX, radiusY, rotation, largeArc, sweep, point);
        }
    }
}
