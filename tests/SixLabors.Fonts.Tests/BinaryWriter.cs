using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace SixLabors.Fonts.Tests
{
    internal class BinaryWriter
    {
        /// <summary>
        /// Buffer used for temporary storage during conversion from primitives
        /// </summary>
        private readonly byte[] buffer = new byte[16];

        /// <summary>
        /// Buffer used for Write(char)
        /// </summary>
        private readonly char[] charBuffer = new char[1];

        /// <summary>
        /// Whether or not this writer has been disposed yet.
        /// </summary>
        private bool disposed;


        public BinaryWriter()
            : this(new MemoryStream())
        {
        }

        public BinaryWriter(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException("stream");
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentException("Stream isn't writable", "stream");
            }

            this.BaseStream = stream;
        }

        /// <summary>
        /// Gets the underlying stream of the EndianBinaryWriter.
        /// </summary>
        public Stream BaseStream { get; }

        /// <summary>
        /// Closes the writer, including the underlying stream.
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Flushes the underlying stream.
        /// </summary>
        public void Flush()
        {
            this.CheckDisposed();
            this.BaseStream.Flush();
        }


        public BinaryReader GetReader()
        {
            return new BinaryReader(this.GetStream(), true);
        }

        public MemoryStream GetStream()
        {
            this.Flush();
            long p = this.BaseStream.Position;
            this.BaseStream.Position = 0;

            var ms = new MemoryStream();
            this.BaseStream.CopyTo(ms);
            ms.Position = 0;
            this.BaseStream.Position = 0;

            return ms;
        }

        /// <summary>
        /// Seeks within the stream.
        /// </summary>
        /// <param name="offset">Offset to seek to.</param>
        /// <param name="origin">Origin of seek operation.</param>
        public void Seek(int offset, SeekOrigin origin)
        {
            this.CheckDisposed();
            this.BaseStream.Seek(offset, origin);
        }

        /// <summary>
        /// Writes a boolean value to the stream. 1 byte is written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(bool value)
        {
            this.buffer[0] = value ? (byte)1 : (byte)0;

            this.WriteInternal(this.buffer, 1);
        }

        /// <summary>
        /// Writes a 16-bit signed integer to the stream, using the bit converter
        /// for this writer. 2 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(short value)
        {
            BinaryPrimitives.WriteInt16BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 2);
        }

        /// <summary>
        /// Writes a 32-bit signed integer to the stream, using the bit converter
        /// for this writer. 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(int value)
        {
            BinaryPrimitives.WriteInt32BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 4);
        }

        /// <summary>
        /// Writes a 64-bit signed integer to the stream.
        /// 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(long value)
        {
            BinaryPrimitives.WriteInt64BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 8);
        }

        public void WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 4);
        }

        public void WriteUInt64(ulong value)
        {
            BinaryPrimitives.WriteUInt64BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 8);
        }

        public void WriteInt64(long value)
        {
            BinaryPrimitives.WriteInt64BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 8);
        }

        public void WriteOffset32(uint? value)
        {
            this.WriteUInt32(value ?? 0);
        }


        public void WriteOffset16(ushort? value)
        {
            this.WriteUInt16(value ?? 0);
        }

        /// <summary>
        /// Writes a 32-bit unsigned integer to the stream.
        /// 4 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 4);
        }

        /// <summary>
        /// Writes a 64-bit unsigned integer to the stream, using the bit converter
        /// for this writer. 8 bytes are written.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(ulong value)
        {
            BinaryPrimitives.WriteUInt64BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 8);
        }

        /// <summary>
        /// Writes a signed byte to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(byte value)
        {
            this.buffer[0] = value;
            this.WriteInternal(this.buffer, 1);
        }

        /// <summary>
        /// Writes an unsigned byte to the stream.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(sbyte value)
        {
            this.buffer[0] = unchecked((byte)value);
            this.WriteInternal(this.buffer, 1);
        }

        /// <summary>
        /// Writes an array of bytes to the stream.
        /// </summary>
        /// <param name="value">The values to write</param>
        public void Write(byte[] value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.WriteInternal(value, value.Length);
        }

        /// <summary>
        /// Writes a portion of an array of bytes to the stream.
        /// </summary>
        /// <param name="value">An array containing the bytes to write</param>
        /// <param name="offset">The index of the first byte to write within the array</param>
        /// <param name="count">The number of bytes to write</param>
        public void Write(byte[] value, int offset, int count)
        {
            this.CheckDisposed();
            this.BaseStream.Write(value, offset, count);
        }

        /// <summary>
        /// Writes a single character to the stream, using the encoding for this writer.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void Write(char value, Encoding encoding)
        {
            this.charBuffer[0] = value;
            this.Write(this.charBuffer, encoding);
        }

        /// <summary>
        /// Writes an array of characters to the stream, using the encoding for this writer.
        /// </summary>
        /// <param name="value">An array containing the characters to write</param>
        public void Write(char[] value, Encoding encoding)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.CheckDisposed();
            byte[] data = encoding.GetBytes(value, 0, value.Length);
            this.WriteInternal(data, data.Length);
        }

        public void WriteUInt32(string text)
        {
            if (text.Length != 4)
            {
                throw new Exception("text must be exactly 4 characters long");
            }

            this.WriteNoLength(text, Encoding.ASCII);
        }

        /// <summary>
        /// Writes a string to the stream, using the encoding for this writer.
        /// </summary>
        /// <param name="value">The value to write. Must not be null.</param>
        /// <exception cref="ArgumentNullException">value is null</exception>
        public void WriteNoLength(string value, Encoding encoding)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.CheckDisposed();
            byte[] data = encoding.GetBytes(value);

            // this.Write7BitEncodedInt(data.Length);
            this.WriteInternal(data, data.Length);
        }

        /// <summary>
        /// Writes a 7-bit encoded integer from the stream. This is stored with the least significant
        /// information first, with 7 bits of information per byte of value, and the top
        /// bit as a continuation flag.
        /// </summary>
        /// <param name="value">The 7-bit encoded integer to write to the stream</param>
        public void Write7BitEncodedInt(int value)
        {
            this.CheckDisposed();
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be greater than or equal to 0.");
            }

            int index = 0;
            while (value >= 128)
            {
                this.buffer[index++] = (byte)((value & 0x7f) | 0x80);
                value = value >> 7;
                index++;
            }

            this.buffer[index++] = (byte)value;
            this.BaseStream.Write(this.buffer, 0, index);
        }

        /// <summary>
        /// Disposes of the underlying stream.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Flush();
                this.disposed = true;
                ((IDisposable)this.BaseStream).Dispose();
            }
        }

        /// <summary>
        /// Checks whether or not the writer has been disposed, throwing an exception if so.
        /// </summary>
        private void CheckDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(BinaryWriter));
            }
        }

        /// <summary>
        /// Writes the specified number of bytes from the start of the given byte array,
        /// after checking whether or not the writer has been disposed.
        /// </summary>
        /// <param name="bytes">The array of bytes to write from</param>
        /// <param name="length">The number of bytes to write</param>
        private void WriteInternal(byte[] bytes, int length)
        {
            this.CheckDisposed();
            this.BaseStream.Write(bytes, 0, length);
        }

        public void WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 2);
        }

        public void WriteInt16(short value)
        {
            BinaryPrimitives.WriteInt16BigEndian(this.buffer, value);

            this.WriteInternal(this.buffer, 2);
        }

        public void WriteUInt8(byte value)
        {
            this.buffer[0] = value;

            this.WriteInternal(this.buffer, 1);
        }

        public void WriteFWORD(short value)
        {
            this.WriteInt16(value);
        }

        public void WriteUFWORD(ushort value)
        {
            this.WriteUInt16(value);
        }
    }
}
