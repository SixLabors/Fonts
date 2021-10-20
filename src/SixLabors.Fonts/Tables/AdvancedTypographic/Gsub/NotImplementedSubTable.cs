// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
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
            ushort index,
            int count)
            => false;
    }
}
