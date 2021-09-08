// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// <para>
    /// Represents a font face with metrics, which is
    /// a set of glyphs with a specific style (regular, italic, bold etc).
    /// </para>
    /// <para>The font source is a filesystem path.</para>
    /// </summary>
    internal class FileFontMetrics : IFontMetrics
    {
        private readonly Lazy<IFontMetrics> metrics;

        public FileFontMetrics(string path)
            : this(path, 0)
        {
        }

        public FileFontMetrics(string path, long offset)
            : this(FontDescription.LoadDescription(path), path, offset)
        {
        }

        internal FileFontMetrics(FontDescription description, string path, long offset)
        {
            this.Description = description;
            this.Path = path;
            this.metrics = new Lazy<IFontMetrics>(() => FontMetrics.LoadFont(path, offset));
        }

        /// <inheritdoc cref="IFontMetrics.Description"/>
        public FontDescription Description { get; }

        /// <summary>
        /// Gets the filesystem path to the font face source.
        /// </summary>
        public string Path { get; }

        /// <inheritdoc />
        public ushort UnitsPerEm => this.metrics.Value.UnitsPerEm;

        /// <inheritdoc />
        public float ScaleFactor => this.metrics.Value.ScaleFactor;

        /// <inheritdoc />
        public short Ascender => this.metrics.Value.Ascender;

        /// <inheritdoc />
        public short Descender => this.metrics.Value.Descender;

        /// <inheritdoc />
        public short LineGap => this.metrics.Value.LineGap;

        /// <inheritdoc />
        public short LineHeight => this.metrics.Value.LineHeight;

        /// <inheritdoc/>
        public short AdvanceWidthMax => this.metrics.Value.AdvanceWidthMax;

        /// <inheritdoc/>
        public short AdvanceHeightMax => this.metrics.Value.AdvanceHeightMax;

        /// <inheritdoc/>
        public bool TryGetGlyphId(
            CodePoint codePoint,
            CodePoint? nextCodePoint,
            out int glyphId,
            out bool skipNextCodePoint)
            => this.metrics.Value.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);

        /// <inheritdoc/>
        public void ApplySubstitions(GlyphSubstitutionCollection collection)
            => this.metrics.Value.ApplySubstitions(collection);

        /// <inheritdoc/>
        public void UpdatePositions(GlyphPositioningCollection collection)
            => this.metrics.Value.UpdatePositions(collection);

        /// <inheritdoc />
        public IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, int glyphId, ColorFontSupport support)
            => this.metrics.Value.GetGlyphMetrics(codePoint, glyphId, support);

        /// <inheritdoc />
        public GlyphMetrics GetGlyphMetrics(CodePoint codePoint)
              => this.metrics.Value.GetGlyphMetrics(codePoint);

        /// <inheritdoc />
        public Vector2 GetOffset(GlyphMetrics glyph, GlyphMetrics previousGlyph)
            => this.metrics.Value.GetOffset(glyph, previousGlyph);

        /// <summary>
        /// Reads a <see cref="FontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="FontMetrics"/>.</returns>
        public static FileFontMetrics[] LoadFontCollection(string path)
        {
            using FileStream fs = File.OpenRead(path);
            long startPos = fs.Position;
            var reader = new BigEndianBinaryReader(fs, true);
            var ttcHeader = TtcHeader.Read(reader);
            var fonts = new FileFontMetrics[(int)ttcHeader.NumFonts];

            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                fs.Position = startPos + ttcHeader.OffsetTable[i];
                var description = FontDescription.LoadDescription(fs);
                fonts[i] = new FileFontMetrics(description, path, ttcHeader.OffsetTable[i]);
            }

            return fonts;
        }
    }
}
