// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

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
            ushort index,
            int count)
            => false;
    }
}
