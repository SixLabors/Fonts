// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables
{
    internal readonly struct TripleEncodingRecord
    {
        public readonly byte ByteCount;
        public readonly byte XBits;
        public readonly byte YBits;
        public readonly ushort DeltaX;
        public readonly ushort DeltaY;
        public readonly sbyte Xsign;
        public readonly sbyte Ysign;

        public TripleEncodingRecord(
            byte byteCount,
            byte xbits,
            byte ybits,
            ushort deltaX,
            ushort deltaY,
            sbyte xsign,
            sbyte ysign)
        {
            this.ByteCount = byteCount;
            this.XBits = xbits;
            this.YBits = ybits;
            this.DeltaX = deltaX;
            this.DeltaY = deltaY;
            this.Xsign = xsign;
            this.Ysign = ysign;
        }

        public int Tx(int orgX) => (orgX + this.DeltaX) * this.Xsign;

        public int Ty(int orgY) => (orgY + this.DeltaY) * this.Ysign;
    }
}
