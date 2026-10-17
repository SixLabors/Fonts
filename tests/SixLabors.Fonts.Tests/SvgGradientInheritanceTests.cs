// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Tests that SVG gradients referencing a template gradient through <c>href</c> inherit only the attributes they
/// do not specify themselves.
/// </summary>
/// <remarks>
/// Each test renders a document whose gradient <c>g</c> uses an <c>href</c> chain, and an equivalent document whose
/// gradient <c>g</c> spells out the resolved attributes directly, and asserts that both produce the same paint.
/// Comparing against an equivalent document keeps the tests independent of how the paint is mapped to device space.
/// </remarks>
public class SvgGradientInheritanceTests
{
    /// <summary>
    /// Two gradient stops shared by the documents in these tests.
    /// </summary>
    private const string Stops = "<stop offset=\"0\" stop-color=\"#FF0000\"/><stop offset=\"0.4\" stop-color=\"#00FF00\"/><stop offset=\"1\" stop-color=\"#0000FF\"/>";

    /// <summary>
    /// A square filled with gradient <c>g</c>, drawn above the baseline.
    /// </summary>
    private const string GlyphBody = "<path d=\"M100 -600 L600 -600 L600 -100 L100 -100 Z\" fill=\"url(#g)\"/>";

    /// <summary>
    /// A linear gradient's own <c>gradientTransform</c> replaces the referenced gradient's rather than combining with it.
    /// </summary>
    [Fact]
    public void LinearGradient_OwnGradientTransformOverridesReferenced()
        => AssertEquivalentPaint(
            $"<linearGradient id=\"base\" gradientTransform=\"rotate(30)\">{Stops}</linearGradient>" +
            "<linearGradient id=\"g\" href=\"#base\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\" gradientTransform=\"matrix(1.5 0.2 -0.1 0.8 0.1 -0.05)\"/>",
            $"<linearGradient id=\"g\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\" gradientTransform=\"matrix(1.5 0.2 -0.1 0.8 0.1 -0.05)\">{Stops}</linearGradient>");

    /// <summary>
    /// A linear gradient without a <c>gradientTransform</c> inherits the referenced gradient's.
    /// </summary>
    [Fact]
    public void LinearGradient_InheritsGradientTransformWhenNotSpecified()
        => AssertEquivalentPaint(
            $"<linearGradient id=\"base\" gradientTransform=\"rotate(30)\">{Stops}</linearGradient>" +
            "<linearGradient id=\"g\" xlink:href=\"#base\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\"/>",
            $"<linearGradient id=\"g\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\" gradientTransform=\"rotate(30)\">{Stops}</linearGradient>");

    /// <summary>
    /// Along a chain of references, the nearest gradient that specifies a <c>gradientTransform</c> supplies it.
    /// </summary>
    [Fact]
    public void LinearGradient_NearestGradientTransformInChainWins()
        => AssertEquivalentPaint(
            $"<linearGradient id=\"base\" gradientTransform=\"rotate(30)\">{Stops}</linearGradient>" +
            "<linearGradient id=\"mid\" href=\"#base\" gradientTransform=\"scale(0.5 2)\"/>" +
            "<linearGradient id=\"g\" href=\"#mid\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\"/>",
            $"<linearGradient id=\"g\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\" gradientTransform=\"scale(0.5 2)\">{Stops}</linearGradient>");

    /// <summary>
    /// A linear gradient's own <c>gradientUnits</c> overrides the referenced gradient's, in either direction.
    /// </summary>
    /// <param name="own">The units specified by the referencing gradient.</param>
    /// <param name="referenced">The units specified by the referenced gradient.</param>
    [Theory]
    [InlineData("userSpaceOnUse", "objectBoundingBox")]
    [InlineData("objectBoundingBox", "userSpaceOnUse")]
    public void LinearGradient_OwnGradientUnitsOverridesReferenced(string own, string referenced)
        => AssertEquivalentPaint(
            $"<linearGradient id=\"base\" gradientUnits=\"{referenced}\">{Stops}</linearGradient>" +
            $"<linearGradient id=\"g\" href=\"#base\" gradientUnits=\"{own}\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\"/>",
            $"<linearGradient id=\"g\" gradientUnits=\"{own}\" x1=\"0.1\" y1=\"0.2\" x2=\"0.9\" y2=\"0.6\">{Stops}</linearGradient>");

    /// <summary>
    /// A linear gradient without <c>gradientUnits</c> inherits the referenced gradient's.
    /// </summary>
    [Fact]
    public void LinearGradient_InheritsGradientUnitsWhenNotSpecified()
        => AssertEquivalentPaint(
            $"<linearGradient id=\"base\" gradientUnits=\"userSpaceOnUse\">{Stops}</linearGradient>" +
            "<linearGradient id=\"g\" href=\"#base\" x1=\"150\" y1=\"-500\" x2=\"550\" y2=\"-200\"/>",
            $"<linearGradient id=\"g\" gradientUnits=\"userSpaceOnUse\" x1=\"150\" y1=\"-500\" x2=\"550\" y2=\"-200\">{Stops}</linearGradient>");

    /// <summary>
    /// A linear gradient's own <c>spreadMethod</c> overrides the referenced gradient's, in either direction.
    /// </summary>
    /// <param name="own">The spread method specified by the referencing gradient.</param>
    /// <param name="referenced">The spread method specified by the referenced gradient.</param>
    [Theory]
    [InlineData("reflect", "repeat")]
    [InlineData("pad", "reflect")]
    [InlineData("repeat", "pad")]
    public void LinearGradient_OwnSpreadMethodOverridesReferenced(string own, string referenced)
        => AssertEquivalentPaint(
            $"<linearGradient id=\"base\" spreadMethod=\"{referenced}\">{Stops}</linearGradient>" +
            $"<linearGradient id=\"g\" href=\"#base\" spreadMethod=\"{own}\" x1=\"0.3\" y1=\"0.5\" x2=\"0.6\" y2=\"0.5\"/>",
            $"<linearGradient id=\"g\" spreadMethod=\"{own}\" x1=\"0.3\" y1=\"0.5\" x2=\"0.6\" y2=\"0.5\">{Stops}</linearGradient>");

    /// <summary>
    /// A specified but unrecognized <c>spreadMethod</c> resolves to the initial value <c>pad</c>
    /// and still overrides the referenced gradient's.
    /// </summary>
    [Fact]
    public void LinearGradient_UnrecognizedSpreadMethodStillOverridesReferenced()
        => AssertEquivalentPaint(
            $"<linearGradient id=\"base\" spreadMethod=\"reflect\">{Stops}</linearGradient>" +
            "<linearGradient id=\"g\" href=\"#base\" spreadMethod=\"bogus\" x1=\"0.3\" y1=\"0.5\" x2=\"0.6\" y2=\"0.5\"/>",
            $"<linearGradient id=\"g\" spreadMethod=\"pad\" x1=\"0.3\" y1=\"0.5\" x2=\"0.6\" y2=\"0.5\">{Stops}</linearGradient>");

    /// <summary>
    /// A linear gradient with its own stops still inherits the attributes it does not specify from the referenced gradient.
    /// </summary>
    [Fact]
    public void LinearGradient_WithOwnStopsInheritsUnspecifiedAttributes()
        => AssertEquivalentPaint(
            "<linearGradient id=\"base\" gradientUnits=\"userSpaceOnUse\" spreadMethod=\"reflect\" gradientTransform=\"rotate(30)\" x1=\"150\" y1=\"-500\" x2=\"350\" y2=\"-200\"><stop offset=\"0\" stop-color=\"#FFFFFF\"/></linearGradient>" +
            $"<linearGradient id=\"g\" href=\"#base\">{Stops}</linearGradient>",
            $"<linearGradient id=\"g\" gradientUnits=\"userSpaceOnUse\" spreadMethod=\"reflect\" gradientTransform=\"rotate(30)\" x1=\"150\" y1=\"-500\" x2=\"350\" y2=\"-200\">{Stops}</linearGradient>");

    /// <summary>
    /// A radial gradient's own <c>gradientTransform</c> replaces the referenced gradient's rather than combining with it.
    /// </summary>
    [Fact]
    public void RadialGradient_OwnGradientTransformOverridesReferenced()
        => AssertEquivalentPaint(
            $"<radialGradient id=\"base\" gradientTransform=\"rotate(30)\">{Stops}</radialGradient>" +
            "<radialGradient id=\"g\" href=\"#base\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\" fx=\"0.35\" fy=\"0.55\" gradientTransform=\"matrix(1.5 0.2 -0.1 0.8 0.1 -0.05)\"/>",
            $"<radialGradient id=\"g\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\" fx=\"0.35\" fy=\"0.55\" gradientTransform=\"matrix(1.5 0.2 -0.1 0.8 0.1 -0.05)\">{Stops}</radialGradient>");

    /// <summary>
    /// A radial gradient without a <c>gradientTransform</c> inherits the referenced gradient's.
    /// </summary>
    [Fact]
    public void RadialGradient_InheritsGradientTransformWhenNotSpecified()
        => AssertEquivalentPaint(
            $"<radialGradient id=\"base\" gradientTransform=\"rotate(30)\">{Stops}</radialGradient>" +
            "<radialGradient id=\"g\" xlink:href=\"#base\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\"/>",
            $"<radialGradient id=\"g\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\" gradientTransform=\"rotate(30)\">{Stops}</radialGradient>");

    /// <summary>
    /// Along a chain of references, the nearest gradient that specifies a <c>gradientTransform</c> supplies it.
    /// </summary>
    [Fact]
    public void RadialGradient_NearestGradientTransformInChainWins()
        => AssertEquivalentPaint(
            $"<radialGradient id=\"base\" gradientTransform=\"rotate(30)\">{Stops}</radialGradient>" +
            "<radialGradient id=\"mid\" href=\"#base\" gradientTransform=\"scale(0.5 2)\"/>" +
            "<radialGradient id=\"g\" href=\"#mid\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\"/>",
            $"<radialGradient id=\"g\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\" gradientTransform=\"scale(0.5 2)\">{Stops}</radialGradient>");

    /// <summary>
    /// A radial gradient's own <c>gradientUnits</c> overrides the referenced gradient's, in either direction.
    /// </summary>
    /// <param name="own">The units specified by the referencing gradient.</param>
    /// <param name="referenced">The units specified by the referenced gradient.</param>
    [Theory]
    [InlineData("userSpaceOnUse", "objectBoundingBox")]
    [InlineData("objectBoundingBox", "userSpaceOnUse")]
    public void RadialGradient_OwnGradientUnitsOverridesReferenced(string own, string referenced)
        => AssertEquivalentPaint(
            $"<radialGradient id=\"base\" gradientUnits=\"{referenced}\">{Stops}</radialGradient>" +
            $"<radialGradient id=\"g\" href=\"#base\" gradientUnits=\"{own}\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\"/>",
            $"<radialGradient id=\"g\" gradientUnits=\"{own}\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\">{Stops}</radialGradient>");

    /// <summary>
    /// A radial gradient without <c>gradientUnits</c> inherits the referenced gradient's.
    /// </summary>
    [Fact]
    public void RadialGradient_InheritsGradientUnitsWhenNotSpecified()
        => AssertEquivalentPaint(
            $"<radialGradient id=\"base\" gradientUnits=\"userSpaceOnUse\">{Stops}</radialGradient>" +
            "<radialGradient id=\"g\" href=\"#base\" cx=\"350\" cy=\"-350\" r=\"200\"/>",
            $"<radialGradient id=\"g\" gradientUnits=\"userSpaceOnUse\" cx=\"350\" cy=\"-350\" r=\"200\">{Stops}</radialGradient>");

    /// <summary>
    /// A radial gradient's own <c>spreadMethod</c> overrides the referenced gradient's, in either direction.
    /// </summary>
    /// <param name="own">The spread method specified by the referencing gradient.</param>
    /// <param name="referenced">The spread method specified by the referenced gradient.</param>
    [Theory]
    [InlineData("reflect", "repeat")]
    [InlineData("pad", "reflect")]
    [InlineData("repeat", "pad")]
    public void RadialGradient_OwnSpreadMethodOverridesReferenced(string own, string referenced)
        => AssertEquivalentPaint(
            $"<radialGradient id=\"base\" spreadMethod=\"{referenced}\">{Stops}</radialGradient>" +
            $"<radialGradient id=\"g\" href=\"#base\" spreadMethod=\"{own}\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\"/>",
            $"<radialGradient id=\"g\" spreadMethod=\"{own}\" cx=\"0.4\" cy=\"0.6\" r=\"0.3\">{Stops}</radialGradient>");

    /// <summary>
    /// A radial gradient with its own stops still inherits the attributes it does not specify from the referenced gradient.
    /// </summary>
    [Fact]
    public void RadialGradient_WithOwnStopsInheritsUnspecifiedAttributes()
        => AssertEquivalentPaint(
            "<radialGradient id=\"base\" gradientUnits=\"userSpaceOnUse\" spreadMethod=\"reflect\" gradientTransform=\"rotate(30)\" cx=\"350\" cy=\"-350\" r=\"200\" fx=\"300\" fy=\"-320\" fr=\"20\"><stop offset=\"0\" stop-color=\"#FFFFFF\"/></radialGradient>" +
            $"<radialGradient id=\"g\" href=\"#base\">{Stops}</radialGradient>",
            $"<radialGradient id=\"g\" gradientUnits=\"userSpaceOnUse\" spreadMethod=\"reflect\" gradientTransform=\"rotate(30)\" cx=\"350\" cy=\"-350\" r=\"200\" fx=\"300\" fy=\"-320\" fr=\"20\">{Stops}</radialGradient>");

    /// <summary>
    /// Renders both documents and asserts that their gradient <c>g</c> resolves to the same paint.
    /// </summary>
    /// <param name="chainedDefs">Definitions in which gradient <c>g</c> references other gradients.</param>
    /// <param name="flatDefs">Definitions in which gradient <c>g</c> specifies the expected attributes directly.</param>
    private static void AssertEquivalentPaint(string chainedDefs, string flatDefs)
    {
        Paint expected = RenderPaint(flatDefs);
        Paint actual = RenderPaint(chainedDefs);

        Assert.Equal(expected.GetType(), actual.GetType());
        AssertEqual(expected.Transform, actual.Transform);

        switch (expected)
        {
            case LinearGradientPaint e:
            {
                LinearGradientPaint a = (LinearGradientPaint)actual;
                Assert.Equal(e.Units, a.Units);
                Assert.Equal(e.Spread, a.Spread);
                AssertEqual(e.P0, a.P0);
                AssertEqual(e.P1, a.P1);
                Assert.Equal(e.P2.HasValue, a.P2.HasValue);
                AssertEqual(e.P2.GetValueOrDefault(), a.P2.GetValueOrDefault());
                Assert.Equal(e.Stops, a.Stops);
                break;
            }

            case RadialGradientPaint e:
            {
                RadialGradientPaint a = (RadialGradientPaint)actual;
                Assert.Equal(e.Units, a.Units);
                Assert.Equal(e.Spread, a.Spread);
                AssertEqual(e.Center0, a.Center0);
                AssertEqual(e.Center1, a.Center1);
                Assert.Equal(e.Radius0, a.Radius0, 3);
                Assert.Equal(e.Radius1, a.Radius1, 3);
                Assert.Equal(e.Stops, a.Stops);
                break;
            }

            default:
            {
                Assert.Fail($"Expected a gradient paint but found {expected.GetType().Name}.");
                break;
            }
        }
    }

    /// <summary>
    /// Renders the glyph of a font whose SVG document holds the given definitions and returns the single paint it draws with.
    /// </summary>
    /// <param name="defs">The SVG definitions placed outside the glyph's group element.</param>
    /// <returns>The paint passed to the renderer.</returns>
    private static Paint RenderPaint(string defs)
    {
        Font font = SvgTestFonts.CreateFont(GlyphBody, defs);
        TextOptions options = new(font) { ColorFontSupport = ColorFontSupport.Svg };

        PaintCaptureRenderer renderer = new();
        TextRenderer.RenderTo(renderer, "A", options);

        return Assert.Single(renderer.Paints);
    }

    /// <summary>
    /// Asserts that two matrices are equal within a small tolerance.
    /// </summary>
    /// <param name="expected">The expected matrix.</param>
    /// <param name="actual">The actual matrix.</param>
    private static void AssertEqual(Matrix3x2 expected, Matrix3x2 actual)
    {
        Assert.Equal(expected.M11, actual.M11, 3);
        Assert.Equal(expected.M12, actual.M12, 3);
        Assert.Equal(expected.M21, actual.M21, 3);
        Assert.Equal(expected.M22, actual.M22, 3);
        Assert.Equal(expected.M31, actual.M31, 3);
        Assert.Equal(expected.M32, actual.M32, 3);
    }

    /// <summary>
    /// Asserts that two points are equal within a small tolerance.
    /// </summary>
    /// <param name="expected">The expected point.</param>
    /// <param name="actual">The actual point.</param>
    private static void AssertEqual(Vector2 expected, Vector2 actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }

    /// <summary>
    /// A renderer that records the paint of every layer it is asked to draw.
    /// </summary>
    private sealed class PaintCaptureRenderer : GlyphRenderer
    {
        /// <summary>
        /// Gets the paints passed to <see cref="BeginLayer(Paint, FillRule)"/>, in order.
        /// </summary>
        public List<Paint> Paints { get; } = [];

        /// <inheritdoc/>
        public override void BeginLayer(Paint paint, FillRule fillRule)
        {
            this.Paints.Add(paint);
            base.BeginLayer(paint, fillRule);
        }
    }
}
