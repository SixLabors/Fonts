// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using SixLabors.Fonts.Tables.General;

namespace SixLabors.Fonts
{
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    public class FontDescription
    {
        internal FontDescription(string fontName, string fontFamily, string fontSubFamilyName, FontStyle style)
        {
            this.FontName = fontName;
            this.FontFamily = fontFamily;
            this.FontSubFamilyName = fontSubFamilyName;
            this.Style = style;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontDescription" /> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        /// <param name="os2">The os2.</param>
        /// <param name="head">The head.</param>
        internal FontDescription(NameTable nameTable, OS2Table os2, HeadTable head)
            : this(nameTable.FontName, nameTable.FontFamilyName, nameTable.FontSubFamilyName, ConvertStyle(os2, head))
        {
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
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription LoadDescription(string path)
        {
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
            // NOTE: These fields are read in their optimized order
            // https://docs.microsoft.com/en-gb/typography/opentype/spec/recom#optimized-table-ordering
            HeadTable head = reader.GetTable<HeadTable>();
            OS2Table os2 = reader.GetTable<OS2Table>();
            NameTable nameTable = reader.GetTable<NameTable>();

            return new FontDescription(nameTable, os2, head);
        }

        private static FontStyle ConvertStyle(OS2Table os2, HeadTable head)
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
