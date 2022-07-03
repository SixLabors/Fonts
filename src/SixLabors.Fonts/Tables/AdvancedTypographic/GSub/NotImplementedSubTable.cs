// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub
{
    internal class NotImplementedSubTable : LookupSubTable
    {
        public NotImplementedSubTable()
            : base(default)
        {
        }

        public override bool TrySubstitution(
            FontMetrics fontMetrics,
            GSubTable table,
            GlyphSubstitutionCollection collection,
            Tag feature,
            int index,
            int count)
            => false;
    }
}
