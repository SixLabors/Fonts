// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap;

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

    public abstract bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId);

    public abstract IEnumerable<int> GetAvailableCodePoints();
}
