// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts
{
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    public class FontDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontDescription" /> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="os2">The os2.</param>
        /// <param name="head">The head.</param>
        /// <param name="culture">The culture to load metadata in.</param>
        internal FontDescription(NameTable nameTable, OS2Table? os2, HeadTable? head, CultureInfo culture)
        {
            this.FontName = nameTable.FontName(culture);
            this.FontFamily = nameTable.FontFamilyName(culture);
            this.FontSubFamilyName = nameTable.FontSubFamilyName(culture);
            this.Style = ConvertStyle(os2, head);
            this.FontNames = nameTable.GetNameRecordsById(NameIds.FullFontName);
            this.FontFamilyNames = nameTable.GetNameRecordsById(NameIds.FontFamilyName);
            this.FontSubFamilyNames = nameTable.GetNameRecordsById(NameIds.FontSubfamilyName);
        }

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>
        /// The style.
        /// </value>
        public FontStyle Style { get; }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        public string FontFamily { get; }

        /// <summary>
        /// Gets the font sub family.
        /// </summary>
        public string FontSubFamilyName { get; }

        /// <summary>
        /// Gets the font names in every platform and language.
        /// </summary>
        internal IEnumerable<NameRecord> FontNames { get; }

        /// <summary>
        /// Gets the font family names in every platform and language.
        /// </summary>
        internal IEnumerable<NameRecord> FontFamilyNames { get; }

        /// <summary>
        /// Gets the font sub family names in every platform and language.
        /// </summary>
        internal IEnumerable<NameRecord> FontSubFamilyNames { get; }

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="culture">Culture to load metadate in.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription LoadDescription(string path, CultureInfo culture)
        {
            Guard.NotNullOrWhiteSpace(path, nameof(path));

            using (FileStream fs = File.OpenRead(path))
            {
                var reader = new FontReader(fs);
                return LoadDescription(reader, culture);
            }
        }

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription LoadDescription(string path)
            => LoadDescription(path, CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="culture">Culture to load metadate in.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription LoadDescription(Stream stream, CultureInfo culture)
        {
            Guard.NotNull(stream, nameof(stream));

            // only read the name table
            var reader = new FontReader(stream);
            return LoadDescription(reader, culture);
        }

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription LoadDescription(Stream stream)
            => LoadDescription(stream, CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads a <see cref="FontDescription" /> from the specified stream.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="culture">Culture to load metadate in.</param>
        /// <returns>
        /// a <see cref="FontDescription" />.
        /// </returns>
        internal static FontDescription LoadDescription(FontReader reader, CultureInfo culture)
        {
            DebugGuard.NotNull(reader, nameof(reader));

            // NOTE: These fields are read in their optimized order
            // https://docs.microsoft.com/en-gb/typography/opentype/spec/recom#optimized-table-ordering
            HeadTable? head = reader.TryGetTable<HeadTable>();
            OS2Table? os2 = reader.TryGetTable<OS2Table>();
            NameTable nameTable = reader.GetTable<NameTable>();

            return new FontDescription(nameTable, os2, head, culture);
        }

        /// <summary>
        /// Reads a <see cref="FontDescription" /> from the specified stream.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// a <see cref="FontDescription" />.
        /// </returns>
        internal static FontDescription LoadDescription(FontReader reader)
            => LoadDescription(reader, CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads all the <see cref="FontDescription"/>s from the file at the specified path (typically a .ttc file like simsun.ttc).
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="culture">The culture to load metadata in.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription[] LoadFontCollectionDescriptions(string path, CultureInfo culture)
        {
            Guard.NotNullOrWhiteSpace(path, nameof(path));

            using (FileStream fs = File.OpenRead(path))
            {
                return LoadFontCollectionDescriptions(fs, culture);
            }
        }

        /// <summary>
        /// Reads all the <see cref="FontDescription"/>s from the file at the specified path (typically a .ttc file like simsun.ttc).
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription[] LoadFontCollectionDescriptions(string path)
            => LoadFontCollectionDescriptions(path, CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads all the <see cref="FontDescription"/>s from the specified stream (typically a .ttc file like simsun.ttc).
        /// </summary>
        /// <param name="stream">The stream to read the font collection from.</param>
        /// <param name="culture">The culture to load metadata in.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription[] LoadFontCollectionDescriptions(Stream stream, CultureInfo culture)
        {
            long startPos = stream.Position;
            var reader = new BinaryReader(stream, true);
            var ttcHeader = TtcHeader.Read(reader);

            var result = new FontDescription[(int)ttcHeader.NumFonts];
            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                stream.Position = startPos + ttcHeader.OffsetTable[i];
                var fontReader = new FontReader(stream);
                result[i] = LoadDescription(fontReader, culture);
            }

            return result;
        }

        /// <summary>
        /// Reads all the <see cref="FontDescription"/>s from the specified stream (typically a .ttc file like simsun.ttc).
        /// </summary>
        /// <param name="stream">The stream to read the font collection from.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription[] LoadFontCollectionDescriptions(Stream stream)
            => LoadFontCollectionDescriptions(stream, CultureInfo.InvariantCulture);

        private static FontStyle ConvertStyle(OS2Table? os2, HeadTable? head)
        {
            FontStyle style = FontStyle.Regular;

            if (os2 != null)
            {
                if (os2.FontStyle.HasFlag(OS2Table.FontStyleSelection.BOLD))
                {
                    style |= FontStyle.Bold;
                }

                if (os2.FontStyle.HasFlag(OS2Table.FontStyleSelection.ITALIC))
                {
                    style |= FontStyle.Italic;
                }
            }
            else if (head != null)
            {
                if (head.MacStyle.HasFlag(HeadTable.HeadMacStyle.Bold))
                {
                    style |= FontStyle.Bold;
                }

                if (head.MacStyle.HasFlag(HeadTable.HeadMacStyle.Italic))
                {
                    style |= FontStyle.Italic;
                }
            }

            return style;
        }
    }
}
