// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.Tables.General;
using Xunit;
using static SixLabors.Fonts.Tests.WriterExtensions;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class ColrTableTests
    {
        [Fact]
        public void ShouldReturnNullWhenTableCouldNotBeFound()
        {
            var writer = new BigEndianBinaryWriter();
            writer.WriteTrueTypeFileHeader();

            using (System.IO.MemoryStream stream = writer.GetStream())
            {
                Assert.Null(ColrTable.Load(new FontReader(stream)));
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
                        new ColrLayerRecord { Glyph = 10, Pallete = 1 },
                        new ColrLayerRecord { Glyph = 11, Pallete = 2 }
                    }
                },
                new ColrGlyphRecord
                {
                    Glyph = 2,
                    Layers =
                    {
                        new ColrLayerRecord { Glyph = 12, Pallete = 1 },
                        new ColrLayerRecord { Glyph = 13, Pallete = 2 }
                    }
                }
            });

            using (System.IO.Stream stream = TestFonts.TwemojiMozillaData())
            {
                var reader = new FontReader(stream);
                ColrTable tbl = reader.GetTable<ColrTable>();

                System.Span<Fonts.Tables.General.Colr.LayerRecord> layers = tbl.GetLayers(15);
                Assert.Equal(2, layers.Length);
            }
        }
    }
}
