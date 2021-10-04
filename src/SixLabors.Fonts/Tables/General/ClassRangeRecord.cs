// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.General
{
    [DebuggerDisplay("StartGlyphId: {StartGlyphId}, EndGlyphId: {EndGlyphId}, ClassNo: {ClassNo}")]
    internal class ClassRangeRecord
    {
        public ClassRangeRecord(ushort startGlyphId, ushort endGlyphId, ushort classNo)
        {
            this.StartGlyphId = startGlyphId;
            this.EndGlyphId = endGlyphId;
            this.ClassNo = classNo;
        }

        public ushort StartGlyphId { get; }

        public ushort EndGlyphId { get; }

        public ushort ClassNo { get; }
    }
}
