// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

namespace SixLabors.Fonts
{
    internal interface IFontInstance
    {
        FontDescription Description { get; }

        ushort EmSize { get; }

        int LineHeight { get; }

        short Ascender { get; }

        short Descender { get; }

        short LineGap { get; }

        GlyphInstance GetGlyph(int codePoint);

        Vector2 GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph);
    }
}