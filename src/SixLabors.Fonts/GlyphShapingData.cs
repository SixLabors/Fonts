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
    internal class GlyphShapingData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphShapingData"/> class.
        /// </summary>
        public GlyphShapingData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphShapingData"/> class.
        /// </summary>
        /// <param name="data">The data to copy properties from.</param>
        /// <param name="clearFeatures">Whether to clear features.</param>
        public GlyphShapingData(GlyphShapingData data, bool clearFeatures = false)
        {
            this.CodePoint = data.CodePoint;
            this.CodePointCount = data.CodePointCount;
            this.Direction = data.Direction;
            this.TextAttributes = data.TextAttributes;
            this.GlyphIds = data.GlyphIds;
            this.LigatureId = data.LigatureId;
            this.LigatureComponent = data.LigatureComponent;
            this.MarkAttachment = data.MarkAttachment;
            this.CursiveAttachment = data.CursiveAttachment;

            if (!clearFeatures)
            {
                this.Features = data.Features;
            }

            this.Bounds = data.Bounds;
        }

        /// <summary>
        /// Gets or sets the leading codepoint.
        /// </summary>
        public CodePoint CodePoint { get; set; }

        /// <summary>
        /// Gets or sets the codepoint count represented by this glyph.
        /// </summary>
        public int CodePointCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the text direction.
        /// </summary>
        public TextDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets the text attributes applied to this glyph.
        /// </summary>
        public TextAttribute TextAttributes { get; set; }

        /// <summary>
        /// Gets or sets the collection of glyph ids.
        /// </summary>
        public ushort[] GlyphIds { get; set; } = Array.Empty<ushort>();

        /// <summary>
        /// Gets or sets the id of any ligature this glyph is a member of.
        /// </summary>
        public int LigatureId { get; set; } = 0;

        /// <summary>
        /// Gets or sets the ligature component index of the glyph.
        /// </summary>
        public int LigatureComponent { get; set; } = -1;

        /// <summary>
        /// Gets or sets the index of any mark attachment.
        /// </summary>
        public int MarkAttachment { get; set; } = -1;

        /// <summary>
        /// Gets or sets the index of any cursive attachment.
        /// </summary>
        public int CursiveAttachment { get; set; } = -1;

        /// <summary>
        /// Gets or sets the collection of features.
        /// </summary>
        public List<TagEntry> Features { get; set; } = new List<TagEntry>();

        /// <summary>
        /// Gets or sets the shaping bounds.
        /// </summary>
        public GlyphShapingBounds Bounds { get; set; } = new(0, 0, 0, 0);

        private string DebuggerDisplay
            => FormattableString
            .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {CodePoint.GetScriptClass(this.CodePoint)} : {this.Direction} : {this.LigatureId} : {this.LigatureComponent} : [{string.Join(",", this.GlyphIds)}]");
    }
}
