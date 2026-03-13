// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables;

/// <summary>
/// Represents a single entry in the WOFF2 triplet encoding table, which defines how
/// glyph coordinate triplets (flag, x, y) are packed into a compact binary representation.
/// <see href="https://www.w3.org/TR/WOFF2/#triplet_encoding"/>
/// </summary>
internal readonly struct TripleEncodingRecord
{
    /// <summary>The total number of bytes for this triplet (flag + x + y).</summary>
    public readonly byte ByteCount;

    /// <summary>The number of bits used to encode the X coordinate value.</summary>
    public readonly byte XBits;

    /// <summary>The number of bits used to encode the Y coordinate value.</summary>
    public readonly byte YBits;

    /// <summary>The delta offset added to the raw X coordinate value before applying the sign.</summary>
    public readonly ushort DeltaX;

    /// <summary>The delta offset added to the raw Y coordinate value before applying the sign.</summary>
    public readonly ushort DeltaY;

    /// <summary>The sign multiplier for the X coordinate (-1, 0, or 1).</summary>
    public readonly sbyte Xsign;

    /// <summary>The sign multiplier for the Y coordinate (-1, 0, or 1).</summary>
    public readonly sbyte Ysign;

    /// <summary>
    /// Initializes a new instance of the <see cref="TripleEncodingRecord"/> struct.
    /// </summary>
    /// <param name="byteCount">The total byte count for this triplet.</param>
    /// <param name="xbits">The number of bits for the X coordinate.</param>
    /// <param name="ybits">The number of bits for the Y coordinate.</param>
    /// <param name="deltaX">The delta offset for X.</param>
    /// <param name="deltaY">The delta offset for Y.</param>
    /// <param name="xsign">The sign multiplier for X.</param>
    /// <param name="ysign">The sign multiplier for Y.</param>
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

    /// <summary>
    /// Transforms a raw X coordinate value using the delta and sign from this record.
    /// </summary>
    /// <param name="orgX">The raw X coordinate value read from the stream.</param>
    /// <returns>The signed, delta-adjusted X coordinate.</returns>
    public int Tx(int orgX) => (orgX + this.DeltaX) * this.Xsign;

    /// <summary>
    /// Transforms a raw Y coordinate value using the delta and sign from this record.
    /// </summary>
    /// <param name="orgY">The raw Y coordinate value read from the stream.</param>
    /// <returns>The signed, delta-adjusted Y coordinate.</returns>
    public int Ty(int orgY) => (orgY + this.DeltaY) * this.Ysign;
}
