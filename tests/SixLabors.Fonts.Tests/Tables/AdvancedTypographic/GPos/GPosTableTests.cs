// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

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
            Vector2[] expectedControlPoints =
            {
                new(1.4648438f, 1.734375f),
                new(1.4648438f, 7.421875f),
                new(0.71484375f, 7.421875f),
                new(0.71484375f, 1.734375f),
                new(1.4648438f, 1.734375f),
                new(6.9804688f, 7.421875f),
                new(6.4375f, 5.9375f),
                new(4.0546875f, 5.9375f),
                new(3.5195312f, 7.421875f),
                new(2.7460938f, 7.421875f),
                new(4.9179688f, 1.734375f),
                new(5.5742188f, 1.734375f),
                new(7.75f, 7.421875f),
                new(6.9804688f, 7.421875f),
                new(5.2460938f, 2.6601562f),
                new(4.28125f, 5.3203125f),
                new(6.2148438f, 5.3203125f),
                new(5.2460938f, 2.6601562f)
            };

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gPosFont)
            {
                ApplyKerning = true
            });

            // assert
            Assert.Equal(expectedControlPoints.Length, renderer.ControlPoints.Count);
            for (int i = 0; i < expectedControlPoints.Length; i++)
            {
                Assert.Equal(expectedControlPoints[i], renderer.ControlPoints[i]);
            }
        }
    }
}
