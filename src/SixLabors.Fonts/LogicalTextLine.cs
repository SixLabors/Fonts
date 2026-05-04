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
    public LogicalTextLine(TextLayout.TextLine textLine, List<LineBreak> lineBreaks)
    {
        this.TextLine = textLine;
        this.LineBreaks = lineBreaks;
    }

    /// <summary>
    /// Gets the composed logical text line.
    /// </summary>
    public TextLayout.TextLine TextLine { get; }

    /// <summary>
    /// Gets the collected line break opportunities.
    /// </summary>
    public List<LineBreak> LineBreaks { get; }
}
