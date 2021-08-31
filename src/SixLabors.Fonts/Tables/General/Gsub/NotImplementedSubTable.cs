// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General.Gsub
{
    internal class NotImplementedSubTable : LookupSubTable
    {
        public override bool TrySubstition(IGlyphSubstitutionCollection collection, ushort index, int count)
            => false;
    }
}
