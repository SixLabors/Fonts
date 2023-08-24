// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    [DebuggerDisplay("StartGlyphId: {StartGlyphId}, EndGlyphId: {EndGlyphId}, Index: {Index}")]
    internal readonly struct CoverageRangeRecord
    {
        public CoverageRangeRecord(ushort startGlyphId, ushort endGlyphId, ushort startCoverageIndex)
        {
            this.StartGlyphId = startGlyphId;
            this.EndGlyphId = endGlyphId;
            this.Index = startCoverageIndex;
        }

        public ushort StartGlyphId { get; }

        public ushort EndGlyphId { get; }

        public ushort Index { get; }
    }
}
