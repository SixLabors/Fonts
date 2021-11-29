// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Text;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.Name
{
    internal class NameRecord
    {
        private readonly string value;

        public NameRecord(PlatformIDs platform, ushort languageId, KnownNameIds nameId, string value)
        {
            this.Platform = platform;
            this.LanguageID = languageId;
            this.NameID = nameId;
            this.value = value;
        }

        public PlatformIDs Platform { get; }

        public ushort LanguageID { get; }

        public KnownNameIds NameID { get; }

        internal StringLoader? StringReader { get; private set; }

        public string Value => this.StringReader?.Value ?? this.value;

        public static NameRecord Read(BigEndianBinaryReader reader)
        {
            PlatformIDs platform = reader.ReadUInt16<PlatformIDs>();
            EncodingIDs encodingId = reader.ReadUInt16<EncodingIDs>();
            Encoding encoding = encodingId.AsEncoding();
            ushort languageID = reader.ReadUInt16();
            KnownNameIds nameID = reader.ReadUInt16<KnownNameIds>();

            var stringReader = StringLoader.Create(reader, encoding);

            return new NameRecord(platform, languageID, nameID, string.Empty)
            {
                StringReader = stringReader
            };
        }
    }
}
