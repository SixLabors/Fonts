// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Woff
{
    internal sealed class Woff2TableHeader : TableHeader
    {
        public Woff2TableHeader(string tag, uint checkSum, uint offset, uint len)
            : base(tag, checkSum, offset, len)
        {
        }
    }
}
