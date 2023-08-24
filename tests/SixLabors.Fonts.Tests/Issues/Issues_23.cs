// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_23
    {
        [Fact]
        public void BleadingFonts()
        {
            // wendy one returns wrong points for 'o'
            Font font = new FontCollection().Add(TestFonts.WendyOneFile).CreateFont(12);

            var r = new GlyphRenderer();

            new TextRenderer(r).RenderText("o", new TextOptions(new Font(font, 30)));

            Assert.DoesNotContain(System.Numerics.Vector2.Zero, r.ControlPoints);
        }
    }
}
