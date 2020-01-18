// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.IO.Compression;

namespace SixLabors.Fonts.IO
{
    internal sealed class ZlibInflateStream : Stream
    {
        private long position = 0;

        /// <summary>
        /// The raw stream containing the uncompressed image data.
        /// </summary>
        private readonly Stream rawStream;

        /// <summary>
        /// A value indicating whether this instance of the given entity has been disposed.
        /// </summary>
        /// <value><see langword="true"/> if this instance has been disposed; otherwise, <see langword="false"/>.</value>
        /// <remarks>
        /// If the entity is disposed, it must not be disposed a second
        /// time. The isDisposed field is set the first time the entity
        /// is disposed. If the isDisposed field is true, then the Dispose()
        /// method will not dispose again. This help not to prolong the entity's
        /// life in the Garbage Collector.
        /// </remarks>
        private bool isDisposed;

        /// <summary>
        /// The read crc data.
        /// </summary>
        private byte[]? crcread;

        /// <summary>
        /// The stream responsible for decompressing the input stream.
        /// </summary>
        private DeflateStream? deflateStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZlibInflateStream"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="Exception">
        /// Thrown if the compression method is incorrect.
        /// </exception>
        public ZlibInflateStream(Stream stream)
        {
            // The DICT dictionary identifier identifying the used dictionary.

            // The preset dictionary.
            bool fdict;
            this.rawStream = stream;

            // Read the zlib header : http://tools.ietf.org/html/rfc1950
            // CMF(Compression Method and flags)
            // This byte is divided into a 4 - bit compression method and a
            // 4-bit information field depending on the compression method.
            // bits 0 to 3  CM Compression method
            // bits 4 to 7  CINFO Compression info
            //
            //   0   1
            // +---+---+
            // |CMF|FLG|
            // +---+---+
            int cmf = this.rawStream.ReadByte();
            int flag = this.rawStream.ReadByte();
            if (cmf == -1 || flag == -1)
            {
                return;
            }

            if ((cmf & 0x0f) != 8)
            {
                throw new Exception($"Bad compression method for ZLIB header: cmf={cmf}");
            }

            // CINFO is the base-2 logarithm of the LZ77 window size, minus eight.
            // int cinfo = ((cmf & (0xf0)) >> 8);
            fdict = (flag & 32) != 0;

            if (fdict)
            {
                // The DICT dictionary identifier identifying the used dictionary.
                byte[] dictId = new byte[4];

                for (int i = 0; i < 4; i++)
                {
                    // We consume but don't use this.
                    dictId[i] = (byte)this.rawStream.ReadByte();
                }
            }

            // Initialize the deflate Stream.
            this.deflateStream = new DeflateStream(this.rawStream, CompressionMode.Decompress, true);
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            this.deflateStream?.Flush();
        }

        public override int ReadByte()
        {
            return base.ReadByte();
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.deflateStream is null)
            {
                throw new ObjectDisposedException("inner stream");
            }

            // We dont't check CRC on reading
            int read = this.deflateStream.Read(buffer, offset, count);
            if (read < 1 && this.crcread is null)
            {
                // The deflater has ended. We try to read the next 4 bytes from raw stream (crc)
                this.crcread = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    // we dont really check/use this
                    this.crcread[i] = (byte)this.rawStream.ReadByte();
                }
            }

            this.position += read;
            return read;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                origin = SeekOrigin.Current;
                offset = offset - this.position;
            }

            if (origin == SeekOrigin.Current && offset >= 0)
            {
                // consume bytes
                for (int i = 0; i < offset; i++)
                {
                    this.ReadByte();
                }

                return this.position;
            }

            throw new NotSupportedException("can only seek forwards");
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            if (disposing)
            {
                // dispose managed resources
                if (this.deflateStream != null)
                {
                    this.deflateStream.Dispose();
                    this.deflateStream = null;

                    if (this.crcread is null)
                    {
                        // Consume the trailing 4 bytes
                        this.crcread = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            this.crcread[i] = (byte)this.rawStream.ReadByte();
                        }
                    }
                }
            }

            base.Dispose(disposing);

            // Call the appropriate methods to clean up
            // unmanaged resources here.
            // Note disposing is done.
            this.isDisposed = true;
        }
    }
}
