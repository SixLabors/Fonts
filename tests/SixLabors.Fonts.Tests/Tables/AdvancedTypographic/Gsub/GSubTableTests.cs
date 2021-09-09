// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using System.Numerics;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.Gsub
{
    public class GSubTableTests
    {
        // TODO: add all other arabic letters
        [Fact]
        public void RenderArabicCharacters_WithIsolatedForm_Works()
        {
            // arrange
            Font arabicFont = new FontCollection().Add(TestFonts.ArabicFontFile).CreateFont(8);
            string testStr = "ب";
            var rendererTtf = new ColorGlyphRenderer();
            bool applyKerning = true;
            Vector2[] expectedControlPoints = new[]
            {
                new Vector2(5.904f, 6.344f), new Vector2(5.904f, 6.344f), new Vector2(6.08f, 6.752f),
                new Vector2(6.1759996f, 7.12f), new Vector2(6.1759996f, 7.12f), new Vector2(6.1759996f, 7.12f),
                new Vector2(6.272f, 7.4879994f), new Vector2(6.272f, 7.7359996f), new Vector2(6.272f, 7.7359996f),
                new Vector2(6.272f, 7.7359996f), new Vector2(6.272f, 8.063999f), new Vector2(6.152f, 8.331999f),
                new Vector2(6.152f, 8.331999f), new Vector2(6.152f, 8.331999f), new Vector2(6.032f, 8.599999f),
                new Vector2(5.776f, 8.752f), new Vector2(5.776f, 8.752f), new Vector2(5.776f, 8.752f),
                new Vector2(5.184f, 9.12f), new Vector2(3.608f, 9.12f), new Vector2(3.608f, 9.12f),
                new Vector2(3.064f, 9.12f), new Vector2(3.064f, 9.12f), new Vector2(2.16f, 9.12f),
                new Vector2(1.5680001f, 8.976f), new Vector2(1.5680001f, 8.976f), new Vector2(1.5680001f, 8.976f),
                new Vector2(0.976f, 8.832f), new Vector2(0.668f, 8.507999f), new Vector2(0.668f, 8.507999f),
                new Vector2(0.668f, 8.507999f), new Vector2(0.36f, 8.184f), new Vector2(0.36f, 7.648f),
                new Vector2(0.36f, 7.648f), new Vector2(0.36f, 7.648f), new Vector2(0.36f, 7.3999996f),
                new Vector2(0.39600003f, 7.1559997f), new Vector2(0.39600003f, 7.1559997f),
                new Vector2(0.39600003f, 7.1559997f), new Vector2(0.432f, 6.9119997f), new Vector2(0.488f, 6.752f),
                new Vector2(0.488f, 6.752f), new Vector2(0.96f, 6.5759993f), new Vector2(1.024f, 6.608f),
                new Vector2(1.024f, 6.608f), new Vector2(0.944f, 7.0079994f), new Vector2(0.944f, 7.3359995f),
                new Vector2(0.944f, 7.3359995f), new Vector2(0.944f, 7.3359995f), new Vector2(0.944f, 7.7999997f),
                new Vector2(1.2f, 8.052f), new Vector2(1.2f, 8.052f), new Vector2(1.2f, 8.052f),
                new Vector2(1.456f, 8.304f), new Vector2(1.948f, 8.4f), new Vector2(1.948f, 8.4f),
                new Vector2(1.948f, 8.4f), new Vector2(2.44f, 8.495999f), new Vector2(3.232f, 8.495999f),
                new Vector2(3.232f, 8.495999f), new Vector2(3.792f, 8.495999f), new Vector2(3.792f, 8.495999f),
                new Vector2(4.392f, 8.495999f), new Vector2(4.9160004f, 8.4279995f),
                new Vector2(4.9160004f, 8.4279995f), new Vector2(4.9160004f, 8.4279995f), new Vector2(5.44f, 8.36f),
                new Vector2(5.68f, 8.247999f), new Vector2(5.68f, 8.247999f), new Vector2(5.68f, 8.247999f),
                new Vector2(5.696f, 8.12f), new Vector2(5.696f, 8.063999f), new Vector2(5.696f, 8.063999f),
                new Vector2(5.696f, 8.063999f), new Vector2(5.696f, 7.8159995f), new Vector2(5.604f, 7.5399995f),
                new Vector2(5.604f, 7.5399995f), new Vector2(5.604f, 7.5399995f), new Vector2(5.512f, 7.2639995f),
                new Vector2(5.28f, 6.752f), new Vector2(5.28f, 6.752f), new Vector2(5.832f, 6.344f),
                new Vector2(5.904f, 6.344f), new Vector2(3.84f, 10.176f), new Vector2(3.432f, 10.792f),
                new Vector2(2.776f, 10.3359995f), new Vector2(3.184f, 9.736f), new Vector2(3.84f, 10.176f)
            };

            // act
            TextRenderer.RenderTextTo(rendererTtf, testStr, new RendererOptions(arabicFont)
            {
                ApplyKerning = applyKerning,
                ColorFontSupport = ColorFontSupport.MicrosoftColrFormat
            });

            // assert
            Assert.Equal(expectedControlPoints.Length, rendererTtf.ControlPoints.Count);
            Assert.True(rendererTtf.ControlPoints.SequenceEqual(expectedControlPoints));
        }
    }
}
