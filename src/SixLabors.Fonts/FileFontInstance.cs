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
        private readonly Lazy<IFontInstance> font;

        public FileFontInstance(string path)
        {
            this.Description = FontDescription.LoadDescription(path);
            this.font = new Lazy<Fonts.IFontInstance>(() => FontInstance.LoadFont(path));
        }

        public FontDescription Description { get; }

        public ushort EmSize => this.font.Value.EmSize;

        public short Ascender => this.font.Value.Ascender;

        public short Descender => this.font.Value.Descender;

        public short LineGap => this.font.Value.LineGap;

        public int LineHeight => this.font.Value.LineHeight;

        GlyphInstance IFontInstance.GetGlyph(int codePoint)
            => this.font.Value.GetGlyph(codePoint);

        Vector2 IFontInstance.GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph)
            => this.font.Value.GetOffset(glyph, previousGlyph);
    }
#endif
}
