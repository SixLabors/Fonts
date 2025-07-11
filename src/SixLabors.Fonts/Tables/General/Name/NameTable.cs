// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.Name;

internal class NameTable : Table
{
    internal const string TableName = "name";
    private readonly NameRecord[] names;

    internal NameTable(NameRecord[] names, string[] languages)
        => this.names = names;

    /// <summary>
    /// Gets the name of the font.
    /// </summary>
    /// <value>
    /// The name of the font.
    /// </value>
    public string Id(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.UniqueFontID);

    /// <summary>
    /// Gets the name of the font.
    /// </summary>
    /// <value>
    /// The name of the font.
    /// </value>
    public string FontName(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.FullFontName);

    /// <summary>
    /// Gets the name of the font.
    /// </summary>
    /// <value>
    /// The name of the font.
    /// </value>
    public string FontFamilyName(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.FontFamilyName);

    /// <summary>
    /// Gets the name of the font.
    /// </summary>
    /// <value>
    /// The name of the font.
    /// </value>
    public string FontSubFamilyName(CultureInfo culture)
        => this.GetNameById(culture, KnownNameIds.FontSubfamilyName);

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

    public string GetNameById(CultureInfo culture, ushort nameId)
        => this.GetNameById(culture, (KnownNameIds)nameId);

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

    public static NameTable Load(BigEndianBinaryReader reader)
    {
        List<StringLoader> strings = new();
        ushort format = reader.ReadUInt16();
        ushort nameCount = reader.ReadUInt16();
        ushort stringOffset = reader.ReadUInt16();

        NameRecord[] names = new NameRecord[nameCount];

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
