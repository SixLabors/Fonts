// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts
{
    /// <summary>
    /// provide metadata about a font.
    /// </summary>
    internal class FileFontInstance : IFontInstance
    {
        private readonly Lazy<IFontInstance> font;

        public FileFontInstance(string path)
            : this(path, 0)
        {
        }

        public FileFontInstance(string path, long offset)
            : this(FontDescription.LoadDescription(path), path, offset)
        {
        }

        internal FileFontInstance(FontDescription description, string path, long offset)
        {
            this.Description = description;
            this.Path = path;
            this.font = new Lazy<Fonts.IFontInstance>(() => FontInstance.LoadFont(path, offset));
        }

        public FontDescription Description { get; }

        public string Path { get; }

        public ushort EmSize => this.font.Value.EmSize;

        public short Ascender => this.font.Value.Ascender;

        public short Descender => this.font.Value.Descender;

        public short LineGap => this.font.Value.LineGap;

        public int LineHeight => this.font.Value.LineHeight;

        GlyphInstance IFontInstance.GetGlyph(int codePoint)
            => this.font.Value.GetGlyph(codePoint);

        Vector2 IFontInstance.GetOffset(GlyphInstance glyph, GlyphInstance previousGlyph)
            => this.font.Value.GetOffset(glyph, previousGlyph);

        /// <summary>
        /// Reads a <see cref="FontInstance"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontInstance"/>.</returns>
        public static FileFontInstance[] LoadFontCollection(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                long startPos = fs.Position;
                var reader = new BinaryReader(fs, true);
                var ttcHeader = TtcHeader.Read(reader);
                var fonts = new FileFontInstance[(int)ttcHeader.NumFonts];

                for (int i = 0; i < ttcHeader.NumFonts; ++i)
                {
                    fs.Position = startPos + ttcHeader.OffsetTable[i];
                    var description = FontDescription.LoadDescription(fs);
                    fonts[i] = new FileFontInstance(description, path, (long)ttcHeader.OffsetTable[i]);
                }

                return fonts;
            }
        }
    }
}
