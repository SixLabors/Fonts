// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// A lightweight big-endian binary reader over a <see cref="ReadOnlySpan{T}"/> buffer,
/// used for reading Type 2 charstring data without allocations.
/// </summary>
internal ref struct SimpleBinaryReader
{
    private readonly ReadOnlySpan<byte> buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleBinaryReader"/> struct.
    /// </summary>
    /// <param name="buffer">The byte buffer to read from.</param>
    public SimpleBinaryReader(ReadOnlySpan<byte> buffer)
    {
        this.buffer = buffer;
        this.Position = 0;
    }

    /// <summary>
    /// Gets the total length of the underlying buffer.
    /// </summary>
    public readonly int Length => this.buffer.Length;

    /// <summary>
    /// Gets or sets the current read position within the buffer.
    /// </summary>
    // TODO: Bounds checks.
    public int Position { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are remaining bytes to read.
    /// </summary>
    /// <returns><see langword="true"/> if the position is within the buffer; otherwise, <see langword="false"/>.</returns>
    public readonly bool CanRead() => (uint)this.Position < this.buffer.Length;

    /// <summary>
    /// Reads a single byte and advances the position.
    /// </summary>
    /// <returns>The byte value.</returns>
    public byte ReadByte() => this.buffer[this.Position++];

    /// <summary>
    /// Reads a big-endian 16-bit signed integer and advances the position by 2 bytes.
    /// </summary>
    /// <returns>The 16-bit signed integer value.</returns>
    public int ReadInt16BE()
    {
        byte b1 = this.buffer[this.Position + 1];
        byte b0 = this.buffer[this.Position];
        this.Position += 2;

        return (short)((b0 << 8) | b1);
    }

    /// <summary>
    /// Reads a big-endian 16.16 fixed-point number and advances the position by 4 bytes.
    /// </summary>
    /// <returns>The floating-point value.</returns>
    public float ReadFloatFixed1616()
    {
        // Read a BE int, we parse it later.
        byte b3 = this.buffer[this.Position + 3];
        byte b2 = this.buffer[this.Position + 2];
        byte b1 = this.buffer[this.Position + 1];
        byte b0 = this.buffer[this.Position];
        this.Position += 4;

        // This number is interpreted as a Fixed; that is, a signed number with 16 bits of fraction
        float number = (short)((b0 << 8) | b1);
        float fraction = (short)((b2 << 8) | b3) / 65536F;
        return number + fraction;
    }
}
