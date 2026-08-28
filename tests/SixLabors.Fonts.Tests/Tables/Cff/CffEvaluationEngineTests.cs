// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.Cff;

namespace SixLabors.Fonts.Tests.Tables.Cff;

public class CffEvaluationEngineTests
{
    // This is the raw Type 2 prefix that reproduces the fixed-point coordinates from
    // glyph 54 of the Igrunok repro font. The font itself is not redistributed.
    private static readonly byte[] IgrunokGlyph54Prefix =
    [
        0xF8, 0x66,
        0xFF, 0x01, 0xBE, 0x88, 0xC3,
        0xFF, 0x00, 0x4E, 0xD0, 0x92,
        0x15,
        0xFF, 0xFF, 0xE7, 0x82, 0x98,
        0xFF, 0x00, 0x10, 0xD0, 0x9F,
        0xFF, 0xFF, 0xC9, 0x66, 0x95,
        0xFF, 0x00, 0x3D, 0x98, 0xAE,
        0xFF, 0xFF, 0xE4, 0xB0, 0x7E,
        0xFF, 0x00, 0x23, 0x01, 0xE4,
        0x08,
        0xFF, 0x00, 0x56, 0x1B, 0xAE,
        0xFF, 0x00, 0x22, 0x49, 0x2B,
        0xFF, 0x00, 0x14, 0xFD, 0xC7,
        0xFF, 0x00, 0x62, 0x00, 0xD2,
        0xFF, 0xFF, 0xED, 0xC9, 0x21,
        0xFF, 0x00, 0x4B, 0x9C, 0xCB,
        0x08,
        0x0E
    ];

    [Fact]
    public void Fixed1616_UsesUnsignedFraction()
    {
        SimpleBinaryReader reader = new([0x01, 0xBE, 0x88, 0xC3]);

        Assert.Equal(446.534225F, reader.ReadFloatFixed1616(), 4);
    }

    [Fact]
    public void RawIgrunokGlyph54Prefix_PreservesCurrentPointAfterEachCurve()
    {
        using CffEvaluationEngine engine = new(
            IgrunokGlyph54Prefix,
            Array.Empty<byte[]>(),
            Array.Empty<byte[]>(),
            0,
            1);
        GlyphRenderer renderer = new();

        engine.RenderTo(renderer, Vector2.Zero, Vector2.One, Vector2.Zero, Matrix3x2.Identity);

        Assert.Equal(7, renderer.ControlPoints.Count);
        AssertPoint(renderer.ControlPoints[0], 446.534225F, -78.814728F);
        AssertPoint(renderer.ControlPoints[3], 340.13449F, -192.23344F);
        AssertPoint(renderer.ControlPoints[6], 429.01959F, -400.13495F);
    }

    private static void AssertPoint(Vector2 actual, float expectedX, float expectedY)
    {
        Assert.Equal(expectedX, actual.X, 4);
        Assert.Equal(expectedY, actual.Y, 4);
    }
}
