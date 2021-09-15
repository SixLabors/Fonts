// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using System.Numerics;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GPos
{
    public class GPosTableTests
    {
        [Fact]
        public void SingleAdjustmentPositioning_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposTestFontFile).CreateFont(8);
            var renderer = new ColorGlyphRenderer();
            string testStr = "IA"; // character A should be placed slightly to the right.
            Vector2[] expectedControlPoints = new[]
            {
                new Vector2(1.4648438f, 1.734375f),
                new Vector2(1.4648438f, 7.421875f),
                new Vector2(0.71484375f, 7.421875f),
                new Vector2(0.71484375f, 1.734375f),
                new Vector2(1.4648438f, 1.734375f),
                new Vector2(6.9804688f, 7.421875f),
                new Vector2(6.4375f, 5.9375f),
                new Vector2(4.0546875f, 5.9375f),
                new Vector2(3.5195312f, 7.421875f),
                new Vector2(2.7460938f, 7.421875f),
                new Vector2(4.9179688f, 1.734375f),
                new Vector2(5.5742188f, 1.734375f),
                new Vector2(7.75f, 7.421875f),
                new Vector2(6.9804688f, 7.421875f),
                new Vector2(5.2460938f, 2.6601562f),
                new Vector2(4.28125f, 5.3203125f),
                new Vector2(6.2148438f, 5.3203125f),
                new Vector2(5.2460938f, 2.6601562f)
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gPosFont)
            {
                ApplyKerning = true
            });

            // assert
            Assert.Equal(expectedControlPoints.Length, renderer.ControlPoints.Count);
            Assert.True(renderer.ControlPoints.SequenceEqual(expectedControlPoints));
        }
    }
}
