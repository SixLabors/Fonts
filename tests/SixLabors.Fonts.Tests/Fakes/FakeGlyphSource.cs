using System.Numerics;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tests.Fakes
{
    internal class FakeGlyphSource
    {
        public GlyphVector Vector { get; }

        public FakeGlyphSource(int codePoint, ushort index)
            : this(codePoint,
                  index,
                  new GlyphVector(new Vector2[] { new Vector2(10, 10), new Vector2(10, 20), new Vector2(20, 20), new Vector2(20, 10) },
                  new bool[] { true, true, true, true },
                  new ushort[] { 3 },
                  new Bounds(10, 10, 20, 20)))
        {
        }

        public FakeGlyphSource(int codePoint, ushort index, GlyphVector vector)
        {
            this.CodePoint = codePoint;
            this.Vector = vector;
            this.Index = index;
        }

        public int CodePoint { get; private set; }

        public ushort Index { get; private set; }
    }
}
