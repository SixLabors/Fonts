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
            this.EntryAnchorOffset = reader.ReadOffset16();
            this.ExitAnchorOffset = reader.ReadOffset16();
        }

        /// <summary>
        /// Gets the offset to entryAnchor table, from beginning of CursivePos subtable.
        /// </summary>
        public ushort EntryAnchorOffset { get; }

        /// <summary>
        /// Gets the offset to exitAnchor table, from beginning of CursivePos subtable.
        /// </summary>
        public ushort ExitAnchorOffset { get; }
    }
}
