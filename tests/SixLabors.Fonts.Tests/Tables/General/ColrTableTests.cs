// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.General.Colr;
using static SixLabors.Fonts.Tests.WriterExtensions;

namespace SixLabors.Fonts.Tests.Tables.General;

public class ColrTableTests
{
    [Fact]
    public void ShouldReturnNullWhenTableCouldNotBeFound()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteTrueTypeFileHeader();

        using (MemoryStream stream = writer.GetStream())
        {
            using var reader = new FontReader(stream);
            Assert.Null(ColrTable.Load(reader));
        }
    }

    [Fact]
    public void ShouldReturnTableValues()
    {
        var writer = new BigEndianBinaryWriter();
        writer.WriteTrueTypeFileHeader();
        writer.WriteColrTable(new[]
        {
            new ColrGlyphRecord
            {
                Glyph = 1,
                Layers =
                {
                    new ColrLayerRecord { Glyph = 10, Palette = 1 },
                    new ColrLayerRecord { Glyph = 11, Palette = 2 }
                }
            },
            new ColrGlyphRecord
            {
                Glyph = 2,
                Layers =
                {
                    new ColrLayerRecord { Glyph = 12, Palette = 1 },
                    new ColrLayerRecord { Glyph = 13, Palette = 2 }
                }
            }
        });

        using (Stream stream = TestFonts.TwemojiMozillaData())
        {
            using var reader = new FontReader(stream);
            ColrTable tbl = reader.GetTable<ColrTable>();

            Span<LayerRecord> layers = tbl.GetLayers(15);
            Assert.Equal(2, layers.Length);
        }
    }
}
