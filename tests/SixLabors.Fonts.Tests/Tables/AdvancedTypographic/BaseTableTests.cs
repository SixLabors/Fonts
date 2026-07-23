// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers.Binary;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic;

public class BaseTableTests
{
    [Fact]
    public void LoadBaseTable_ReadsBothAxesAndAllCoordFormats()
    {
        BaseTable table = BaseTable.Load(CreateFullTableWriter().GetReader());

        Assert.NotNull(table.HorizontalAxis);
        Assert.NotNull(table.VerticalAxis);
        Assert.Equal(2, table.HorizontalAxis.BaselineTags.Length);
        Assert.Equal(Tag.Parse("hang"), table.HorizontalAxis.BaselineTags[0]);
        Assert.Equal(Tag.Parse("ideo"), table.HorizontalAxis.BaselineTags[1]);
        Assert.Equal(1, table.HorizontalAxis.Scripts[0].Values!.DefaultBaselineIndex);

        // Horizontal axis coordinates use BaseCoord format 1.
        Assert.True(table.TryGetBaselineCoordinate(Tag.Parse("hang"), false, out short coordinate));
        Assert.Equal(1638, coordinate);
        Assert.True(table.TryGetBaselineCoordinate(Tag.Parse("ideo"), false, out coordinate));
        Assert.Equal(-288, coordinate);

        // Vertical axis coordinates use BaseCoord formats 2 and 3, whose design unit
        // coordinate occupies the same leading position.
        Assert.True(table.TryGetBaselineCoordinate(Tag.Parse("hang"), true, out coordinate));
        Assert.Equal(1900, coordinate);
        Assert.True(table.TryGetBaselineCoordinate(Tag.Parse("ideo"), true, out coordinate));
        Assert.Equal(100, coordinate);

        // A baseline missing from the tag list reports false.
        Assert.False(table.TryGetBaselineCoordinate(Tag.Parse("romn"), false, out _));
        Assert.False(table.TryGetBaselineCoordinate(Tag.Parse("romn"), true, out _));
    }

    [Fact]
    public void TryGetBaselineCoordinate_ReturnsFalseWhenAxisMissing()
    {
        BigEndianBinaryWriter writer = new();

        // Header referencing only a horizontal axis.
        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteOffset16(8);
        writer.WriteOffset16(0);
        WriteSingleScriptAxis(writer, 1638, -288);

        BaseTable table = BaseTable.Load(writer.GetReader());

        Assert.Null(table.VerticalAxis);
        Assert.False(table.TryGetBaselineCoordinate(Tag.Parse("hang"), true, out _));
        Assert.True(table.TryGetBaselineCoordinate(Tag.Parse("hang"), false, out short coordinate));
        Assert.Equal(1638, coordinate);
    }

    [Fact]
    public void TryGetBaselineCoordinate_ReturnsFalseWhenTagListMissing()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteOffset16(8);
        writer.WriteOffset16(0);

        // Axis at 8 with a NULL BaseTagList.
        writer.WriteOffset16(0);
        writer.WriteOffset16(4);

        // BaseScriptList at 12 with a single 'DFLT' record at 8 from list start.
        writer.WriteUInt16(1);
        writer.WriteUInt32("DFLT");
        writer.WriteOffset16(8);

        // BaseScript at 20 with values at 6 from script start.
        writer.WriteOffset16(6);
        writer.WriteOffset16(0);
        writer.WriteUInt16(0);

        // BaseValues at 26 with no coordinates.
        writer.WriteUInt16(0);
        writer.WriteUInt16(0);

        BaseTable table = BaseTable.Load(writer.GetReader());

        Assert.False(table.TryGetBaselineCoordinate(Tag.Parse("hang"), false, out _));
    }

    [Fact]
    public void TryGetBaselineCoordinate_ReturnsFalseWhenScriptDefinesNoValues()
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteOffset16(8);
        writer.WriteOffset16(0);

        // Axis at 8.
        writer.WriteOffset16(4);
        writer.WriteOffset16(14);

        // BaseTagList at 12.
        writer.WriteUInt16(2);
        writer.WriteUInt32("hang");
        writer.WriteUInt32("ideo");

        // BaseScriptList at 22 with a single 'DFLT' record at 8 from list start.
        writer.WriteUInt16(1);
        writer.WriteUInt32("DFLT");
        writer.WriteOffset16(8);

        // BaseScript at 30 with a NULL BaseValues offset.
        writer.WriteOffset16(0);
        writer.WriteOffset16(0);
        writer.WriteUInt16(0);

        BaseTable table = BaseTable.Load(writer.GetReader());

        Assert.False(table.TryGetBaselineCoordinate(Tag.Parse("hang"), false, out _));
    }

    [Fact]
    public void TryGetBaselineCoordinate_PrefersDefaultScriptRecord()
    {
        BaseTable table = BaseTable.Load(CreateTwoScriptWriter(true).GetReader());

        Assert.True(table.TryGetBaselineCoordinate(Tag.Parse("ideo"), false, out short coordinate));
        Assert.Equal(222, coordinate);
    }

    [Fact]
    public void TryGetBaselineCoordinate_FallsBackToFirstScriptWithValues()
    {
        BaseTable table = BaseTable.Load(CreateTwoScriptWriter(false).GetReader());

        Assert.True(table.TryGetBaselineCoordinate(Tag.Parse("ideo"), false, out short coordinate));
        Assert.Equal(222, coordinate);
    }

    [Fact]
    public void ShouldReturnNullWhenTableCouldNotBeFound()
    {
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader();

        using MemoryStream stream = writer.GetStream();
        using FontReader reader = new(stream);

        Assert.Null(BaseTable.Load(reader));
    }

    [Fact]
    public void GetBaselineOffset_UsesBaselineTableCoordinates()
    {
        Font font = CreateFontWithBaseTable();
        FontMetrics metrics = font.FontMetrics;
        float scale = font.Size / metrics.ScaleFactor;

        // Horizontal coordinates are Y values measured up from the alphabetic baseline.
        Assert.Equal(-(1638 * scale), TextLayout.GetBaselineOffset(TextBaseline.Hanging, font, false));
        Assert.Equal(-(-288 * scale), TextLayout.GetBaselineOffset(TextBaseline.Ideographic, font, false));

        // Vertical coordinates are X values measured from the em box leading edge, re-centered
        // on the central column axis; X increases toward the over side.
        Assert.Equal((1900 - (metrics.UnitsPerEm * .5F)) * scale, TextLayout.GetBaselineOffset(TextBaseline.Hanging, font, true));
        Assert.Equal((100 - (metrics.UnitsPerEm * .5F)) * scale, TextLayout.GetBaselineOffset(TextBaseline.Ideographic, font, true));
    }

    [Fact]
    public void GetBaselineOffset_BaselineTableLeavesMetricBaselinesUntouched()
    {
        Font tabled = CreateFontWithBaseTable();
        Font plain = TestFonts.GetFont(TestFonts.OpenSansFile, 72);

        Assert.Equal(
            TextLayout.GetBaselineOffset(TextBaseline.TextBottom, plain, false),
            TextLayout.GetBaselineOffset(TextBaseline.TextBottom, tabled, false));
        Assert.Equal(
            TextLayout.GetBaselineOffset(TextBaseline.TextTop, plain, true),
            TextLayout.GetBaselineOffset(TextBaseline.TextTop, tabled, true));

        // The plain font resolves the same baselines from metric fallbacks instead.
        Assert.NotEqual(
            TextLayout.GetBaselineOffset(TextBaseline.Hanging, plain, false),
            TextLayout.GetBaselineOffset(TextBaseline.Hanging, tabled, false));
    }

    private static BigEndianBinaryWriter CreateFullTableWriter()
    {
        BigEndianBinaryWriter writer = new();

        // Header with the horizontal axis at 8 and the vertical axis at 52.
        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteOffset16(8);
        writer.WriteOffset16(52);

        WriteSingleScriptAxis(writer, 1638, -288);

        // Vertical axis at 52; layout mirrors the horizontal axis with wider coords.
        writer.WriteOffset16(4);
        writer.WriteOffset16(14);

        // BaseTagList at axis + 4.
        writer.WriteUInt16(2);
        writer.WriteUInt32("hang");
        writer.WriteUInt32("ideo");

        // BaseScriptList at axis + 14 with a single 'DFLT' record at 8 from list start.
        writer.WriteUInt16(1);
        writer.WriteUInt32("DFLT");
        writer.WriteOffset16(8);

        // BaseScript at axis + 22 with values at 6 from script start.
        writer.WriteOffset16(6);
        writer.WriteOffset16(0);
        writer.WriteUInt16(0);

        // BaseValues at axis + 28; coords at 8 and 16 from values start.
        writer.WriteUInt16(1);
        writer.WriteUInt16(2);
        writer.WriteOffset16(8);
        writer.WriteOffset16(16);

        // BaseCoord format 2 with a reference glyph and contour point.
        writer.WriteUInt16(2);
        writer.Write((short)1900);
        writer.WriteUInt16(42);
        writer.WriteUInt16(7);

        // BaseCoord format 3 with a NULL device offset.
        writer.WriteUInt16(3);
        writer.Write((short)100);
        writer.WriteOffset16(0);

        return writer;
    }

    private static void WriteSingleScriptAxis(BigEndianBinaryWriter writer, short hangCoordinate, short ideoCoordinate)
    {
        // Axis table.
        writer.WriteOffset16(4);
        writer.WriteOffset16(14);

        // BaseTagList at axis + 4.
        writer.WriteUInt16(2);
        writer.WriteUInt32("hang");
        writer.WriteUInt32("ideo");

        // BaseScriptList at axis + 14 with a single 'DFLT' record at 8 from list start.
        writer.WriteUInt16(1);
        writer.WriteUInt32("DFLT");
        writer.WriteOffset16(8);

        // BaseScript at axis + 22 with values at 6 from script start.
        writer.WriteOffset16(6);
        writer.WriteOffset16(0);
        writer.WriteUInt16(0);

        // BaseValues at axis + 28; format 1 coords at 8 and 12 from values start.
        writer.WriteUInt16(1);
        writer.WriteUInt16(2);
        writer.WriteOffset16(8);
        writer.WriteOffset16(12);

        writer.WriteUInt16(1);
        writer.Write(hangCoordinate);

        writer.WriteUInt16(1);
        writer.Write(ideoCoordinate);
    }

    private static BigEndianBinaryWriter CreateTwoScriptWriter(bool secondScriptIsDefault)
    {
        BigEndianBinaryWriter writer = new();

        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteOffset16(8);
        writer.WriteOffset16(0);

        // Axis at 8.
        writer.WriteOffset16(4);
        writer.WriteOffset16(10);

        // BaseTagList at 12 with the single 'ideo' tag.
        writer.WriteUInt16(1);
        writer.WriteUInt32("ideo");

        // BaseScriptList at 18 with records at 14 and 20 from list start.
        writer.WriteUInt16(2);
        writer.WriteUInt32("BNG ");
        writer.WriteOffset16(14);
        writer.WriteUInt32(secondScriptIsDefault ? "DFLT" : "latn");
        writer.WriteOffset16(20);

        // First BaseScript at 32. When testing default script preference it carries a
        // decoy value; when testing first-with-values fallback it carries none.
        if (secondScriptIsDefault)
        {
            writer.WriteOffset16(12);
        }
        else
        {
            writer.WriteOffset16(0);
        }

        writer.WriteOffset16(0);
        writer.WriteUInt16(0);

        // Second BaseScript at 38 with values at 16 from script start.
        writer.WriteOffset16(16);
        writer.WriteOffset16(0);
        writer.WriteUInt16(0);

        // First script BaseValues at 44 with its format 1 coord at 6 from values start.
        writer.WriteUInt16(0);
        writer.WriteUInt16(1);
        writer.WriteOffset16(6);
        writer.WriteUInt16(1);
        writer.Write((short)111);

        // Second script BaseValues at 54 with its format 1 coord at 6 from values start.
        writer.WriteUInt16(0);
        writer.WriteUInt16(1);
        writer.WriteOffset16(6);
        writer.WriteUInt16(1);
        writer.Write((short)222);

        return writer;
    }

    private static Font CreateFontWithBaseTable()
    {
        using MemoryStream baseStream = CreateFullTableWriter().GetStream();
        byte[] baseTable = baseStream.ToArray();
        byte[] source = File.ReadAllBytes(TestFonts.OpenSansFile);

        // An OpenType file starts with the offset table:
        //   uint32 sfntVersion, uint16 numTables, uint16 searchRange,
        //   uint16 entrySelector, uint16 rangeShift
        // followed by numTables directory entries of 16 bytes each:
        //   uint32 tag, uint32 checksum, uint32 offset, uint32 length
        // where offset is measured from the start of the file. Table data follows the
        // directory, so inserting one directory entry shifts every table by 16 bytes.
        ushort numTables = BinaryPrimitives.ReadUInt16BigEndian(source.AsSpan(4));
        int directoryEnd = 12 + (numTables * 16);

        // Rebuild the font with one extra directory entry, shifting every table by the
        // inserted entry size and appending the BASE data at the end.
        byte[] result = new byte[source.Length + 16 + baseTable.Length];

        // Copy the 12 byte offset table header and bump the table count.
        source.AsSpan(0, 12).CopyTo(result);
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(4), (ushort)(numTables + 1));

        // Copy each original directory entry, adjusting its data offset for the 16 bytes
        // the new entry inserts ahead of every table. searchRange, entrySelector, and
        // rangeShift in the header describe an optional binary search layout the reader
        // never consults, so they can stay stale.
        for (int i = 0; i < numTables; i++)
        {
            int entry = 12 + (i * 16);
            source.AsSpan(entry, 16).CopyTo(result.AsSpan(entry));
            uint offset = BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(entry + 8));
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(entry + 8), offset + 16);
        }

        // Append the new directory entry after the originals. 0x42415345 is the table tag
        // 'BASE' as big endian ASCII (0x42 'B', 0x41 'A', 0x53 'S', 0x45 'E'). The checksum
        // is left zero because the reader never validates checksums, and the table data is
        // appended at what was the end of the file, now 16 bytes further along.
        int newEntry = directoryEnd;
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(newEntry), 0x42415345);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(newEntry + 4), 0);
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(newEntry + 8), (uint)(source.Length + 16));
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(newEntry + 12), (uint)baseTable.Length);

        // Copy all original table data verbatim, then the new BASE table at the end.
        source.AsSpan(directoryEnd).CopyTo(result.AsSpan(directoryEnd + 16));
        baseTable.CopyTo(result.AsSpan(source.Length + 16));

        using MemoryStream fontStream = new(result);
        return new FontCollection().Add(fontStream).CreateFont(72);
    }
}
