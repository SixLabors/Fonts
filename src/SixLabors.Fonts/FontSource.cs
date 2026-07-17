// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts;

/// <summary>
/// Provides access to the source bytes for a font face.
/// </summary>
internal abstract class FontSource
{
    /// <summary>
    /// Creates a source backed by a filesystem path.
    /// </summary>
    /// <param name="path">The font path.</param>
    /// <param name="offset">The face offset.</param>
    /// <returns>The font source.</returns>
    public static FontSource Create(string path, long offset) => new FileFontSource(path, offset);

    /// <summary>
    /// Creates a source backed by owned memory.
    /// </summary>
    /// <param name="data">The source data.</param>
    /// <param name="offset">The face offset.</param>
    /// <returns>The font source.</returns>
    public static FontSource Create(byte[] data, long offset) => new MemoryFontSource(data, offset);

    /// <summary>
    /// Attempts to get the OpenType table data for the specified tag.
    /// </summary>
    /// <param name="tag">The table tag.</param>
    /// <param name="table">
    /// When this method returns, contains the table data if the table exists; otherwise, the default value.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the table exists; otherwise, <see langword="false"/>.</returns>
    public abstract bool TryGetTableData(Tag tag, out ReadOnlyMemory<byte> table);

    /// <summary>
    /// Opens a readable stream positioned at the font face data.
    /// </summary>
    /// <returns>A readable stream positioned at the font face data.</returns>
    public abstract Stream OpenStream();

    private sealed class FileFontSource : FontSource
    {
        private readonly string path;
        private readonly long offset;

        public FileFontSource(string path, long offset)
        {
            this.path = path;
            this.offset = offset;
        }

        /// <inheritdoc/>
        public override bool TryGetTableData(Tag tag, out ReadOnlyMemory<byte> table)
        {
            using Stream stream = this.OpenStream();
            using FontReader reader = new(stream);
            return reader.TryGetTableData(tag, out table);
        }

        /// <inheritdoc/>
        public override Stream OpenStream()
        {
            FileStream stream = File.OpenRead(this.path);
            stream.Position = this.offset;

            return stream;
        }
    }

    private sealed class MemoryFontSource : FontSource
    {
        private readonly byte[] data;
        private readonly long offset;

        public MemoryFontSource(byte[] data, long offset)
        {
            this.data = data;
            this.offset = offset;
        }

        /// <inheritdoc/>
        public override bool TryGetTableData(Tag tag, out ReadOnlyMemory<byte> table)
        {
            using Stream stream = this.OpenStream();
            using FontReader reader = new(stream);
            return reader.TryGetTableData(tag, out table);
        }

        /// <inheritdoc/>
        public override Stream OpenStream()
        {
            MemoryStream stream = new(this.data, writable: false)
            {
                Position = this.offset
            };

            return stream;
        }
    }
}
