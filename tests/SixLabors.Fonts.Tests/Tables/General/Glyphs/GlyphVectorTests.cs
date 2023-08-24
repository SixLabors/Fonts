// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tests.Tables.General.Glyphs;

public class GlyphVectorTests
{
    [Fact]
    public void CloneIsDeep()
    {
        // arrange
        ControlPoint[] controlPoints = { new(new Vector2(1.0f), true), new(new Vector2(2.0f), false) };
        ushort[] endPoints = { 1, 2, 3 };
        var bounds = new Bounds(1.0f, 2.0f, 3.0f, 4.0f);
        var outline = new GlyphVector(controlPoints, endPoints, bounds, Array.Empty<byte>(), false);

        // act
        var clone = GlyphVector.DeepClone(outline);

        // assert
        Assert.False(outline.ControlPoints.Equals(clone.ControlPoints));
        Assert.True(outline.ControlPoints.SequenceEqual(clone.ControlPoints));
        Assert.False(outline.EndPoints.Equals(clone.EndPoints));
        Assert.True(outline.EndPoints.SequenceEqual(clone.EndPoints));
    }

    [Fact]
    public void TtfOffsetXy_Works()
    {
        // arrange
        ControlPoint[] controlPoints = { new(new Vector2(1.0f), true), new(new Vector2(2.0f), false) };
        ushort[] endPoints = { 1, 2, 3 };
        var bounds = new Bounds(1.0f, 2.0f, 3.0f, 4.0f);
        var expectedBounds = new Bounds(11.0f, 12.0f, 13.0f, 14.0f);
        var glyphVector = new GlyphVector(controlPoints, endPoints, bounds, Array.Empty<byte>(), false);

        // act
        Matrix3x2 matrix = Matrix3x2.Identity;
        matrix.Translation = new Vector2(10, 10);
        GlyphVector.TransformInPlace(ref glyphVector, matrix);

        // assert
        Assert.Equal(expectedBounds, glyphVector.Bounds);
    }

    [Fact]
    public void TtfTransformWith2x2Matrix_Works()
    {
        // arrange
        const float precision = 2F;
        ControlPoint[] controlPoints =
        {
            new(new Vector2(653.0f, 791.0f), true),
            new(new Vector2(1065.0f, 791.0f), false),
            new(new Vector2(1065.0f, 653.0f), true),
            new(new Vector2(653.0f, 653.0f), false),
            new(new Vector2(653.0f, 227.0f), true),
            new(new Vector2(514.0f, 227.0f), false),
            new(new Vector2(514.0f, 653.0f), true),
            new(new Vector2(104.0f, 653.0f), false),
            new(new Vector2(104.0f, 791.0f), true),
            new(new Vector2(514.0f, 791.0f), false),
            new(new Vector2(514.0f, 1219.0f), true),
            new(new Vector2(653.0f, 1219.0f), false),
            new(new Vector2(104.0f, 1.0f), true),
            new(new Vector2(104.0f, 139.0f), false),
            new(new Vector2(1065.0f, 139.0f), true),
            new(new Vector2(1065.0f, 1.0f), false),
        };

        ushort[] endPoints = { 1, 2, 3 };
        var bounds = new Bounds(16130.0f, 260.0f, 26624.0f, 28928.0f);
        var expectedBounds = new Bounds(19876f, 013684f, 89804.8f, 108083.2f);
        var glyphVector = new GlyphVector(controlPoints, endPoints, bounds, Array.Empty<byte>(), false);

        // act
        Matrix3x2 matrix = Matrix3x2.Identity;
        matrix.M11 = 1.2F;
        matrix.M12 = 0.8F;
        matrix.M21 = 2.0F;
        matrix.M22 = 3.0F;
        GlyphVector.TransformInPlace(ref glyphVector, matrix);

        // assert
        Bounds transformedBounds = glyphVector.Bounds;
        Assert.Equal(expectedBounds.Min.X, transformedBounds.Min.X, precision);
        Assert.Equal(expectedBounds.Min.Y, transformedBounds.Min.Y, precision);
        Assert.Equal(expectedBounds.Max.X, transformedBounds.Max.X, precision);
        Assert.Equal(expectedBounds.Max.Y, transformedBounds.Max.Y, precision);
    }
}
