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
        public EntryExitRecord(BigEndianBinaryReader reader)
        {
            this.EntryAnchorOffset = reader.ReadUInt16();
            this.ExitAnchorOffset = reader.ReadUInt16();
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
