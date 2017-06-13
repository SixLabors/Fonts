using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tests
{
    using Moq;
    using SixLabors.Fonts.Tests.Fakes;
    using SixLabors.Primitives;
    using System.Numerics;
    using Xunit;

    public class GlyphTests
    {
        GlyphRenderer renderer = new GlyphRenderer();
        Glyph glyph = new Glyph(new GlyphInstance(new Vector2[0], new bool[0], new ushort[0], new Bounds(0, 1, 0, 1), 0, 0, 1, 0), 10);
        [Fact]
        public void RenderToPointAndSingleDPI()
        {
            var locationInFontSpace = new PointF(99, 99) / 72;
            glyph.RenderTo(renderer, locationInFontSpace, 72, 0);

            Assert.Equal(new RectangleF(99, 99, 0, 0), renderer.GlyphRects.Single());
        }
    }
}
