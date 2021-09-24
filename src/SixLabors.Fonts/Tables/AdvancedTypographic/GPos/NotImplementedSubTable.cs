// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GPos
{
    internal class NotImplementedSubTable : LookupSubTable
    {
        public override bool TryUpdatePosition(
            IFontMetrics fontMetrics,
            GPosTable table,
            GlyphPositioningCollection collection,
            Tag feature,
            ushort index,
            int count)
            => false;
    }
}
