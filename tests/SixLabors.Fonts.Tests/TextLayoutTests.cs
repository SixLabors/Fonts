using Moq;
using SixLabors.Fonts.Tables.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using SixLabors.Fonts.Tables.General.CMap;
using SixLabors.Fonts.Tables.General.Glyphs;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TextLayoutTests
    {
        [Fact]
        public void FakeFontGetGlyph()
        {
            var font = CreateFont("hello world");
            var glyph = font.GetGlyph('h');
            Assert.NotNull(glyph);
        }

        [Theory]
        [InlineData("hello world", 20, 330)]
        [InlineData("hello world\nhello world",
            50, //30 actaul line height + 20 actual height
            330)]
        [InlineData("hello\nworld",
            50, //30 actaul line height + 20 actual height
            150)]
        public void MeasureText(string text, float height, float width)
        {
            var font = CreateFont(text);

            var scaleFactor = 72 * font.EmSize; // 72 * emSize means 1 point = 1px 
            var size = new TextMeasurer().MeasureText(text, font, 72 * font.EmSize);

            Assert.Equal(height, size.Height, 4);
            Assert.Equal(width, size.Width, 4);
        }

        public static Font CreateFont(string text)
        {
            var fc = new FontCollection();
            var d = fc.Install(new FakeFontInstance(text));
            return new Font(d, 1, fc);
        }
    }

    internal class FakeFontInstance : FontInstance
    {
        internal FakeFontInstance(string text)
            : this(text.Distinct().Select((x, i) => new FakeGlyphSource(x, (ushort)i)).ToList())
        {
        }

        internal FakeFontInstance(List<FakeGlyphSource> glyphs)
            : base(GenerateNameTable(), GenerateCMapTable(glyphs), new FakeGlyphTable(glyphs)
                  , GenerateOS2Table(), GenerateHorizontalMetricsTable(glyphs), GenerateHeadTable(glyphs), new KerningTable(new Fonts.Tables.General.Kern.KerningSubTable[0]))
        {

        }
        static NameTable GenerateNameTable()
        {
            return new NameTable(new[] {
                new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FullFontName, "name"),
                new Fonts.Tables.General.Name.NameRecord(WellKnownIds.PlatformIDs.Windows, 0, WellKnownIds.NameIds.FontFamilyName, "name")
            }, new string[0]);
        }

        static CMapTable GenerateCMapTable(List<FakeGlyphSource> glyphs)
        {
            return new CMapTable(new[] { new FakeCmapSubtable(glyphs) });
        }
        static OS2Table GenerateOS2Table()
        {
            return new OS2Table(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, new byte[0], 1, 1, 1, 1, "", OS2Table.FontStyleSelection.ITALIC, 1, 1, 20, 10, 20, 1, 1);
        }
        static HorizontalMetricsTable GenerateHorizontalMetricsTable(List<FakeGlyphSource> glyphs)
        {
            return new HorizontalMetricsTable(glyphs.Select(x => (ushort)30).ToArray(), glyphs.Select(x => (short)10).ToArray());
        }
        static HeadTable GenerateHeadTable(List<FakeGlyphSource> glyphs)
        {
            return new HeadTable(HeadTable.HeadFlags.ForcePPEMToInt, HeadTable.HeadMacStyle.None, 30, DateTime.Now, DateTime.Now, new Bounds(10, 10, 20, 20), 1, HeadTable.IndexLocationFormats.Offset16);
        }
    }
    internal class FakeGlyphTable : GlyphTable
    {
        private List<FakeGlyphSource> glyphs;

        public FakeGlyphTable(List<FakeGlyphSource> glyphs)
            : base(new GlyphLoader[glyphs.Count])
        {
            this.glyphs = glyphs;
        }

        internal override GlyphVector GetGlyph(int index)
        {
            foreach (var c in glyphs)
            {
                if (c.Index == index)
                {
                    return c.Vector;
                }
            }
            return default(GlyphVector);
        }
    }

    internal class FakeCmapSubtable : CMapSubTable
    {
        private readonly List<FakeGlyphSource> glyphs;

        public FakeCmapSubtable(List<FakeGlyphSource> glyphs)
        {
            this.glyphs = glyphs;
        }
        public override ushort GetGlyphId(char character)
        {
            foreach (var c in glyphs)
            {
                if (c.Character == character)
                {
                    return c.Index;
                }
            }
            return 0;
        }
    }
    internal class FakeGlyphSource
    {
        public GlyphVector Vector { get; }

        public FakeGlyphSource(char character, ushort index)
            : this(character, index, new GlyphVector(new Vector2[] {
                new Vector2(10,10),new Vector2(10,20),new Vector2(20,20) ,new Vector2(20,10)
            }, new bool[] { true, true, true, true }, new ushort[] { 3 }, new Bounds(10, 10, 20, 20)))
        { }
        public FakeGlyphSource(char character, ushort index, GlyphVector vector)
        {
            this.Character = character;
            this.Vector = vector;
            this.Index = index;
        }

        public char Character { get; private set; }
        public ushort Index { get; private set; }
    }
}