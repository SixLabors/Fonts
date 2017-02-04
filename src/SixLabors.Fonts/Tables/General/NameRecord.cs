using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    internal class NameRecord
    {
        private readonly string value;

        public PlatformIDs Platform { get; }

        public ushort LanguageID { get; }

        public NameIds NameID { get; }

        internal StringLoader StringReader { get; private set; }

        public string Value => this.StringReader?.Value ?? this.value;

        public NameRecord(PlatformIDs platform, ushort languageId, NameIds nameId, string value)
        {
            this.Platform = platform;
            this.LanguageID = languageId;
            this.NameID = nameId;
            this.value = value;
        }

        public static NameRecord Read(BinaryReader reader)
        {
            var platform = (PlatformIDs)reader.ReadUInt16();
            Encoding encoding = ((EncodingIDs)reader.ReadUInt16()).AsEncoding();
            var languageID = reader.ReadUInt16();
            var nameID = (NameIds)reader.ReadUInt16();

            var stringReader = StringLoader.Create(reader, encoding);

            return new NameRecord(platform, languageID, nameID, null)
            {
                StringReader = stringReader
            };
        }
    }
}
