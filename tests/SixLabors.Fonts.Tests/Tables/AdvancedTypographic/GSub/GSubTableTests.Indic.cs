// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GSub
{
    public partial class GSubTableTests
    {
        private readonly Font kannadaFontTTF = CreateKannadaFont();

        private static Font CreateKannadaFont()
        {
            var collection = new FontCollection();
            FontFamily family = collection.Add(TestFonts.NotoSerifKannadaRegular);
            return family.CreateFont(12);
        }

        [Theory]
        [InlineData("\u0cb2\u0ccd\u0cb2\u0cbf", new int[] { 250, 126 })]
        public void CanShapeKannadaText(string input, int[] expectedGlyphIndices)
        {
            ColorGlyphRenderer renderer = new();
            TextRenderer.RenderTextTo(renderer, input, new TextOptions(this.kannadaFontTTF));

            Assert.Equal(expectedGlyphIndices.Length, renderer.GlyphKeys.Count);
            for (int i = 0; i < expectedGlyphIndices.Length; i++)
            {
                Assert.Equal(expectedGlyphIndices[i], renderer.GlyphKeys[i].GlyphId);
            }
        }
    }
}
