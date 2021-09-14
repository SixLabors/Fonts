// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Gsub
{
    internal class NotImplementedSubTable : LookupSubTable
    {
        public override bool TrySubstitution(GSubTable table, GlyphSubstitutionCollection collection, ushort index, int count)
            => false;
    }
}
