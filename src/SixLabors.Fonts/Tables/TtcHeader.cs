// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace SixLabors.Fonts.Tables
{
    /// <summary>
    /// Represents a font collection header (for .ttc font follections).
    /// A font collection contains one or more fonts where typically the glyf table is shared by multiple fonts to save space,
    /// but other tables are not.
    /// Each font in the collection has its own set of tables.
    /// </summary>
    internal class TtcHeader
    {
        public TtcHeader(string ttcTag, ushort majorVersion, ushort minorVersion, uint numFonts, uint[] offsetTable, uint dsigTag, uint dsigLength, uint dsigOffset)
        {
            this.TtcTag = ttcTag;
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.NumFonts = numFonts;
            this.OffsetTable = offsetTable;
            this.DsigTag = dsigTag;
            this.DsigLength = dsigLength;
            this.DsigOffset = dsigOffset;
        }

        /// <summary>
        /// Gets the tag, should be "ttcf".
        /// </summary>
        public string TtcTag { get; }

        public ushort MajorVersion { get; }

        public ushort MinorVersion { get; }

        public uint NumFonts { get; }

        /// <summary>
        /// Gets the array of offsets to the OffsetTable of each font. Use <see cref="FontReader"/> for each font.
        /// </summary>
        public uint[] OffsetTable { get; }

        public uint DsigTag { get; }

        public uint DsigLength { get; }

        public uint DsigOffset { get; }

        public static TtcHeader Read(BinaryReader reader)
        {
            string tag = reader.ReadTag();
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            uint numFonts = reader.ReadUInt32();
            uint[] offsetTable = new uint[numFonts];
            for (int i = 0; i < numFonts; ++i)
            {
                offsetTable[i] = reader.ReadOffset32();
            }

            // Version 2 fields
            uint dsigTag = 0;
            uint dsigLength = 0;
            uint dsigOffset = 0;
            if (majorVersion >= 2)
            {
                dsigTag = reader.ReadUInt32();
                dsigLength = reader.ReadUInt32();
                dsigOffset = reader.ReadUInt32();
            }

            return new TtcHeader(tag, majorVersion, minorVersion, numFonts, offsetTable, dsigTag, dsigLength, dsigOffset);
        }
    }
}
