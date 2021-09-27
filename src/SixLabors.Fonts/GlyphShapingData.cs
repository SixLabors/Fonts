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
    /// Contains supplemetary data that allows the shaping of glyphs.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct GlyphShapingData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphShapingData"/> struct.
        /// </summary>
        /// <param name="codePoint">The codepoint.</param>
        /// <param name="direction">The text direction.</param>
        /// <param name="glyphIds">The collection of glyph ids.</param>
        public GlyphShapingData(CodePoint codePoint, TextDirection direction, ushort[] glyphIds)
            : this(codePoint, direction, glyphIds, new HashSet<Tag>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphShapingData"/> struct.
        /// </summary>
        /// <param name="codePoint">The codepoint.</param>
        /// <param name="direction">The text direction.</param>
        /// <param name="glyphIds">The collection of glyph ids.</param>
        /// <param name="features">The collection of features.</param>
        public GlyphShapingData(CodePoint codePoint, TextDirection direction, ushort[] glyphIds, HashSet<Tag> features)
        {
            this.CodePoint = codePoint;
            this.Direction = direction;
            this.GlyphIds = glyphIds;
            this.Features = features;
        }

        /// <summary>
        /// Gets the codepoint.
        /// </summary>
        public CodePoint CodePoint { get; }

        /// <summary>
        /// Gets the text direction.
        /// </summary>
        public TextDirection Direction { get; }

        /// <summary>
        /// Gets the collection of glyph ids.
        /// </summary>
        public ushort[] GlyphIds { get; }

        /// <summary>
        /// Gets the collection of features.
        /// </summary>
        public HashSet<Tag> Features { get; }

        private string DebuggerDisplay
            => FormattableString
            .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {CodePoint.GetScript(this.CodePoint)} : {this.Direction} : [{string.Join(",", this.GlyphIds)}]");
    }
}