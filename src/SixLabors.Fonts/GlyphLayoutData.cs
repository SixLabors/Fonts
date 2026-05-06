// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Generic;
using System.Diagnostics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Per-codepoint shaping data stored inside a <see cref="TextLine"/>.
/// Each entry corresponds to a single codepoint — complex scripts may map one grapheme to
/// multiple entries (tracked via <see cref="GraphemeCodePointIndex"/>).
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal struct GlyphLayoutData
{
    internal const int NoHyphenationMarker = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphLayoutData"/> struct.
    /// </summary>
    /// <param name="metrics">The shaped glyph metrics for this codepoint.</param>
    /// <param name="pointSize">The point size at which the glyph is rendered.</param>
    /// <param name="scaledAdvance">The scaled advance of this entry.</param>
    /// <param name="scaledLineHeight">The scaled line height contributed by this entry.</param>
    /// <param name="scaledAscender">The scaled typographic ascender.</param>
    /// <param name="scaledDescender">The scaled typographic descender.</param>
    /// <param name="scaledDelta">The symmetric metrics delta applied during line-box construction.</param>
    /// <param name="scaledMinY">The minimum scaled Y (topmost ink) across <paramref name="metrics"/>.</param>
    /// <param name="bidiRun">The resolved bidi run this entry belongs to.</param>
    /// <param name="graphemeIndex">The grapheme index in the source text.</param>
    /// <param name="isLastInGrapheme">Whether this is the last codepoint in its grapheme cluster.</param>
    /// <param name="codePointIndex">The codepoint index in the source text.</param>
    /// <param name="graphemeCodePointIndex">The index of this codepoint within its grapheme cluster.</param>
    /// <param name="isTransformed">Whether the entry participates in a transformed vertical layout.</param>
    /// <param name="isDecomposed">Whether the entry was produced by Unicode decomposition.</param>
    /// <param name="stringIndex">The UTF-16 character index in the source string.</param>
    /// <param name="contributesToMeasurement">Whether this entry contributes to line metrics and measurements.</param>
    /// <param name="hyphenationMarkerIndex">The marker index to use if this entry becomes a selected soft-hyphen break.</param>
    public GlyphLayoutData(
        IReadOnlyList<GlyphMetrics> metrics,
        float pointSize,
        float scaledAdvance,
        float scaledLineHeight,
        float scaledAscender,
        float scaledDescender,
        float scaledDelta,
        float scaledMinY,
        BidiRun bidiRun,
        int graphemeIndex,
        bool isLastInGrapheme,
        int codePointIndex,
        int graphemeCodePointIndex,
        bool isTransformed,
        bool isDecomposed,
        int stringIndex,
        bool contributesToMeasurement = true,
        int hyphenationMarkerIndex = NoHyphenationMarker)
    {
        this.Metrics = metrics;
        this.PointSize = pointSize;
        this.ScaledAdvance = scaledAdvance;
        this.ScaledLineHeight = scaledLineHeight;
        this.ScaledAscender = scaledAscender;
        this.ScaledDescender = scaledDescender;
        this.ScaledDelta = scaledDelta;
        this.ScaledMinY = scaledMinY;
        this.BidiRun = bidiRun;
        this.GraphemeIndex = graphemeIndex;
        this.IsLastInGrapheme = isLastInGrapheme;
        this.CodePointIndex = codePointIndex;
        this.GraphemeCodePointIndex = graphemeCodePointIndex;
        this.IsTransformed = isTransformed;
        this.IsDecomposed = isDecomposed;
        this.StringIndex = stringIndex;
        this.ContributesToMeasurement = contributesToMeasurement;
        this.HyphenationMarkerIndex = hyphenationMarkerIndex;
    }

    /// <summary>Gets the source codepoint for this entry.</summary>
    public readonly CodePoint CodePoint => this.Metrics[0].CodePoint;

    /// <summary>Gets the shaped glyph metrics produced for this codepoint (one codepoint may map to several glyphs).</summary>
    public IReadOnlyList<GlyphMetrics> Metrics { get; }

    /// <summary>Gets the point size at which this entry is rendered.</summary>
    public float PointSize { get; }

    /// <summary>Gets or sets the scaled advance of this entry (mutated by justification).</summary>
    public float ScaledAdvance { get; set; }

    /// <summary>Gets the scaled line height contributed by this entry, before line-spacing is applied.</summary>
    public float ScaledLineHeight { get; }

    /// <summary>Gets the scaled typographic ascender.</summary>
    public float ScaledAscender { get; }

    /// <summary>Gets the scaled typographic descender.</summary>
    public float ScaledDescender { get; }

    /// <summary>Gets the symmetric ascender/descender delta applied during line-box construction.</summary>
    public float ScaledDelta { get; }

    /// <summary>Gets the smallest (most negative) scaled Y across <see cref="Metrics"/>.</summary>
    public float ScaledMinY { get; }

    /// <summary>Gets the resolved bidi run this entry belongs to.</summary>
    public BidiRun BidiRun { get; }

    /// <summary>Gets the text direction derived from <see cref="BidiRun"/>.</summary>
    public readonly TextDirection TextDirection => (TextDirection)this.BidiRun.Direction;

    /// <summary>Gets the grapheme index in the source text.</summary>
    public int GraphemeIndex { get; }

    /// <summary>Gets or sets a value indicating whether this is the last entry in its grapheme cluster.</summary>
    public bool IsLastInGrapheme { get; set; }

    /// <summary>Gets the index of this codepoint within its grapheme cluster (0-based).</summary>
    public int GraphemeCodePointIndex { get; }

    /// <summary>Gets the codepoint index in the source text.</summary>
    public int CodePointIndex { get; }

    /// <summary>Gets a value indicating whether the entry participates in a transformed vertical layout.</summary>
    public bool IsTransformed { get; }

    /// <summary>Gets a value indicating whether the entry was produced by Unicode decomposition.</summary>
    public bool IsDecomposed { get; }

    /// <summary>Gets the UTF-16 character index in the source string.</summary>
    public int StringIndex { get; }

    /// <summary>Gets or sets a value indicating whether this entry contributes to line metrics and measurements.</summary>
    public bool ContributesToMeasurement { get; set; }

    /// <summary>Gets the marker index to use if this entry becomes a selected soft-hyphen break.</summary>
    public int HyphenationMarkerIndex { get; }

    /// <summary>Gets a value indicating whether the codepoint is a line-break character.</summary>
    public readonly bool IsNewLine => CodePoint.IsNewLine(this.CodePoint);

    private readonly string DebuggerDisplay => FormattableString
        .Invariant($"{this.CodePoint.ToDebuggerDisplay()} : {this.TextDirection} : {this.CodePointIndex}, level: {this.BidiRun.Level}");
}
