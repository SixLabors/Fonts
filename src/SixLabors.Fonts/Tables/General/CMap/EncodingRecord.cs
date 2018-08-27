// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap
{
    internal readonly struct EncodingRecord
    {
        public EncodingRecord(PlatformIDs platformID, ushort encodingID, uint offset)
        {
            this.PlatformID = platformID;
            this.EncodingID = encodingID;
            this.Offset = offset;
        }

        public PlatformIDs PlatformID { get; }

        public ushort EncodingID { get; }

        public uint Offset { get; }

        public static EncodingRecord Read(BinaryReader reader)
        {
            PlatformIDs platform = (PlatformIDs)reader.ReadUInt16();
            ushort encoding = reader.ReadUInt16();
            uint offset = reader.ReadOffset32();

            return new EncodingRecord(platform, encoding, offset);
        }
    }
}
