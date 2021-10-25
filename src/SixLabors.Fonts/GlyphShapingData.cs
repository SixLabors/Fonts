// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SixLabors.Fonts.Tables.AdvancedTypographic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Contains supplementary data that allows the shaping of glyphs.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal readonly struct GlyphShapingData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphShapingData"/> struct.
        /// </summary>
        /// <param name="codePoint">The leading codepoint.</param>
        /// <param name="direction">The text direction.</param>
        /// <param name="glyphIds">The collection of glyph ids.</param>
        public GlyphShapingData(CodePoint codePoint, TextDirection direction, ushort[] glyphIds)
            : this(codePoint, 1, direction, glyphIds, new List<TagEntry>(), 0, -1, -1, -1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphShapingData"/> struct.
        /// </summary>
        /// <param name="codePoint">The leading codepoint.</param>
        /// <param name="codePointCount">The codepoint count represented by this glyph.</param>
        /// <param name="direction">The text direction.</param>
        /// <param name="glyphIds">The collection of glyph ids.</param>
        /// <param name="features">The collection of features.</param>
        /// <param name="ligatureId">The id of any ligature this glyph is a member of.</param>
        /// <param name="ligatureComponents">The component count of the glyph.</param>
        /// <param name="markAttachment">The index of any mark attachment.</param>
        /// <param name="cursiveAttachment">The index of any cursive attachment.</param>
        public GlyphShapingData(
            CodePoint codePoint,
            int codePointCount,
            TextDirection direction,
            ushort[] glyphIds,
            List<TagEntry> features,
            int ligatureId,
            int ligatureComponents,
            int markAttachment,
            int cursiveAttachment)
        {
            this.CodePoint = codePoint;
            this.CodePointCount = codePointCount;
            this.Direction = direction;
            this.GlyphIds = glyphIds;
            this.Features = features;
            this.LigatureId = ligatureId;
            this.LigatureComponent = ligatureComponents;
            this.MarkAttachment = markAttachment;
            this.CursiveAttachment = cursiveAttachment;
        }

        /// <summary>
        /// Gets the leading codepoint.
        /// </summary>
        public CodePoint CodePoint { get; }

        /// <summary>
        /// Gets the codepoint count represented by this glyph.
        /// </summary>
        public int CodePointCount { get; }

        /// <summary>
        /// Gets the text direction.
        /// </summary>
        public TextDirection Direction { get; }

        /// <summary>
        /// Gets the collection of glyph ids.
        /// </summary>
        public ushort[] GlyphIds { get; }

        /// <summary>
        /// Gets the id of any ligature this glyph is a member of.
        /// </summary>
        public int LigatureId { get; }

        /// <summary>
        /// Gets the ligature component index of the glyph.
        /// </summary>
        public int LigatureComponent { get; }

        /// <summary>
        /// Gets the index of any mark attachment.
        /// </summary>
        public int MarkAttachment { get; }

        /// <summary>
        /// Gets the index of any cursive attachment.
        /// </summary>
        public int CursiveAttachment { get; }

        /// <summary>
        /// Gets the collection of features.
        /// </summary>
        public List<TagEntry> Features { get; }

        private string DebuggerDisplay
            => FormattableString
            .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {CodePoint.GetScriptClass(this.CodePoint)} : {this.Direction} : {this.LigatureId} : {this.LigatureComponent} : [{string.Join(",", this.GlyphIds)}]");
    }
}
