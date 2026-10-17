// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Tests that a <c>transform</c> on the root <c>&lt;svg&gt;</c> element of an OT-SVG glyph document applies exactly
/// once, in the parent space outside the <c>viewBox</c> mapping.
/// </summary>
public class SvgRootTransformTests
{
    /// <summary>
    /// The opening tag attributes shared by every document in these tests.
    /// </summary>
    private const string Namespaces = "xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"";

    /// <summary>
    /// A root translation moves a <c>glyph{gid}</c> group by the translation once.
    /// </summary>
    [Fact]
    public void RootTransformAppliesOnceToGlyphGroup()
    {
        // translate(100 -200) moves the rect's (0, -500) corner to (100, -700).
        Font transformed = SvgTestFonts.CreateFontFromDocument(gid => Format(
            $"<svg {Namespaces} transform=\"translate(100 -200)\"><g id=\"glyph{gid}\"><rect x=\"0\" y=\"-500\" width=\"300\" height=\"400\"/></g></svg>"));

        Font expected = SvgTestFonts.CreateFontFromDocument(gid => Format(
            $"<svg {Namespaces}><g id=\"glyph{gid}\"><rect x=\"100\" y=\"-700\" width=\"300\" height=\"400\"/></g></svg>"));

        AssertSamePoints(expected, transformed);
    }

    /// <summary>
    /// A root translation moves the content of a document without a <c>glyph{gid}</c> element, which renders
    /// from the root element itself, by the translation once.
    /// </summary>
    [Fact]
    public void RootTransformAppliesOnceWithoutGlyphId()
    {
        // translate(100 -200) moves the rect's (0, -500) corner to (100, -700).
        Font transformed = SvgTestFonts.CreateFontFromDocument(_ =>
            $"<svg {Namespaces} transform=\"translate(100 -200)\"><rect x=\"0\" y=\"-500\" width=\"300\" height=\"400\"/></svg>");

        Font expected = SvgTestFonts.CreateFontFromDocument(_ =>
            $"<svg {Namespaces}><rect x=\"100\" y=\"-700\" width=\"300\" height=\"400\"/></svg>");

        AssertSamePoints(expected, transformed);
    }

    /// <summary>
    /// The root transform applies once in font units, after the <c>viewBox</c> has mapped user space onto the em.
    /// </summary>
    [Fact]
    public void RootTransformAppliesOnceOutsideViewBox()
    {
        ushort upem = new FontCollection().Add(TestFonts.SimpleFontFile).CreateFont(100).FontMetrics.UnitsPerEm;
        float side = upem * 2F;

        // The viewBox spans two em in each direction, so user space is scaled by 1/2 into font units:
        // the rect (0, -1000) 600x800 becomes (0, -500) 300x400. matrix(2 0 0 2 100 -200) then maps
        // (x, y) to (2x + 100, 2y - 200) in font units, giving (100, -1200) 600x800. Applying the
        // matrix inside the viewBox instead would place the rect at (50, -1100) 600x800.
        Font transformed = SvgTestFonts.CreateFontFromDocument(gid => Format(
            $"<svg {Namespaces} viewBox=\"0 0 {side} {side}\" transform=\"matrix(2 0 0 2 100 -200)\"><g id=\"glyph{gid}\"><rect x=\"0\" y=\"-1000\" width=\"600\" height=\"800\"/></g></svg>"));

        Font expected = SvgTestFonts.CreateFontFromDocument(gid => Format(
            $"<svg {Namespaces}><g id=\"glyph{gid}\"><rect x=\"100\" y=\"-1200\" width=\"600\" height=\"800\"/></g></svg>"));

        AssertSamePoints(expected, transformed);
    }

    /// <summary>
    /// A rotated root transform yields the same tight ink bounds as the equivalent pre-rotated geometry,
    /// not the looser box around the rotated bounds of the unrotated geometry.
    /// </summary>
    [Fact]
    public void RotatedRootTransformKeepsTightBounds()
    {
        // A thin diagonal strip. rotate(45) maps (x, y) to (c(x - y), c(x + y)) with c = sqrt(2) / 2, so
        // (0, 0) -> (0, 0), (500, -500) -> (707.107, 0), (490, -510) -> (707.107, -14.142) and
        // (-10, -10) -> (0, -14.142): an axis-aligned 707.107 x 14.142 rectangle. Rotating the strip's
        // own 510 x 510 box instead would give a box roughly 721 units on each side.
        Font transformed = SvgTestFonts.CreateFontFromDocument(gid => Format(
            $"<svg {Namespaces} transform=\"rotate(45)\"><g id=\"glyph{gid}\"><polygon points=\"0 0 500 -500 490 -510 -10 -10\"/></g></svg>"));

        Font expected = SvgTestFonts.CreateFontFromDocument(gid => Format(
            $"<svg {Namespaces}><g id=\"glyph{gid}\"><rect x=\"0\" y=\"-14.142136\" width=\"707.10678\" height=\"14.142136\"/></g></svg>"));

        ApproximateFloatComparer comparer = new(0.01F);

        TextOptions expectedOptions = new(expected) { ColorFontSupport = ColorFontSupport.Svg };
        TextOptions transformedOptions = new(transformed) { ColorFontSupport = ColorFontSupport.Svg };
        Assert.Equal(TextMeasurer.MeasureBounds("A", expectedOptions), TextMeasurer.MeasureBounds("A", transformedOptions), comparer);

        GlyphRenderer expectedRenderer = new();
        GlyphRenderer transformedRenderer = new();
        TextRenderer.RenderTo(expectedRenderer, "A", expectedOptions);
        TextRenderer.RenderTo(transformedRenderer, "A", transformedOptions);
        Assert.Equal(Assert.Single(expectedRenderer.GlyphRects), Assert.Single(transformedRenderer.GlyphRects), comparer);
    }

    /// <summary>
    /// Formats an interpolated document string with the invariant culture so numbers use '.' separators.
    /// </summary>
    /// <param name="document">The interpolated document.</param>
    /// <returns>The formatted document.</returns>
    private static string Format(FormattableString document)
        => document.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Renders 'A' from both fonts and asserts that the SVG glyphs emit the same device-space points.
    /// </summary>
    /// <param name="expected">The font with the hand-computed, untransformed document.</param>
    /// <param name="actual">The font with the transformed document.</param>
    private static void AssertSamePoints(Font expected, Font actual)
    {
        List<Vector2> expectedPoints = RenderPoints(expected);
        List<Vector2> actualPoints = RenderPoints(actual);

        Assert.NotEmpty(expectedPoints);
        Assert.Equal(expectedPoints.Count, actualPoints.Count);

        ApproximateFloatComparer comparer = new(0.01F);
        for (int i = 0; i < expectedPoints.Count; i++)
        {
            Assert.Equal(expectedPoints[i], actualPoints[i], comparer);
        }
    }

    /// <summary>
    /// Renders 'A' through the SVG glyph pipeline and returns the device-space points it emits.
    /// </summary>
    /// <param name="font">The font to render.</param>
    /// <returns>The emitted points, in order.</returns>
    private static List<Vector2> RenderPoints(Font font)
    {
        GlyphRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "A", new TextOptions(font) { ColorFontSupport = ColorFontSupport.Svg });

        Assert.Contains(renderer.GlyphKeys, k => k.GlyphType == GlyphType.Painted);
        return renderer.ControlPoints;
    }
}
