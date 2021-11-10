// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// <para>
    /// Represents a font face with metrics, which is a set of glyphs with a specific style (regular, italic, bold etc).
    /// </para>
    /// <para>The font source is a filesystem path.</para>
    /// </summary>
    internal sealed class FileFontMetrics : FontMetrics
    {
        private readonly Lazy<StreamFontMetrics> metrics;

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
            this.metrics = new Lazy<StreamFontMetrics>(() => StreamFontMetrics.LoadFont(path, offset));
        }

        /// <inheritdoc cref="FontMetrics.Description"/>
        public override FontDescription Description { get; }

        /// <summary>
        /// Gets the filesystem path to the font face source.
        /// </summary>
        public string Path { get; }

        /// <inheritdoc />
        public override ushort UnitsPerEm => this.metrics.Value.UnitsPerEm;

        /// <inheritdoc />
        public override float ScaleFactor => this.metrics.Value.ScaleFactor;

        /// <inheritdoc />
        public override short Ascender => this.metrics.Value.Ascender;

        /// <inheritdoc />
        public override short Descender => this.metrics.Value.Descender;

        /// <inheritdoc />
        public override short LineGap => this.metrics.Value.LineGap;

        /// <inheritdoc />
        public override short LineHeight => this.metrics.Value.LineHeight;

        /// <inheritdoc/>
        public override short AdvanceWidthMax => this.metrics.Value.AdvanceWidthMax;

        /// <inheritdoc/>
        public override short AdvanceHeightMax => this.metrics.Value.AdvanceHeightMax;

        /// <inheritdoc/>
        internal override bool TryGetGlyphId(CodePoint codePoint, out ushort glyphId)
            => this.metrics.Value.TryGetGlyphId(codePoint, out glyphId);

        /// <inheritdoc/>
        internal override bool TryGetGlyphId(
            CodePoint codePoint,
            CodePoint? nextCodePoint,
            out ushort glyphId,
            out bool skipNextCodePoint)
            => this.metrics.Value.TryGetGlyphId(codePoint, nextCodePoint, out glyphId, out skipNextCodePoint);

        /// <inheritdoc/>
        internal override bool TryGetGlyphClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? glyphClass)
            => this.metrics.Value.TryGetGlyphClass(glyphId, out glyphClass);

        /// <inheritdoc/>
        internal override bool TryGetMarkAttachmentClass(ushort glyphId, [NotNullWhen(true)] out GlyphClassDef? markAttachmentClass)
            => this.metrics.Value.TryGetMarkAttachmentClass(glyphId, out markAttachmentClass);

        /// <inheritdoc />
        public override IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, ColorFontSupport support)
              => this.metrics.Value.GetGlyphMetrics(codePoint, support);

        /// <inheritdoc />
        internal override IEnumerable<GlyphMetrics> GetGlyphMetrics(CodePoint codePoint, ushort glyphId, ColorFontSupport support)
            => this.metrics.Value.GetGlyphMetrics(codePoint, glyphId, support);

        /// <inheritdoc/>
        internal override void ApplySubstitution(GlyphSubstitutionCollection collection, KerningMode kerningMode)
            => this.metrics.Value.ApplySubstitution(collection, kerningMode);

        /// <inheritdoc/>
        internal override void UpdatePositions(GlyphPositioningCollection collection, KerningMode kerningMode)
            => this.metrics.Value.UpdatePositions(collection, kerningMode);

        /// <summary>
        /// Reads a <see cref="StreamFontMetrics"/> from the specified stream.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>a <see cref="StreamFontMetrics"/>.</returns>
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
