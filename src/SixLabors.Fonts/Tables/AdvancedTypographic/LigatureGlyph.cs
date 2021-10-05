// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal sealed class LigatureGlyph
    {
        public ushort[]? CaretValueOffsets { get; internal set; }

        public static LigatureGlyph Load(BigEndianBinaryReader reader, long offset)
        {
            reader.Seek(offset, SeekOrigin.Begin);

            ushort caretCount = reader.ReadUInt16();
            var ligatureGlyph = new LigatureGlyph()
            {
                CaretValueOffsets = reader.ReadUInt16Array(caretCount)
            };

            return ligatureGlyph;
        }
    }
}
