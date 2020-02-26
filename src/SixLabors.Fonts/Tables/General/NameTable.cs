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

        public IEnumerable<NameRecord> GetNameRecordsById(NameIds nameId)
        {
            return this.names.Where(record => record.NameID == nameId);
        }

        /// <summary>
        /// Get windows names from <paramref name="records"/>,
        /// in language specified by <paramref name="culture"/>, whenever possible.
        /// Returns a <see cref="string.Empty"/> if nothing can be found.
        /// </summary>
        public string GetName(IEnumerable<NameRecord> records, CultureInfo culture)
        {
            // get windows names whenever possible
            IEnumerable<NameRecord> windowsNames = records.Where(record => record.Platform == PlatformIDs.Windows);
            records = windowsNames.Count() > 0 ? windowsNames : records;

            // LCID of English (US) is 0x0409
            int usaLCID = 0x0409;
#if SUPPORTS_CULTUREINFO_LCID
            int languageId = culture.LCID;
#else
            int languageId = usaLCID;
#endif

            // get current culture names whenever possible, and then fallback to getting English (US) names
            IEnumerable<NameRecord> namesByLanguage = records.Where(record => record.LanguageID == languageId);
            records = namesByLanguage.Count() > 0 ? namesByLanguage : records.Where(record => record.LanguageID == usaLCID);

            // return the first value
            return records.Select(record => record.Value).DefaultIfEmpty(string.Empty).First();
        }

        public string GetNameById(CultureInfo culture, NameIds nameId)
        {
            return this.GetName(this.GetNameRecordsById(nameId), culture);
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
