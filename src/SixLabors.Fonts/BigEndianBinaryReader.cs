// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SixLabors.Fonts;

/// <summary>
/// <para>
/// A binary reader that reads in big-endian format.
/// </para>
/// <para>
/// This reader captures the stream position at construction time as <c>startOfStream</c>.
/// All offset values read from OpenType tables (via <see cref="ReadOffset16"/>,
/// <see cref="ReadOffset32"/>, etc.) are raw values relative to wherever the spec says
/// they originate (typically the start of the containing table).
/// </para>
/// <para>
/// When seeking with <see cref="Seek"/> using <see cref="SeekOrigin.Begin"/>, the
/// <c>startOfStream</c> is automatically added to the supplied offset.  This means
/// table-relative offsets can be passed directly to <see cref="Seek"/> without manually
/// adding the table's absolute position. Do <strong>not</strong> add the table start
/// yourself — that would double-count and land at the wrong position.
/// </para>
/// <para>
/// In contrast, <see cref="BaseStream"/>.<see cref="Stream.Position"/> always returns the
/// <strong>absolute</strong> position within the underlying stream and is unaffected by
/// <c>startOfStream</c>.
/// </para>
/// </summary>
[DebuggerDisplay("Start: {StartOfStream}, Position: {BaseStream.Position}")]
internal sealed class BigEndianBinaryReader : IDisposable
{
    /// <summary>
    /// Buffer used for temporary storage before conversion into primitives.
    /// </summary>
    private readonly byte[] buffer = new byte[16];
    private readonly bool leaveOpen;

    /// <summary>
    /// Initializes a new instance of the <see cref="BigEndianBinaryReader" /> class.
    /// The current position of <paramref name="stream"/> is captured as <c>startOfStream</c>
    /// and used as the origin for all subsequent <see cref="Seek"/> calls with
    /// <see cref="SeekOrigin.Begin"/>.
    /// </summary>
    /// <param name="stream">Stream to read data from.</param>
    /// <param name="leaveOpen">If <see langword="true"/>, the stream is not disposed when this reader is disposed.</param>
    public BigEndianBinaryReader(Stream stream, bool leaveOpen)
    {
        this.BaseStream = stream;
        this.StartOfStream = stream.Position;
        this.leaveOpen = leaveOpen;
    }

    /// <summary>
    /// Gets the underlying stream of the EndianBinaryReader.
    /// Note that <see cref="Stream.Position"/> on this stream is always the
    /// <strong>absolute</strong> position and is <strong>not</strong> adjusted by
    /// <c>startOfStream</c>. Avoid using <c>BaseStream.Position</c> to compute
    /// offsets for <see cref="Seek"/> — use raw offsets from <see cref="ReadOffset16"/>,
    /// <see cref="ReadOffset32"/>, etc. instead.
    /// </summary>
    public Stream BaseStream { get; }

    /// <summary>
    /// Gets the absolute stream position captured at construction time.
    /// This is the origin for all <see cref="SeekOrigin.Begin"/> seeks.
    /// </summary>
    public long StartOfStream { get; }

    /// <summary>
    /// Seeks within the stream.
    /// When <paramref name="origin"/> is <see cref="SeekOrigin.Begin"/>, <c>startOfStream</c>
    /// is automatically added to <paramref name="offset"/>, so callers should pass
    /// table-relative offsets directly (e.g. values read from <see cref="ReadOffset16"/>
    /// or <see cref="ReadOffset32"/>).  Do <strong>not</strong> add the table's absolute
    /// position — that would double-count.
    /// </summary>
    /// <param name="offset">Offset to seek to, relative to <paramref name="origin"/>.</param>
    /// <param name="origin">Origin of seek operation.</param>
    public void Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin)
        {
            offset += this.StartOfStream;
        }

        _ = this.BaseStream.Seek(offset, origin);
    }

    /// <summary>
    /// Reads a single byte from the stream.
    /// </summary>
    /// <returns>The byte read</returns>
    public byte ReadByte()
    {
        this.ReadInternal(this.buffer, 1);
        return this.buffer[0];
    }

    /// <summary>
    /// Reads a single byte from the stream and reinterprets it as the specified enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type whose underlying type must be a single byte.</typeparam>
    /// <returns>The enum value.</returns>
    public TEnum ReadByte<TEnum>()
        where TEnum : struct, Enum
    {
        _ = TryConvert(this.ReadByte(), out TEnum value);
        return value;
    }

    /// <summary>
    /// Reads a single signed byte from the stream.
    /// </summary>
    /// <returns>The byte read</returns>
    public sbyte ReadSByte()
    {
        this.ReadInternal(this.buffer, 1);
        return unchecked((sbyte)this.buffer[0]);
    }

    /// <summary>
    /// Reads a 2.14 fixed-point number from the stream.
    /// 2 bytes are read and divided by 16384 to produce a value in the range [-2, +2).
    /// </summary>
    /// <returns>The fixed-point value as a <see cref="float"/>.</returns>
    public float ReadF2Dot14()
    {
        const float f2Dot14ToFloat = 16384F;
        return this.ReadInt16() / f2Dot14ToFloat;
    }

    /// <summary>
    /// Reads a 16-bit signed integer from the stream, using the bit converter
    /// for this reader. 2 bytes are read.
    /// </summary>
    /// <returns>The 16-bit integer read</returns>
    public short ReadInt16()
    {
        this.ReadInternal(this.buffer, 2);

        return BinaryPrimitives.ReadInt16BigEndian(this.buffer);
    }

    /// <summary>
    /// Reads a 16-bit integer from the stream and reinterprets it as the specified enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type whose underlying type must be 16 bits.</typeparam>
    /// <returns>The enum value.</returns>
    public TEnum ReadInt16<TEnum>()
        where TEnum : struct, Enum
    {
        _ = TryConvert(this.ReadUInt16(), out TEnum value);
        return value;
    }

    /// <summary>
    /// Reads a signed 16-bit integer in big-endian order, representing an FWORD value from the current stream position.
    /// </summary>
    /// <returns>A 16-bit signed integer read from the stream, interpreted as an FWORD value.</returns>
    public short ReadFWORD() => this.ReadInt16();

    /// <summary>
    /// Reads an array of FWORD (signed 16-bit) values from the stream.
    /// </summary>
    /// <param name="length">The number of values to read.</param>
    /// <returns>An array of 16-bit signed integers.</returns>
    public short[] ReadFWORDArray(int length) => this.ReadInt16Array(length);

    /// <summary>
    /// Reads an unsigned 16-bit integer (UFWORD) from the current stream and advances the position by two bytes.
    /// </summary>
    /// <returns>An unsigned 16-bit integer read from the current stream.</returns>
    public ushort ReadUFWORD() => this.ReadUInt16();

    /// <summary>
    /// Reads a 32-bit fixed-point number from the underlying data source and returns it as a single-precision
    /// floating-point value.
    /// </summary>
    /// <returns>A <see cref="float"/> representing the fixed-point value read from the data source.</returns>
    public float ReadFixed()
    {
        this.ReadInternal(this.buffer, 4);
        return BinaryPrimitives.ReadInt32BigEndian(this.buffer) / 65536F;
    }

    /// <summary>
    /// Reads a 4-byte signed integer from the current stream.
    /// </summary>
    /// <returns>The 32-bit signed integer read from the stream.</returns>
    public int ReadInt32()
    {
        this.ReadInternal(this.buffer, 4);

        return BinaryPrimitives.ReadInt32BigEndian(this.buffer);
    }

    /// <summary>
    /// Reads a 64-bit signed integer from the stream.
    /// 8 bytes are read.
    /// </summary>
    /// <returns>The 64-bit integer read.</returns>
    public long ReadInt64()
    {
        this.ReadInternal(this.buffer, 8);

        return BinaryPrimitives.ReadInt64BigEndian(this.buffer);
    }

    /// <summary>
    /// Reads a 16-bit unsigned integer from the stream.
    /// 2 bytes are read.
    /// </summary>
    /// <returns>The 16-bit unsigned integer read.</returns>
    public ushort ReadUInt16()
    {
        this.ReadInternal(this.buffer, 2);

        return BinaryPrimitives.ReadUInt16BigEndian(this.buffer);
    }

    /// <summary>
    /// Reads a 16-bit unsigned integer from the stream representing an offset position.
    /// 2 bytes are read. The returned value is the raw offset as stored in the font file
    /// (typically relative to the start of the containing table). Pass it directly to
    /// <see cref="Seek"/> with <see cref="SeekOrigin.Begin"/> — do not add the table's
    /// absolute position.
    /// </summary>
    /// <returns>The 16-bit unsigned integer read.</returns>
    public ushort ReadOffset16() => this.ReadUInt16();

    /// <summary>
    /// Reads a 16-bit unsigned integer from the stream and reinterprets it as the specified enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type whose underlying type must be 16 bits.</typeparam>
    /// <returns>The enum value.</returns>
    public TEnum ReadUInt16<TEnum>()
        where TEnum : struct, Enum
    {
        _ = TryConvert(this.ReadUInt16(), out TEnum value);
        return value;
    }

    /// <summary>
    /// Reads an array of 16-bit unsigned integers from the stream.
    /// </summary>
    /// <param name="length">The number of values to read.</param>
    /// <returns>An array of 16-bit unsigned integers.</returns>
    public ushort[] ReadUInt16Array(int length)
    {
        ushort[] data = new ushort[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = this.ReadUInt16();
        }

        return data;
    }

    /// <summary>
    /// Reads array of 16-bit unsigned integers from the stream to the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read to.</param>
    public void ReadUInt16Array(Span<ushort> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = this.ReadUInt16();
        }
    }

    /// <summary>
    /// Reads an array of 32-bit unsigned integers from the stream.
    /// </summary>
    /// <param name="length">The number of values to read.</param>
    /// <returns>An array of 32-bit unsigned integers.</returns>
    public uint[] ReadUInt32Array(int length)
    {
        uint[] data = new uint[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = this.ReadUInt32();
        }

        return data;
    }

    /// <summary>
    /// Reads an array of 8-bit unsigned integers (bytes) from the stream.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>A byte array of the requested length.</returns>
    public byte[] ReadUInt8Array(int length)
    {
        byte[] data = new byte[length];

        this.ReadInternal(data, length);

        return data;
    }

    /// <summary>
    /// Reads an array of 16-bit signed integers from the stream.
    /// </summary>
    /// <param name="length">The number of values to read.</param>
    /// <returns>An array of 16-bit signed integers.</returns>
    public short[] ReadInt16Array(int length)
    {
        short[] data = new short[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = this.ReadInt16();
        }

        return data;
    }

    /// <summary>
    /// Reads an array of 16-bit signed integers from the stream to the buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read to.</param>
    public void ReadInt16Array(Span<short> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = this.ReadInt16();
        }
    }

    /// <summary>
    /// Reads a 8-bit unsigned integer from the stream, using the bit converter
    /// for this reader. 1 bytes are read.
    /// </summary>
    /// <returns>The 8-bit unsigned integer read.</returns>
    public byte ReadUInt8()
    {
        this.ReadInternal(this.buffer, 1);
        return this.buffer[0];
    }

    /// <summary>
    /// Reads a 24-bit unsigned integer from the stream, using the bit converter
    /// for this reader. 3 bytes are read.
    /// </summary>
    /// <returns>The 24-bit unsigned integer read.</returns>
    public uint ReadUInt24()
    {
        byte highByte = this.ReadByte();
        return (uint)((highByte << 16) | this.ReadUInt16());
    }

    /// <summary>
    /// Reads a 24-bit unsigned integer from the stream representing an offset position.
    /// 3 bytes are read. The returned value is the raw offset as stored in the font file
    /// (typically relative to the start of the containing table). Pass it directly to
    /// <see cref="Seek"/> with <see cref="SeekOrigin.Begin"/> — do not add the table's
    /// absolute position.
    /// </summary>
    /// <returns>The 24-bit unsigned integer read.</returns>
    public uint ReadOffset24() => this.ReadUInt24();

    /// <summary>
    /// Reads a 32-bit unsigned integer from the stream, using the bit converter
    /// for this reader. 4 bytes are read.
    /// </summary>
    /// <returns>The 32-bit unsigned integer read.</returns>
    public uint ReadUInt32()
    {
        this.ReadInternal(this.buffer, 4);

        return BinaryPrimitives.ReadUInt32BigEndian(this.buffer);
    }

    /// <summary>
    /// Reads a 32-bit unsigned integer from the stream representing an offset position.
    /// 4 bytes are read. The returned value is the raw offset as stored in the font file
    /// (typically relative to the start of the containing table). Pass it directly to
    /// <see cref="Seek"/> with <see cref="SeekOrigin.Begin"/> — do not add the table's
    /// absolute position.
    /// </summary>
    /// <returns>The 32-bit unsigned integer read.</returns>
    public uint ReadOffset32() => this.ReadUInt32();

    /// <summary>
    /// Reads the specified number of bytes, returning them in a new byte array.
    /// If not enough bytes are available before the end of the stream, this
    /// method will return what is available.
    /// </summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The bytes read.</returns>
    public byte[] ReadBytes(int count)
    {
        byte[] ret = new byte[count];
        int index = 0;
        while (index < count)
        {
            int read = this.BaseStream.Read(ret, index, count - index);

            // Stream has finished half way through. That's fine, return what we've got.
            if (read == 0)
            {
                byte[] copy = new byte[index];
                Buffer.BlockCopy(ret, 0, copy, 0, index);
                return copy;
            }

            index += read;
        }

        return ret;
    }

    /// <summary>
    /// Reads a string of a specific length, which specifies the number of bytes
    /// to read from the stream. These bytes are then converted into a string with
    /// the encoding for this reader.
    /// </summary>
    /// <param name="bytesToRead">The bytes to read.</param>
    /// <param name="encoding">The encoding.</param>
    /// <returns>
    /// The string read from the stream.
    /// </returns>
    public string ReadString(int bytesToRead, Encoding encoding)
    {
        byte[] data = new byte[bytesToRead];
        this.ReadInternal(data, bytesToRead);
        return encoding.GetString(data, 0, data.Length);
    }

    /// <summary>
    /// Reads a 4-byte OpenType tag from the stream as a UTF-8 string.
    /// </summary>
    /// <returns>A 4-character string representing the tag (e.g. "glyf", "GPOS").</returns>
    public string ReadTag()
    {
        this.ReadInternal(this.buffer, 4);

        return Encoding.UTF8.GetString(this.buffer, 0, 4);
    }

    /// <summary>
    /// Reads an offset consuming the given number of bytes (1–4).
    /// The returned value is the raw offset as stored in the font file
    /// (typically relative to the start of the containing table). Pass it directly to
    /// <see cref="Seek"/> with <see cref="SeekOrigin.Begin"/> — do not add the table's
    /// absolute position.
    /// </summary>
    /// <param name="size">The offset size in bytes (1, 2, 3, or 4).</param>
    /// <returns>The 32-bit signed integer representing the offset.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="size"/> is not 1–4.</exception>
    public int ReadOffset(int size)
        => size switch
        {
            1 => this.ReadByte(),
            2 => (this.ReadByte() << 8) | (this.ReadByte() << 0),
            3 => (this.ReadByte() << 16) | (this.ReadByte() << 8) | (this.ReadByte() << 0),
            4 => (this.ReadByte() << 24) | (this.ReadByte() << 16) | (this.ReadByte() << 8) | (this.ReadByte() << 0),
            _ => throw new InvalidOperationException(),
        };

    /// <summary>
    /// Reads the given number of bytes from the stream, throwing an exception
    /// if they can't all be read.
    /// </summary>
    /// <param name="data">Buffer to read into.</param>
    /// <param name="size">Number of bytes to read.</param>
    /// <exception cref="EndOfStreamException">The end of the stream was reached before reading could complete.</exception>
    private void ReadInternal(byte[] data, int size)
    {
        int index = 0;

        while (index < size)
        {
            int read = this.BaseStream.Read(data, index, size - index);
            if (read == 0)
            {
                throw new EndOfStreamException($"End of stream reached with {size - index} byte{(size - index == 1 ? "s" : string.Empty)} left to read.");
            }

            index += read;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this.leaveOpen)
        {
            this.BaseStream?.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvert<T, TEnum>(T input, out TEnum value)
        where T : struct, IConvertible, IFormattable, IComparable
        where TEnum : struct, Enum
    {
        if (Unsafe.SizeOf<T>() == Unsafe.SizeOf<TEnum>())
        {
            value = Unsafe.As<T, TEnum>(ref input);
            return true;
        }

        value = default;
        return false;
    }
}
