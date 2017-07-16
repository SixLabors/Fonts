// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts
{
#if FILESYSTEM
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    internal class FileFontInstance : IFontInstance
    {
        private Lazy<FontInstance> font;

        public FileFontInstance(string path)
        {
            this.Description = FontDescription.LoadDescription(path);
            this.font = new Lazy<Fonts.FontInstance>(() => FontInstance.LoadFont(path));
        }

        public FontDescription Description { get; }

        public ushort EmSize => this.font.Value.EmSize;

        public int LineHeight => this.font.Value.LineHeight;

        public GlyphInstance GetGlyph(char character)
            => this.font.Value.GetGlyph(character);

        public Vector2 GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph)
            => this.font.Value.GetOffset(glyph, previousGlyph);
    }
#endif
}
