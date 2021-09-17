// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    /// <summary>
    /// In a ComponentRecord, the zero-based ligatureAnchorOffsets array lists offsets to Anchor tables by mark class.
    /// If a component does not define an attachment point for a particular class of marks, then the offset to the corresponding Anchor table will be NULL.
    /// Example 8 at the end of this chapter shows a MarkLigPosFormat1 subtable used to attach mark accents to a ligature glyph in the Arabic script.
    /// </summary>
    internal class ComponentRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentRecord"/> class.
        /// </summary>
        /// <param name="reader">The big endian binary reader.</param>
        /// <param name="markClassCount">Number of defined mark classes.</param>
        public ComponentRecord(BigEndianBinaryReader reader, ushort markClassCount)
        {
            this.LigatureAnchorOffsets = new ushort[markClassCount];
            for (int i = 0; i < markClassCount; i++)
            {
                this.LigatureAnchorOffsets[i] = reader.ReadOffset16();
            }
        }

        public ushort[] LigatureAnchorOffsets { get; }
    }
}
