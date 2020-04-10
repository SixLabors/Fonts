// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts
{
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    public class FontDescription
    {
        private NameTable nameTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontDescription" /> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="os2">The os2.</param>
        /// <param name="head">The head.</param>
        internal FontDescription(NameTable nameTable, OS2Table? os2, HeadTable? head)
        {
            this.nameTable = nameTable;
            this.Style = ConvertStyle(os2, head);

            this.FontNameInvariantCulture = this.FontName(CultureInfo.InvariantCulture);
            this.FontFamilyInvariantCulture = this.FontFamily(CultureInfo.InvariantCulture);
            this.FontSubFamilyNameInvariantCulture = this.FontSubFamilyName(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>
        /// The style.
        /// </value>
        public FontStyle Style { get; }

        /// <summary>
        /// Gets the name of the font in the invariant culture.
        /// </summary>
        /// <returns>The font name</returns>
        public string FontNameInvariantCulture { get; }

        /// <summary>
        /// Gets the name of the font family in the invariant culture.
        /// </summary>
        /// <returns>The font name</returns>
        public string FontFamilyInvariantCulture { get; }

        /// <summary>
        /// Gets the font sub family in the invariant culture.
        /// </summary>
        /// <returns>The font sub family name</returns>
        public string FontSubFamilyNameInvariantCulture { get; }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        /// <param name="culture">The culture to load metadata in.</param>
        /// <returns>The font name</returns>
        public string FontName(CultureInfo culture) => this.nameTable.FontName(culture);

        /// <summary>
        /// Gets the name of the font family .
        /// </summary>
        /// <param name="culture">The culture to load metadata in.</param>
        /// <returns>The font family name</returns>
        public string FontFamily(CultureInfo culture) => this.nameTable.FontFamilyName(culture);

        /// <summary>
        /// Gets the font sub family.
        /// </summary>
        /// <param name="culture">The culture to load metadata in.</param>
        /// <returns>The font sub family name</returns>
        public string FontSubFamilyName(CultureInfo culture) => this.nameTable.FontSubFamilyName(culture);

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription LoadDescription(string path)
        {
            Guard.NotNullOrWhiteSpace(path, nameof(path));

            using (FileStream fs = File.OpenRead(path))
            {
                var reader = new FontReader(fs);
                return LoadDescription(reader);
            }
        }

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription LoadDescription(Stream stream)
        {
            Guard.NotNull(stream, nameof(stream));

            // only read the name table
            var reader = new FontReader(stream);
            return LoadDescription(reader);
        }

        /// <summary>
        /// Reads a <see cref="FontDescription" /> from the specified stream.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// a <see cref="FontDescription" />.
        /// </returns>
        internal static FontDescription LoadDescription(FontReader reader)
        {
            DebugGuard.NotNull(reader, nameof(reader));

            // NOTE: These fields are read in their optimized order
            // https://docs.microsoft.com/en-gb/typography/opentype/spec/recom#optimized-table-ordering
            HeadTable? head = reader.TryGetTable<HeadTable>();
            OS2Table? os2 = reader.TryGetTable<OS2Table>();
            NameTable nameTable = reader.GetTable<NameTable>();

            return new FontDescription(nameTable, os2, head);
        }

        /// <summary>
        /// Reads all the <see cref="FontDescription"/>s from the file at the specified path (typically a .ttc file like simsun.ttc).
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription[] LoadFontCollectionDescriptions(string path)
        {
            Guard.NotNullOrWhiteSpace(path, nameof(path));

            using (FileStream fs = File.OpenRead(path))
            {
                return LoadFontCollectionDescriptions(fs);
            }
        }

        /// <summary>
        /// Reads all the <see cref="FontDescription"/>s from the specified stream (typically a .ttc file like simsun.ttc).
        /// </summary>
        /// <param name="stream">The stream to read the font collection from.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription[] LoadFontCollectionDescriptions(Stream stream)
        {
            long startPos = stream.Position;
            var reader = new BinaryReader(stream, true);
            var ttcHeader = TtcHeader.Read(reader);

            var result = new FontDescription[(int)ttcHeader.NumFonts];
            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                stream.Position = startPos + ttcHeader.OffsetTable[i];
                var fontReader = new FontReader(stream);
                result[i] = LoadDescription(fontReader);
            }

            return result;
        }

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
