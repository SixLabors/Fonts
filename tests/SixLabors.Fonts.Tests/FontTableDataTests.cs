// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts.Tests;

public class FontTableDataTests
{
    [Fact]
    public void FileBackedFontReturnsTableData()
    {
        FontMetrics metrics = GetMetrics(new FontCollection().Add(TestFonts.CarterOneFile));

        Assert.True(metrics.TryGetTableData(Tag.Parse("head"), out ReadOnlyMemory<byte> table));
        Assert.False(table.IsEmpty);
    }

    [Fact]
    public void FileBackedFontReturnsStream()
    {
        FontMetrics metrics = GetMetrics(new FontCollection().Add(TestFonts.CarterOneFile));

        using (Stream stream = metrics.OpenStream())
        {
            Assert.True(stream.CanRead);
            Assert.Equal(0, stream.Position);
        }
    }

    [Fact]
    public void MissingTableReturnsFalse()
    {
        FontMetrics metrics = GetMetrics(new FontCollection().Add(TestFonts.CarterOneFile));

        Assert.False(metrics.TryGetTableData(Tag.Parse("ZZZZ"), out ReadOnlyMemory<byte> table));
        Assert.True(table.IsEmpty);
    }

    [Fact]
    public void StreamBackedFontReturnsTableDataAfterSourceDisposed()
    {
        FontCollection collection = new();
        FontMetrics metrics;

        using (Stream stream = TestFonts.CarterOneFileData())
        {
            FontFamily family = collection.Add(stream);
            metrics = GetMetrics(family);
        }

        Assert.True(metrics.TryGetTableData(Tag.Parse("head"), out ReadOnlyMemory<byte> table));
        Assert.False(table.IsEmpty);
    }

    [Fact]
    public void StreamBackedFontReturnsStreamAfterSourceDisposed()
    {
        FontCollection collection = new();
        FontMetrics metrics;

        using (Stream source = TestFonts.CarterOneFileData())
        {
            FontFamily family = collection.Add(source);
            metrics = GetMetrics(family);
        }

        using (Stream stream = metrics.OpenStream())
        {
            Assert.True(stream.CanRead);
            Assert.Equal(0, stream.Position);
        }
    }

    [Fact]
    public void StreamBackedCollectionReturnsTableDataAfterSourceDisposed()
    {
        FontCollection collection = new();
        FontMetrics metrics;

        using (Stream stream = TestFonts.SSimpleTrueTypeCollectionData())
        {
            FontFamily family = Assert.Single(collection.AddCollection(stream).ToArray(), x => x.Name == "Open Sans");
            metrics = GetMetrics(family);
        }

        Assert.True(metrics.TryGetTableData(Tag.Parse("head"), out ReadOnlyMemory<byte> table));
        Assert.False(table.IsEmpty);
    }

    [Fact]
    public void WoffTableDataMatchesTrueTypeTableData()
    {
        ReadOnlyMemory<byte> trueTypeTable = GetTableData(TestFonts.OpenSansFile, "head");
        ReadOnlyMemory<byte> woffTable = GetTableData(TestFonts.OpenSansFileWoff1, "head");

        Assert.Equal(trueTypeTable.ToArray(), woffTable.ToArray());
    }

    [Fact]
    public void Woff2TableDataMatchesTrueTypeTableData()
    {
        ReadOnlyMemory<byte> trueTypeTable = GetTableData(TestFonts.OpenSansFile, "maxp");
        ReadOnlyMemory<byte> woff2Table = GetTableData(TestFonts.OpenSansFileWoff2, "maxp");

        Assert.Equal(trueTypeTable.ToArray(), woff2Table.ToArray());
    }

    private static ReadOnlyMemory<byte> GetTableData(string path, string tag)
    {
        FontMetrics metrics = GetMetrics(new FontCollection().Add(path));

        Assert.True(metrics.TryGetTableData(Tag.Parse(tag), out ReadOnlyMemory<byte> table));
        return table;
    }

    private static FontMetrics GetMetrics(FontFamily family)
    {
        Assert.True(family.TryGetMetrics(FontStyle.Regular, out FontMetrics metrics));
        return metrics;
    }
}
