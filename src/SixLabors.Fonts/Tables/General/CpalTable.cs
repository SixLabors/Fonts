// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the color palette table, which contains one or more palettes of colors
/// used by color fonts (e.g., COLR table).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cpal"/>
/// </summary>
internal class CpalTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "CPAL";

    /// <summary>
    /// The number of palette entries in each palette.
    /// </summary>
    private readonly ushort paletteEntryCount;

    /// <summary>
    /// The offsets into the palette entries array for each palette.
    /// </summary>
    private readonly ushort[] paletteOffsets;

    /// <summary>
    /// The combined array of color records for all palettes.
    /// </summary>
    private readonly GlyphColor[] paletteEntries;

    /// <summary>
    /// The effective colors of the default palette, built on first use and shared by every
    /// glyph source that renders with the default selection. The unsynchronized build race
    /// is benign: both threads produce identical content.
    /// </summary>
    private GlyphColor[]? defaultPaletteColors;

    /// <summary>
    /// The effective colors per custom palette selection, created on the first custom
    /// selection. Keyed by the value equality of <see cref="FontPalette"/> so each selection
    /// in use materializes once per font; a lost creation race rebuilds into the surviving
    /// dictionary.
    /// </summary>
    private ConcurrentDictionary<FontPalette, GlyphColor[]>? selectedPaletteColors;

    /// <summary>
    /// Initializes a new instance of the <see cref="CpalTable"/> class.
    /// </summary>
    /// <param name="paletteEntryCount">The number of palette entries in each palette.</param>
    /// <param name="paletteOffsets">The index of each palette's first color record.</param>
    /// <param name="paletteEntries">The combined color records for all palettes.</param>
    public CpalTable(ushort paletteEntryCount, ushort[] paletteOffsets, GlyphColor[] paletteEntries)
    {
        this.paletteEntryCount = paletteEntryCount;
        this.paletteEntries = paletteEntries;
        this.paletteOffsets = paletteOffsets;
    }

    /// <summary>
    /// Gets the glyph color at the specified palette and entry indices.
    /// </summary>
    /// <param name="paletteIndex">The zero-based palette index.</param>
    /// <param name="paletteEntryIndex">The zero-based entry index within the palette.</param>
    /// <returns>The <see cref="GlyphColor"/>.</returns>
    public GlyphColor GetGlyphColor(int paletteIndex, int paletteEntryIndex)
        => this.paletteEntries[this.paletteOffsets[paletteIndex] + paletteEntryIndex];

    /// <summary>
    /// Gets the effective color array for the given palette selection: the colors of the
    /// selected palette with the selection's overrides applied in order.
    /// A palette index outside the range defined by the font selects the default palette (index 0)
    /// and overrides still apply, matching the CSS <c>font-palette</c> behavior. Overrides whose
    /// entry index lies outside the palette entry range are ignored.
    /// The returned array is cached per distinct selection and shared across every glyph
    /// source in the font; callers must treat it as read-only.
    /// </summary>
    /// <param name="palette">The palette selection, or <see langword="null"/> for the default palette.</param>
    /// <returns>The effective palette colors.</returns>
    public GlyphColor[] GetPaletteColors(FontPalette? palette)
    {
        // The common case shares one array per font: no selection, and the explicit default
        // selection, both resolve to palette 0 with no overrides.
        if (palette is null || (palette.Index == 0 && palette.Overrides.Count == 0))
        {
            return this.defaultPaletteColors ??= this.BuildPaletteColors(null);
        }

        ConcurrentDictionary<FontPalette, GlyphColor[]> cache = this.selectedPaletteColors ??= new ConcurrentDictionary<FontPalette, GlyphColor[]>();
        return cache.GetOrAdd(palette, static (key, table) => table.BuildPaletteColors(key), this);
    }

    /// <summary>
    /// Builds the effective color array for the given palette selection. Runs once per
    /// distinct selection; <see cref="GetPaletteColors"/> caches and shares the result.
    /// </summary>
    /// <param name="palette">The palette selection, or <see langword="null"/> for the default palette.</param>
    /// <returns>The effective palette colors.</returns>
    private GlyphColor[] BuildPaletteColors(FontPalette? palette)
    {
        int paletteIndex = 0;
        if (palette is not null && palette.Index < this.paletteOffsets.Length)
        {
            paletteIndex = palette.Index;
        }

        GlyphColor[] colors = new GlyphColor[this.paletteEntryCount];
        Array.Copy(this.paletteEntries, this.paletteOffsets[paletteIndex], colors, 0, colors.Length);

        if (palette is not null)
        {
            IReadOnlyList<FontPaletteOverride> overrides = palette.Overrides;
            for (int i = 0; i < overrides.Count; i++)
            {
                FontPaletteOverride paletteOverride = overrides[i];
                if (paletteOverride.Index < colors.Length)
                {
                    colors[paletteOverride.Index] = paletteOverride.Color;
                }
            }
        }

        return colors;
    }

    /// <summary>
    /// Loads the <see cref="CpalTable"/> from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The <see cref="CpalTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static CpalTable? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the <see cref="CpalTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="CpalTable"/>.</returns>
    public static CpalTable Load(BigEndianBinaryReader reader)
    {
        // FORMAT 0

        // Type      | Name                            | Description
        // ----------|---------------------------------|----------------------------------------------------------------------------------------------------
        // uint16    | version                         | Table version number (=0).
        // uint16    | numPaletteEntries               | Number of palette entries in each palette.
        // uint16    | numPalettes                     | Number of palettes in the table.
        // uint16    | numColorRecords                 | Total number of color records, combined for all palettes.
        // Offset32  | offsetFirstColorRecord          | Offset from the beginning of CPAL table to the first ColorRecord.
        // uint16    | colorRecordIndices[numPalettes] | Index of each palette’s first color record in the combined color record array.

        // additional format 1 fields
        // Offset32  | offsetPaletteTypeArray          | Offset from the beginning of CPAL table to the Palette Type Array. Set to 0 if no array is provided.
        // Offset32  | offsetPaletteLabelArray         | Offset from the beginning of CPAL table to the Palette Labels Array. Set to 0 if no array is provided.
        // Offset32  | offsetPaletteEntryLabelArray    | Offset from the beginning of CPAL table to the Palette Entry Label Array.Set to 0 if no array is provided.
        ushort version = reader.ReadUInt16();
        ushort numPaletteEntries = reader.ReadUInt16();
        ushort numPalettes = reader.ReadUInt16();
        ushort numColorRecords = reader.ReadUInt16();
        uint offsetFirstColorRecord = reader.ReadOffset32();

        ushort[]? colorRecordIndices = reader.ReadUInt16Array(numPalettes);

        uint offsetPaletteTypeArray = 0;
        uint offsetPaletteLabelArray = 0;
        uint offsetPaletteEntryLabelArray = 0;
        if (version == 1)
        {
            offsetPaletteTypeArray = reader.ReadOffset32();
            offsetPaletteLabelArray = reader.ReadOffset32();
            offsetPaletteEntryLabelArray = reader.ReadOffset32();
        }

        reader.Seek(offsetFirstColorRecord, SeekOrigin.Begin);
        GlyphColor[] palettes = new GlyphColor[numColorRecords];
        for (int n = 0; n < numColorRecords; n++)
        {
            byte blue = reader.ReadByte();
            byte green = reader.ReadByte();
            byte red = reader.ReadByte();
            byte alpha = reader.ReadByte();
            palettes[n] = new GlyphColor(red, green, blue, alpha);
        }

        return new CpalTable(numPaletteEntries, colorRecordIndices, palettes);
    }
}
