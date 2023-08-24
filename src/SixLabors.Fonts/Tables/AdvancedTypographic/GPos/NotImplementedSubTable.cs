// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal class NotImplementedSubTable : LookupSubTable
    {
        public NotImplementedSubTable()
            : base(default)
        {
        }

        public override bool TryUpdatePosition(
            FontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            int index,
            int count)
            => false;
    }
}
