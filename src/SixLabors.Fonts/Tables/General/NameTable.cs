// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class NameTable : Table
    {
        private const string TableName = "name";
        private readonly NameRecord[] names;

        internal NameTable(NameRecord[] names, string[] languages)
        {
            this.names = names;
        }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string Id(CultureInfo culture)
            => this.GetNameById(culture, NameIds.UniqueFontID);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontName(CultureInfo culture)
            => this.GetNameById(culture, NameIds.FullFontName);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontFamilyName(CultureInfo culture)
            => this.GetNameById(culture, NameIds.FontFamilyName);

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <value>
        /// The name of the font.
        /// </value>
        public string FontSubFamilyName(CultureInfo culture)
            => this.GetNameById(culture, NameIds.FontSubfamilyName);

        public string GetNameById(CultureInfo culture, NameIds nameId)
        {
#if SUPPORTS_CULTUREINFO_LCID
            var languageId = culture.LCID;
#else
            var languageId = 0x0409;
#endif
            NameRecord? usaVersion = null;
            NameRecord? firstWindows = null;
            NameRecord? first = null;
            foreach (NameRecord name in this.names)
            {
                if (name.NameID == nameId)
                {
                    // get just the first one, just incase.
                    first ??= name;
                    if (name.Platform == PlatformIDs.Windows)
                    {
                        // if us not found return the first windows one
                        firstWindows ??= name;
                        if (name.LanguageID == 0x0409)
                        {
                            // grab the us version as its on next best match
                            usaVersion ??= name;
                        }

                        if (name.LanguageID == languageId)
                        {
                            // return the most exact first
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
        {
            return this.GetNameById(culture, (NameIds)nameId);
        }

        public static NameTable Load(FontReader reader)
        {
            using (BinaryReader r = reader.GetReaderAtTablePosition(TableName))
            {
                // move to start of table
                return Load(r);
            }
        }

        public static NameTable Load(BinaryReader reader)
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
                if (sr is object)
                {
                    strings.Add(sr);
                }
            }

            var langs = new StringLoader[0];
            if (format == 1)
            {
                // format 1 adds language data
                ushort langCount = reader.ReadUInt16();
                langs = new StringLoader[langCount];

                for (int i = 0; i < langCount; i++)
                {
                    langs[i] = StringLoader.Create(reader);
                    strings.Add(langs[i]);
                }
            }

            foreach (StringLoader readable in strings.OrderBy(x => x.Offset))
            {
                int diff = stringOffset + readable.Offset;

                // only seek forward, if we find issues with this we will consume forwards as the idea is we will never need to backtrack
                reader.Seek(diff, SeekOrigin.Begin);

                readable.LoadValue(reader);
            }

            string[] langNames = langs?.Select(x => x.Value).ToArray() ?? new string[0];

            return new NameTable(names, langNames);
        }
    }
}
