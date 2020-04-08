// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap
{
    internal abstract class CMapSubTable
    {
        public CMapSubTable()
        {
        }

        public CMapSubTable(PlatformIDs platform, ushort encoding, ushort format)
        {
            this.Platform = platform;
            this.Encoding = encoding;
            this.Format = format;
        }

        public ushort Format { get; }

        public PlatformIDs Platform { get; }

        public ushort Encoding { get; }

        public abstract bool TryGetGlyphId(int codePoint, out ushort glyphId);
    }
}
