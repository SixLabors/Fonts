// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SixLabors.Fonts
{
    /// <summary>
    /// BinaryReader using big-endian encoding.
    /// </summary>
    [DebuggerDisplay("Start: {StartOfStream}, Position: {BaseStream.Position}")]
    internal class BigEndianBinaryReader : IDisposable
    {
        /// <summary>
        /// Buffer used for temporary storage before conversion into primitives
        /// </summary>
        private readonly byte[] buffer = new byte[16];

        private readonly bool leaveOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="BigEndianBinaryReader" /> class.
        /// Constructs a new binary reader with the given bit converter, reading
        /// to the given stream, using the given encoding.
        /// </summary>
        /// <param name="stream">Stream to read data from</param>
        /// <param name="leaveOpen">if set to <c>true</c> [leave open].</param>
        public BigEndianBinaryReader(Stream stream, bool leaveOpen)
        {
            this.BaseStream = stream;
            this.StartOfStream = stream.Position;
            this.leaveOpen = leaveOpen;
        }

        private long StartOfStream { get; }

        /// <summary>
        /// Gets the underlying stream of the EndianBinaryReader.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Seeks within the stream.
        /// </summary>
        /// <param name="offset">Offset to seek to.</param>
        /// <param name="origin">Origin of seek operation. If SeekOrigin.Begin, the offset will be set to the start of stream position.</param>
        public void Seek(long offset, SeekOrigin origin)
        {
            // If SeekOrigin.Begin, the offset will be set to the start of stream position.
            if (origin == SeekOrigin.Begin)
            {
                offset += this.StartOfStream;
            }

            this.BaseStream.Seek(offset, origin);
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
        /// Reads a single signed byte from the stream.
        /// </summary>
        /// <returns>The byte read</returns>
        public sbyte ReadSByte()
        {
            this.ReadInternal(this.buffer, 1);
            return unchecked((sbyte)this.buffer[0]);
        }

        public float ReadF2dot14()
        {
            const float f2Dot14ToFloat = 16384.0f;
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

        public TEnum ReadInt16<TEnum>()
            where TEnum : struct, Enum
        {
            TryConvert(this.ReadUInt16(), out TEnum value);
            return value;
        }

        public short ReadFWORD() => this.ReadInt16();

        public short[] ReadFWORDArray(int length) => this.ReadInt16Array(length);

        public ushort ReadUFWORD() => this.ReadUInt16();

        /// <summary>
        /// Reads a fixed 32-bit value from the stream.
        /// 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit value read.</returns>
        public float ReadFixed()
        {
            this.ReadInternal(this.buffer, 4);
            return BinaryPrimitives.ReadInt32BigEndian(this.buffer) / 65536F;
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream, using the bit converter
        /// for this reader. 4 bytes are read.
        /// </summary>
        /// <returns>The 32-bit integer read</returns>
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
        /// 2 bytes are read.
        /// </summary>
        /// <returns>The 16-bit unsigned integer read.</returns>
        public ushort ReadOffset16() => this.ReadUInt16();

        public TEnum ReadUInt16<TEnum>()
            where TEnum : struct, Enum
        {
            TryConvert(this.ReadUInt16(), out TEnum value);
            return value;
        }

        /// <summary>
        /// Reads array of 16-bit unsigned integers from the stream.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The 16-bit unsigned integer read.
        /// </returns>
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
        /// Reads array or 32-bit unsigned integers from the stream.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The 32-bit unsigned integer read.
        /// </returns>
        public uint[] ReadUInt32Array(int length)
        {
            uint[] data = new uint[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = this.ReadUInt32();
            }

            return data;
        }

        public byte[] ReadUInt8Array(int length)
        {
            byte[] data = new byte[length];

            this.ReadInternal(data, length);

            return data;
        }

        /// <summary>
        /// Reads array of 16-bit unsigned integers from the stream.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>
        /// The 16-bit signed integer read.
        /// </returns>
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
        public int ReadUInt24()
        {
            byte highByte = this.ReadByte();
            return (highByte << 16) | this.ReadUInt16();
        }

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
        /// 4 bytes are read.
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
        /// Reads the uint32 string.
        /// </summary>
        /// <returns>a 4 character long UTF8 encoded string.</returns>
        public string ReadTag()
        {
            this.ReadInternal(this.buffer, 4);

            return Encoding.UTF8.GetString(this.buffer, 0, 4);
        }

        /// <summary>
        /// Reads an offset consuming the given nuber of bytes.
        /// </summary>
        /// <param name="size">The offset size in bytes.</param>
        /// <returns>The 32-bit signed integer representing the offset.</returns>
        /// <exception cref="InvalidOperationException">Size is not in range.</exception>
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
}
