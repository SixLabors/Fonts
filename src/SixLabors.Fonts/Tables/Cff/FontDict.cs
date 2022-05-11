// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class FontDict
    {
        public FontDict(int dictSize, int dictOffset)
        {
            this.PrivateDicSize = dictSize;
            this.PrivateDicOffset = dictOffset;
        }

        public int FontName { get; set; }

        public int PrivateDicSize { get; }

        public int PrivateDicOffset { get; }

        public List<byte[]>? LocalSubr { get; set; }
    }
}
