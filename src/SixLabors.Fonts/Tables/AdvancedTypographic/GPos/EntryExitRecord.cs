// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// EntryExitRecord sued in Cursive Attachment Positioning Format1.
    /// Each EntryExitRecord consists of two offsets: one to an Anchor table that identifies the entry point on the glyph (entryAnchorOffset),
    /// and an offset to an Anchor table that identifies the exit point on the glyph (exitAnchorOffset).
    /// </summary>
    internal readonly struct EntryExitRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntryExitRecord"/> struct.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="offset">The offset to exitAnchor table, from beginning of CursivePos subtable.</param>
        public EntryExitRecord(BigEndianBinaryReader reader, long offset)
        {
            // EntryExitRecord
            // +--------------+------------------------+------------------------------------------------+
            // | Type         | Name                   | Description                                    |
            // +==============+========================+================================================+
            // | Offset16     | entryAnchorOffset      | Offset to entryAnchor table, from beginning of |
            // |              |                        | CursivePos subtable (may be NULL).             |
            // +--------------+------------------------+------------------------------------------------+
            // | Offset16     | exitAnchorOffset       | Offset to exitAnchor table, from beginning of  |
            // |              |                        | CursivePos subtable (may be NULL).             |
            // +--------------+------------------------+------------------------------------------------+
            ushort entryAnchorOffset = reader.ReadOffset16();
            ushort exitAnchorOffset = reader.ReadOffset16();

            this.EntryAnchor = entryAnchorOffset != 0 ? AnchorTable.Load(reader, offset + entryAnchorOffset) : null;
            this.ExitAnchor = exitAnchorOffset != 0 ? AnchorTable.Load(reader, offset + exitAnchorOffset) : null;
        }

        /// <summary>
        /// Gets the entry anchor table.
        /// </summary>
        public AnchorTable? EntryAnchor { get; }

        /// <summary>
        /// Gets the exit anchor table.
        /// </summary>
        public AnchorTable? ExitAnchor { get; }
    }
}
