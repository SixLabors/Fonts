// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Builds fonts whose glyph for 'A' is drawn from an SVG document, so tests can exercise the SVG glyph pipeline
/// with documents written inline.
/// </summary>
internal static class SvgTestFonts
{
    /// <summary>
    /// Creates a font whose glyph for 'A' is drawn from an SVG document.
    /// </summary>
    /// <param name="glyphBody">The SVG markup to place inside the glyph's <c>glyph{gid}</c> group element.</param>
    /// <param name="defs">Optional SVG markup to place in the document outside the glyph's group element.</param>
    /// <param name="size">The font size.</param>
    /// <returns>The font.</returns>
    public static Font CreateFont(string glyphBody, string defs = "", float size = 100)
    {
        byte[] source = File.ReadAllBytes(TestFonts.SimpleFontFile);
        Font plain = new FontCollection().Add(new MemoryStream(source)).CreateFont(size);
        Assert.True(plain.TryGetGlyphId(new CodePoint('A'), out ushort glyphId));

        string svg = string.Create(
            CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\">{defs}<g id=\"glyph{glyphId}\">{glyphBody}</g></svg>");

        return new FontCollection().Add(AddSvgTable(source, glyphId, svg)).CreateFont(size);
    }

    /// <summary>
    /// Returns a copy of a TrueType font with an 'SVG ' table holding one document for one glyph.
    /// </summary>
    /// <param name="font">The source font, which must not already contain an 'SVG ' table.</param>
    /// <param name="glyphId">The glyph the document applies to.</param>
    /// <param name="svgDocument">The SVG document.</param>
    /// <returns>A stream over the new font.</returns>
    private static MemoryStream AddSvgTable(byte[] font, ushort glyphId, string svgDocument)
    {
        // Lift the existing tables out of the source font's table directory.
        int numTables = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4));
        List<(string Tag, byte[] Data)> tables = [];
        for (int i = 0; i < numTables; i++)
        {
            ReadOnlySpan<byte> record = font.AsSpan(12 + (16 * i), 16);
            string tag = Encoding.ASCII.GetString(record[..4]);
            int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(record[12..]);
            tables.Add((tag, font.AsSpan(offset, length).ToArray()));
        }

        // SVG table: the header, a document index with a single entry, then the document.
        byte[] document = Encoding.UTF8.GetBytes(svgDocument);
        BigEndianBinaryWriter svg = new();
        svg.WriteUInt16(0); // version
        svg.WriteUInt32(10); // svgDocumentListOffset
        svg.WriteUInt32(0); // reserved
        svg.WriteUInt16(1); // numEntries
        svg.WriteUInt16(glyphId); // startGlyphID
        svg.WriteUInt16(glyphId); // endGlyphID
        svg.WriteUInt32(14); // svgDocOffset, from the start of the document index
        svg.WriteUInt32((uint)document.Length); // svgDocLength
        svg.Write(document);

        tables.Add(("SVG ", svg.GetStream().ToArray()));
        tables.Sort((x, y) => string.CompareOrdinal(x.Tag, y.Tag));

        // The header lays the tables out back to back in directory order.
        BigEndianBinaryWriter writer = new();
        writer.WriteTrueTypeFileHeader([.. tables.Select(t => new TableHeader(t.Tag, 0, 0, (uint)t.Data.Length))]);
        foreach ((_, byte[] data) in tables)
        {
            writer.Write(data);
        }

        return writer.GetStream();
    }
}
