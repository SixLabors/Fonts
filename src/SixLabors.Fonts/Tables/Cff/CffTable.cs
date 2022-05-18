// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class CffTable : Table
    {
        internal const string TableName = "CFF";

        public CffTable(Cff1FontSet? cff1FontSet) => this.Cff1FontSet = cff1FontSet;

        public Cff1FontSet? Cff1FontSet { get; }

        public static CffTable? Load(FontReader fontReader)
        {
            if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
            {
                return null;
            }

            using (binaryReader)
            {
                return Load(binaryReader);
            }
        }

        public static CffTable Load(BigEndianBinaryReader reader)
        {
            // Type      Name      Description
            // Card8     major     Format major version(starting at 1)
            // Card8     minor     Format minor version(starting at 0)
            // Card8     hdrSize   Header size(bytes)
            // OffSize   offSize   Absolute offset(0) size
            long position = reader.BaseStream.Position;
            byte[] header = reader.ReadBytes(4);
            byte major = header[0];
            byte minor = header[1];
            byte hdrSize = header[2];
            byte offSize = header[3];

            switch (major)
            {
                case 1:
                {
                    Cff1Parser cff1 = new();
                    cff1.Load(reader, position);
                    return new(cff1.Cff1FontSet);
                }

                case 2:
                {
                    // Cff2Parser cff2 = new();
                    // cff2.ParseAfterHeader(reader);
                    // return new(null);
                    throw new NotSupportedException();
                }

                default:
                    throw new NotSupportedException();
            }
        }
    }
}
