using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap
{
    internal class Format0SubTable : CMapSubTable
    {
        internal byte[] GlyphIds { get; }

        public Format0SubTable(ushort language, PlatformIDs platform, ushort encoding, byte[] glyphIds)
            : base(platform, encoding)
        {
            this.Language = language;
            this.GlyphIds = glyphIds;
        }

        public ushort Language { get; }

        public override ushort GetGlyphId(char character)
        {
            uint b = character;
            if (b >= this.GlyphIds.Length)
            {
                return 0;
            }

            return this.GlyphIds[b];
        }

        public static Format0SubTable Load(EncodingRecord encoding, BinaryReader reader)
        {
            // format has already been read by this point skip it
            var length = reader.ReadUInt16();
            ushort language = reader.ReadUInt16();
            var glyphsCount = length - 6;

            // char 'A' == 65 thus glyph = glyphIds[65];
            byte[] glyphIds = reader.ReadBytes(glyphsCount);

            return new Format0SubTable(language, encoding.PlatformID, encoding.EncodingID, glyphIds);
        }
    }
}
