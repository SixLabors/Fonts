// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.IO;

/// <summary>
/// Exposes a range of an existing stream without copying its bytes.
/// Disposing the view leaves the source stream open.
/// </summary>
internal sealed class BoundedReadStream : Stream
{
    private readonly Stream source;
    private readonly long start;
    private long position;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedReadStream"/> class.
    /// </summary>
    /// <param name="source">The seekable source stream.</param>
    /// <param name="start">The first byte in the source stream.</param>
    /// <param name="length">The maximum number of bytes in the view.</param>
    public BoundedReadStream(Stream source, long start, long length)
    {
        this.source = source;
        this.start = start;
        this.Length = Math.Min(length, Math.Max(0, source.Length - start));
    }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length { get; }

    /// <inheritdoc/>
    public override long Position
    {
        get => this.position;
        set => this.position = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <inheritdoc/>
    public override void Flush()
    {
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
        => this.Read(buffer.AsSpan(offset, count));

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer)
    {
        if (this.position >= this.Length || buffer.IsEmpty)
        {
            return 0;
        }

        // Readers for several tables can share the source. Each view keeps
        // its own position so reading a different table cannot move it.
        long offset = this.start + this.position;
        if (this.source.Position != offset)
        {
            this.source.Position = offset;
        }

        int count = (int)Math.Min(buffer.Length, this.Length - this.position);
        int read = this.source.Read(buffer[..count]);
        this.position += read;
        return read;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        this.Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.position + offset,
            SeekOrigin.End => this.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };

        return this.position;
    }

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
