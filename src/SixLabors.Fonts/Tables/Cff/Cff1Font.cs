// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Cff1Font
    {
        public Cff1Font(string name, CffTopDictionary metrics, Cff1GlyphData[] glyphs)
        {
            this.FontName = name;
            this.Metrics = metrics;
            this.Glyphs = glyphs;
        }

        public string FontName { get; set; }

        public CffTopDictionary Metrics { get; set; }

        public Cff1GlyphData[] Glyphs { get; }
    }
}