// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Contains a composed logical text line and its width-independent line break opportunities.
/// </summary>
internal readonly struct LogicalTextLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogicalTextLine"/> struct.
    /// </summary>
    /// <param name="textLine">The composed logical text line.</param>
    /// <param name="lineBreaks">The collected line break opportunities.</param>
    /// <param name="wordSegments">The collected word-boundary segment runs.</param>
    /// <param name="hyphenationMarkers">The visible hyphenation markers created for soft hyphen entries.</param>
    public LogicalTextLine(
        TextLine textLine,
        List<LineBreak> lineBreaks,
        List<WordSegmentRun> wordSegments,
        List<GlyphLayoutData> hyphenationMarkers)
    {
        this.TextLine = textLine;
        this.LineBreaks = lineBreaks;
        this.WordSegments = wordSegments;
        this.HyphenationMarkers = hyphenationMarkers;
    }

    /// <summary>
    /// Gets the composed logical text line.
    /// </summary>
    public TextLine TextLine { get; }

    /// <summary>
    /// Gets the collected line break opportunities.
    /// </summary>
    public List<LineBreak> LineBreaks { get; }

    /// <summary>
    /// Gets the collected word-boundary segment runs.
    /// </summary>
    public List<WordSegmentRun> WordSegments { get; }

    /// <summary>
    /// Gets the visible hyphenation markers created for soft hyphen entries.
    /// </summary>
    public List<GlyphLayoutData> HyphenationMarkers { get; }
}
