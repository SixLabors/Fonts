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
        /// Initializes a new instance of the <see cref="FontDescription"/> class.
        /// </summary>
        /// <param name="nameTable">The name table.</param>
        internal FontDescription(NameTable nameTable)
        {
            this.nameTable = nameTable;
        }

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        public string FontName => this.nameTable.FontName;

        /// <summary>
        /// Gets the name of the font.
        /// </summary>
        public string FontFamily => this.nameTable.FontFamilyName;

        /// <summary>
        /// Gets the font sub family.
        /// </summary>
        public string FontSubFamilyName => this.nameTable.FontSubFamilyName;

        /// <summary>
        /// Reads a <see cref="FontDescription"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>a <see cref="FontDescription"/>.</returns>
        public static FontDescription Load(Stream stream)
        {
            // only read the name table
            var reader = new FontReader(stream);
            return Load(reader);
        }

        /// <summary>
        /// Reads a <see cref="FontDescription" /> from the specified stream.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// a <see cref="FontDescription" />.
        /// </returns>
        internal static FontDescription Load(FontReader reader)
        {
            var nameTable = reader.GetTable<NameTable>();

            return new FontDescription(nameTable);
        }
    }
}
