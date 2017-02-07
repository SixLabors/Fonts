using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General.CMap
{
    internal abstract class CMapSubTable
    {
        public CMapSubTable()
        {
        }

        public CMapSubTable(PlatformIDs platform, ushort encoding)
        {
            this.Platform = platform;
            this.Encoding = encoding;
        }

        public PlatformIDs Platform { get; }

        public ushort Encoding { get; }

        public abstract ushort GetGlyphId(char character);
    }
}