// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using System.Numerics;
using SixLabors.Fonts.Tables.General.Glyphs;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General.Glyphs
{
    public class GlyphVectorTests
    {
        [Fact]
        public void CloneIsDeep()
        {
            // arrange
            Vector2[] controlPoints = { new Vector2(1.0f), new Vector2(2.0f) };
            bool[] onCurves = { true, false };
            ushort[] endPoints = { 1, 2, 3 };
            var bounds = new Bounds(1.0f, 2.0f, 3.0f, 4.0f);
            var glyphVector = new GlyphVector(controlPoints, onCurves, endPoints, bounds);

            // act
            var clone = (GlyphVector)glyphVector.DeepClone();

            // assert
            Assert.False(glyphVector.ControlPoints.Equals(clone.ControlPoints));
            Assert.True(glyphVector.ControlPoints.SequenceEqual(clone.ControlPoints));
            Assert.False(glyphVector.OnCurves.Equals(clone.OnCurves));
            Assert.True(glyphVector.OnCurves.SequenceEqual(clone.OnCurves));
            Assert.False(glyphVector.EndPoints.Equals(clone.EndPoints));
            Assert.True(glyphVector.EndPoints.SequenceEqual(clone.EndPoints));
            Assert.True(glyphVector.Bounds.Equals(clone.Bounds));
        }
    }
}
