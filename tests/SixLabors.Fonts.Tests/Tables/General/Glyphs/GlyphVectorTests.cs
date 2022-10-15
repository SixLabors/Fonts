// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General.Glyphs
{
    public class GlyphVectorTests
    {
        [Fact]
        public void CloneIsDeep()
        {
            // arrange
            Vector2[] controlPoints = { new(1.0f), new(2.0f) };
            bool[] onCurves = { true, false };
            ushort[] endPoints = { 1, 2, 3 };
            var bounds = new Bounds(1.0f, 2.0f, 3.0f, 4.0f);
            var glyphVector = new GlyphVector(controlPoints, onCurves, endPoints, bounds, Array.Empty<byte>());
            GlyphOutline outline = glyphVector.GetOutline();

            // act
            GlyphOutline clone = GlyphVector.DeepClone(glyphVector).GetOutline();

            // assert
            Assert.False(outline.ControlPoints.Equals(clone.ControlPoints));
            Assert.True(outline.ControlPoints.Span.SequenceEqual(clone.ControlPoints.Span));
            Assert.False(outline.OnCurves.Equals(clone.OnCurves));
            Assert.True(outline.OnCurves.Span.SequenceEqual(clone.OnCurves.Span));
            Assert.False(outline.EndPoints.Equals(clone.EndPoints));
            Assert.True(outline.EndPoints.Span.SequenceEqual(clone.EndPoints.Span));
        }

        [Fact]
        public void TtfOffsetXy_Works()
        {
            // arrange
            Vector2[] controlPoints = { new(1.0f), new(2.0f) };
            bool[] onCurves = { true, false };
            ushort[] endPoints = { 1, 2, 3 };
            var bounds = new Bounds(1.0f, 2.0f, 3.0f, 4.0f);
            var expectedBounds = new Bounds(11.0f, 12.0f, 13.0f, 14.0f);
            var glyphVector = new GlyphVector(controlPoints, onCurves, endPoints, bounds, Array.Empty<byte>());

            // act
            Matrix3x2 matrix = Matrix3x2.Identity;
            matrix.Translation = new Vector2(10, 10);
            GlyphVector.TransformInPlace(ref glyphVector, matrix);

            // assert
            Assert.Equal(expectedBounds, glyphVector.GetBounds());
        }

        [Fact]
        public void TtfAppendGlyph_Works()
        {
            // arrange
            Vector2[] controlPoints = { new(1.0f), new(2.0f) };
            Vector2[] expectedControlPoints = { new(1.0f), new(2.0f), new(1.0f), new(2.0f) };
            bool[] onCurves = { true, false };
            ushort[] endPoints = { 1, 2, 3 };
            var bounds = new Bounds(1.0f, 2.0f, 3.0f, 4.0f);
            var glyphVector1 = new GlyphVector(controlPoints, onCurves, endPoints, bounds, Array.Empty<byte>());
            var glyphVector2 = new GlyphVector(controlPoints, onCurves, endPoints, bounds, Array.Empty<byte>());

            // act
            var appended = GlyphVector.Append(glyphVector1, glyphVector2, default);

            // assert
            Assert.True(expectedControlPoints.AsSpan().SequenceEqual(appended.GetOutline().ControlPoints.Span));
        }

        [Fact]
        public void TtfTransformWith2x2Matrix_Works()
        {
            // arrange
            int precision = 2;
            Vector2[] controlPoints =
            {
                new(653.0f, 791.0f),
                new(1065.0f, 791.0f),
                new(1065.0f, 653.0f),
                new(653.0f, 653.0f),
                new(653.0f, 227.0f),
                new(514.0f, 227.0f),
                new(514.0f, 653.0f),
                new(104.0f, 653.0f),
                new(104.0f, 791.0f),
                new(514.0f, 791.0f),
                new(514.0f, 1219.0f),
                new(653.0f, 1219.0f),
                new(104.0f, 1.0f),
                new(104.0f, 139.0f),
                new(1065.0f, 139.0f),
                new(1065.0f, 1.0f),
            };
            bool[] onCurves = { true, false };
            ushort[] endPoints = { 1, 2, 3 };
            var bounds = new Bounds(16130.0f, 260.0f, 26624.0f, 28928.0f);
            var expectedBounds = new Bounds(19876f, 013684f, 89804.8f, 108083.2f);
            var glyphVector = new GlyphVector(controlPoints, onCurves, endPoints, bounds, Array.Empty<byte>());

            // act
            Matrix3x2 matrix = Matrix3x2.Identity;
            matrix.M11 = 1.2F;
            matrix.M12 = 0.8F;
            matrix.M21 = 2.0F;
            matrix.M22 = 3.0F;
            GlyphVector.TransformInPlace(ref glyphVector, matrix);

            // assert
            Bounds transformedBounds = glyphVector.GetBounds();
            Assert.Equal(expectedBounds.Min.X, transformedBounds.Min.X, precision);
            Assert.Equal(expectedBounds.Min.Y, transformedBounds.Min.Y, precision);
            Assert.Equal(expectedBounds.Max.X, transformedBounds.Max.X, precision);
            Assert.Equal(expectedBounds.Max.Y, transformedBounds.Max.Y, precision);
        }
    }
}
