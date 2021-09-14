// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using DiffEngine;
using VerifyXunit;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic.GPos
{
    [UsesVerify]
    public class GPosTableTests
    {
        public GPosTableTests() => DiffTools.UseOrder(DiffTool.DiffMerge, DiffTool.TortoiseMerge, DiffTool.Rider, DiffTool.VisualStudio);

        [Fact]
        public Task SingleAdjustmentPositioning_Works()
        {
            // arrange
            Font gPosFont = new FontCollection().Add(TestFonts.GposTestFontFile).CreateFont(12);
            var renderer = new ColorGlyphRenderer();
            string testStr = "BAB"; // character a should be placed slightly to the right.

            // act
            TextRenderer.RenderTextTo(renderer, testStr, new RendererOptions(gPosFont)
            {
                ApplyKerning = true
            });

            // assert
            return Verifier.Verify(renderer);
        }
    }
}
