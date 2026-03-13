// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.Name;

/// <summary>
/// Represents the OpenType 'name' table, which contains human-readable string names
/// for features, font metadata, and other descriptive information.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/name"/>
/// </summary>
internal class NameTable : Table
{
    /// <summary>
    /// The table tag name identifying the 'name' table.
    /// </summary>
    internal const string TableName = "name";

    /// <summary>
    /// The array of name records contained in this table.
    /// </summary>
    private readonly NameRecord[] names;

    /// <summary>
    /// Initializes a new instance of the <see cref="NameTable"/> class.
    /// </summary>
    /// <param name="names">The name records.</param>
    /// <param name="languages">The language tag strings (format 1 only).</param>
    internal NameTable(NameRecord[] names, string[] languages)
        => this.names = names;

    /// <summary>
    /// Gets the unique font identifier for the specified culture.
    /// </summary>
    /// <param name="culture">The culture used to select the appropriate language-specific name.</param>
    /// <returns>The unique font identifier string.</returns>
    public string Id(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.UniqueFontID);

    /// <summary>
    /// Gets the full font name for the specified culture.
    /// </summary>
    /// <param name="culture">The culture used to select the appropriate language-specific name.</param>
    /// <returns>The full font name string.</returns>
    public string FontName(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.FullFontName);

    /// <summary>
    /// Gets the font family name for the specified culture.
    /// </summary>
    /// <param name="culture">The culture used to select the appropriate language-specific name.</param>
    /// <returns>The font family name string.</returns>
    public string FontFamilyName(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.FontFamilyName);

    /// <summary>
    /// Gets the font subfamily name (e.g., "Bold", "Italic") for the specified culture.
    /// </summary>
    /// <param name="culture">The culture used to select the appropriate language-specific name.</param>
    /// <returns>The font subfamily name string.</returns>
    public string FontSubFamilyName(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.FontSubfamilyName);

    /// <summary>
    /// Gets the name string for the specified culture and name identifier.
    /// Falls back to US English (0x0409), then the first Windows platform record, then any record.
    /// </summary>
    /// <param name="culture">The culture used to select the appropriate language-specific name.</param>
    /// <param name="nameId">The name identifier to look up.</param>
    /// <returns>The name string, or <see cref="string.Empty"/> if not found.</returns>
    public string GetNameById(CultureInfo culture, KnownNameIds nameId)
    {
        int languageId = culture.LCID;
        NameRecord? usaVersion = null;
        NameRecord? firstWindows = null;
        NameRecord? first = null;
        foreach (NameRecord name in this.names)
        {
            if (name.NameID == nameId)
            {
                // Get just the first one, just in case.
                first ??= name;
                if (name.Platform == PlatformIDs.Windows)
                {
                    // If us not found return the first windows one.
                    firstWindows ??= name;
                    if (name.LanguageID == 0x0409)
                    {
                        // Grab the us version as its on next best match.
                        usaVersion ??= name;
                    }

                    if (name.LanguageID == languageId)
                    {
                        // Return the most exact first.
                        return name.Value;
                    }
                }
            }
        }

        return usaVersion?.Value ??
               firstWindows?.Value ??
               first?.Value ??
               string.Empty;
    }

    /// <summary>
    /// Gets the name string for the specified culture and raw name identifier.
    /// </summary>
    /// <param name="culture">The culture used to select the appropriate language-specific name.</param>
    /// <param name="nameId">The raw name identifier value to look up.</param>
    /// <returns>The name string, or <see cref="string.Empty"/> if not found.</returns>
    public string GetNameById(CultureInfo culture, ushort nameId)
        => this.GetNameById(culture, (KnownNameIds)nameId);

    /// <summary>
    /// Loads the <see cref="NameTable"/> from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader to read the table from.</param>
    /// <returns>The loaded <see cref="NameTable"/>.</returns>
    /// <exception cref="InvalidFontTableException">Thrown when the table is missing from the font.</exception>
    public static NameTable? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            throw new InvalidFontTableException($"Table '{TableName}' is missing", TableName);
        }

        using (binaryReader)
        {
            // Move to start of table.
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Loads the <see cref="NameTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the start of the name table data.</param>
    /// <returns>The loaded <see cref="NameTable"/>.</returns>
    public static NameTable Load(BigEndianBinaryReader reader)
    {
        var strings = new List<StringLoader>();
        ushort format = reader.ReadUInt16();
        ushort nameCount = reader.ReadUInt16();
        ushort stringOffset = reader.ReadUInt16();

        var names = new NameRecord[nameCount];

        for (int i = 0; i < nameCount; i++)
        {
            names[i] = NameRecord.Read(reader);
            StringLoader? sr = names[i].StringReader;
            if (sr is not null)
            {
                strings.Add(sr);
            }
        }

        StringLoader[]? langs = Array.Empty<StringLoader>();
        if (format == 1)
        {
            // Format 1 adds language data.
            ushort langCount = reader.ReadUInt16();
            langs = new StringLoader[langCount];

            for (int i = 0; i < langCount; i++)
            {
                langs[i] = StringLoader.Create(reader);
                strings.Add(langs[i]);
            }
        }

        foreach (StringLoader readable in strings)
        {
            int readableStartOffset = stringOffset + readable.Offset;

            reader.Seek(readableStartOffset, SeekOrigin.Begin);

            readable.LoadValue(reader);
        }

        string[] langNames = langs?.Select(x => x.Value).ToArray() ?? Array.Empty<string>();

        return new NameTable(names, langNames);
    }
}
