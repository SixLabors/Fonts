// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    internal class CffFont
    {
        public CffFont(string name, CffTopDictionary metrics, CffGlyphData[] glyphs)
        {
            this.FontName = name;
            this.Metrics = metrics;
            this.Glyphs = glyphs;
        }

        public string FontName { get; set; }

        public CffTopDictionary Metrics { get; set; }

        public CffGlyphData[] Glyphs { get; }
    }
}
