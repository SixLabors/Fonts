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

        public PlatformIDs Platform { get; private set; }
        public ushort Encoding { get; private set; }

        public abstract ushort GetGlyphId(char character);
    }
}