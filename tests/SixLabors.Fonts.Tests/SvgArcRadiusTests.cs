// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Unicode;

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
        byte[] source = File.ReadAllBytes(TestFonts.SimpleFontFile);
        Font plain = new FontCollection().Add(new MemoryStream(source)).CreateFont(100);
        Assert.True(plain.TryGetGlyphId(new CodePoint('A'), out ushort glyphId));

        string svg = string.Create(
            CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\"><g id=\"glyph{glyphId}\">{body}</g></svg>");

        Font font = new FontCollection().Add(AddSvgTable(source, glyphId, svg)).CreateFont(100);

        TextOptions options = new(font) { ColorFontSupport = ColorFontSupport.Svg };
        ArcRecordingRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "A", options);

        // The SVG document has no viewBox, so one document unit is one font unit.
        float scale = 100F * options.Dpi / 72F / font.FontMetrics.UnitsPerEm;
        return ([.. renderer.Arcs], scale);
    }

    /// <summary>
    /// Returns a copy of a TrueType font with an 'SVG ' table holding one document for one glyph.
    /// </summary>
    /// <param name="font">The source font, which must not already contain an 'SVG ' table.</param>
    /// <param name="glyphId">The glyph the document applies to.</param>
    /// <param name="svgDocument">The SVG document.</param>
    /// <returns>A stream over the new font.</returns>
    private static MemoryStream AddSvgTable(byte[] font, ushort glyphId, string svgDocument)
    {
        // Lift the existing tables out of the source font's table directory.
        int numTables = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
        List<(string Tag, byte[] Data)> tables = [];
        for (int i = 0; i < numTables; i++)
        {
            ReadOnlySpan<byte> record = font.AsSpan(12 + (16 * i), 16);
            string tag = Encoding.ASCII.GetString(record[..4]);
            int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(record[12..]);
            tables.Add((tag, font.AsSpan(offset, length).ToArray()));
        }

        // SVG table: the header, a document index with a single entry, then the document.
        byte[] document = Encoding.UTF8.GetBytes(svgDocument);
        BigEndianBinaryWriter svg = new();
        svg.WriteUInt16(0); // version
        svg.WriteUInt32(10); // svgDocumentListOffset
        svg.WriteUInt32(0); // reserved
        svg.WriteUInt16(1); // numEntries
        svg.WriteUInt16(glyphId); // startGlyphID
        svg.WriteUInt16(glyphId); // endGlyphID
        svg.WriteUInt32(14); // svgDocOffset, from the start of the document index
        svg.WriteUInt32((uint)document.Length); // svgDocLength
        svg.Write(document);

        tables.Add(("SVG ", svg.GetStream().ToArray()));
        tables.Sort((x, y) => string.CompareOrdinal(x.Tag, y.Tag));

        // The header lays the tables out back to back in directory order.
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader([.. tables.Select(t => new TableHeader(t.Tag, 0, 0, (uint)t.Data.Length))]);
        foreach ((_, byte[] data) in tables)
        {
            writer.Write(data);
        }

        return writer.GetStream();
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
