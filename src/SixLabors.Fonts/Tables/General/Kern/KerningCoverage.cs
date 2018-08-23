// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General.Kern
{
    internal readonly struct KerningCoverage
    {
        private KerningCoverage(bool horizontal, bool hasMinimum, bool crossStream, bool overrideAccumulator, byte format)
        {
            this.Horizontal = horizontal;
            this.HasMinimum = hasMinimum;
            this.CrossStream = crossStream;
            this.OverrideAccumulator = overrideAccumulator;
            this.Format = format;
        }

        public bool Horizontal { get; }

        public bool HasMinimum { get; }

        public bool CrossStream { get; }

        public bool OverrideAccumulator { get; }

        public byte Format { get; }

        public static KerningCoverage Read(BinaryReader reader)
        {
            // The coverage field is divided into the following sub-fields, with sizes given in bits:
            // Sub-field    | Bits #'s | Size | Description
            // -------------|----------|------|-----------------------------------------------
            // horizontal   |  0       |  1   | 1 if table has horizontal data, 0 if vertical.
            // minimum      |  1       |  1   | If this bit is set to 1, the table has minimum values.If set to 0, the table has kerning values.
            // cross-stream |  2       |  1   | If set to 1, kerning is perpendicular to the flow of the text.
            //                                  If the text is normally written horizontally, kerning will be done in the up and down directions.If kerning values are positive, the text will be kerned upwards; if they are negative, the text will be kerned downwards.
            //                                  If the text is normally written vertically, kerning will be done in the left and right directions.If kerning values are positive, the text will be kerned to the right; if they are negative, the text will be kerned to the left.
            //                                  The value 0x8000 in the kerning data resets the cross-stream kerning back to 0.
            // override     | 3        |  1   | If this bit is set to 1 the value in this table should replace the value currently being accumulated.
            // reserved1    | 4 -7     |  4   | Reserved.This should be set to zero.
            // format       | 8 -15    |  8   | Format of the subtable. Only formats 0 and 2 have been defined.Formats 1 and 3 through 255 are reserved for future use.
            ushort coverage = reader.ReadUInt16();
            bool horizontal = (coverage & 0x1) == 1;
            bool hasMinimum = ((coverage >> 1) & 0x1) == 1;
            bool crossStream = ((coverage >> 2) & 0x1) == 1;
            bool overrideAccumulator = ((coverage >> 3) & 0x1) == 1;
            byte format = (byte)((coverage >> 7) & 0xff);
            return new KerningCoverage(horizontal, hasMinimum, crossStream, overrideAccumulator, format);
        }
    }
}
