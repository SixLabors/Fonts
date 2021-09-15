// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal readonly struct BaseRecord
    {
        public BaseRecord(BigEndianBinaryReader reader, ushort classCount)
        {
            this.BaseAnchorOffsets = new ushort[classCount];
            for (int i = 0; i < classCount; i++)
            {
                this.BaseAnchorOffsets[i] = reader.ReadUInt16();
            }
        }

        /// <summary>
        /// Gets the base anchor offsets.
        /// Array of offsets (one per mark class) to Anchor tables. Offsets are from beginning of BaseArray table, ordered by class.
        /// </summary>
        public ushort[] BaseAnchorOffsets { get; }
    }
}
