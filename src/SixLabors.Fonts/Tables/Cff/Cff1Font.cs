// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Cff1Font
    {
        internal string FontName { get; set; }

        public Cff1GlyphData[] _glyphs { get; internal set; }

        internal List<byte[]> _localSubrRawBufferList;
        internal List<byte[]> _globalSubrRawBufferList;

        internal int _defaultWidthX;
        internal int _nominalWidthX;
        internal List<FontDict> _cidFontDict;

        public string Version { get; set; } // CFF SID

        public string Notice { get; set; }// CFF SID

        public string CopyRight { get; set; }// CFF SID

        public string FullName { get; set; }// CFF SID

        public string FamilyName { get; set; }// CFF SID

        public string Weight { get; set; }// CFF SID

        public double UnderlinePosition { get; set; }

        public double UnderlineThickness { get; set; }

        public double[] FontBBox { get; set; }

#if DEBUG
        public Cff1Font()
        {
        }

#endif
    }
}
