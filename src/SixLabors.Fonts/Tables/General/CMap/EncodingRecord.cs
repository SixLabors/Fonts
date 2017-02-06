using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap
{
    internal struct EncodingRecord
    {
        public PlatformIDs PlatformID { get; }
        public ushort EncodingID { get; }
        public uint Offset { get; }
        public EncodingRecord(PlatformIDs platformID, ushort encodingID, uint offset)
        {
            PlatformID = platformID;
            EncodingID = encodingID;
            Offset = offset;
        }

        public static EncodingRecord Read(BinaryReader reader)
        {
            var platform = (PlatformIDs)reader.ReadUInt16();
            ushort encoding = reader.ReadUInt16();
            uint offset = reader.ReadOffset32();

            return new EncodingRecord(platform, encoding, offset);
        }
    }
}
