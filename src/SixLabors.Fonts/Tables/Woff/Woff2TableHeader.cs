// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Woff;

internal sealed class Woff2TableHeader : TableHeader
{
    public Woff2TableHeader(string tag, uint checkSum, uint offset, uint len)
        : base(tag, checkSum, offset, len)
    {
    }
}
