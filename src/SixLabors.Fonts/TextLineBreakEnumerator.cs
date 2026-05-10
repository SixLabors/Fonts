// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Breaks a prepared logical line one visual line at a time.
/// </summary>
internal sealed class TextLineBreakEnumerator
{
    private readonly LogicalTextLine logicalLine;
    private readonly TextOptions options;
    private readonly bool breakAll;
    private readonly bool keepAll;
    private readonly bool breakWord;
    private readonly bool normalizeDecomposedAdvances;
    private readonly int maxLines;
    private readonly CodePoint? ellipsisMarkerCodePoint;
    private readonly IReadOnlyList<LineBreak> lineBreaks;
    private TextLine textLine;
    private int processed;
    private int lineCount;
    private TextLine? current;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextLineBreakEnumerator"/> class.
    /// </summary>
    /// <param name="logicalLine">The logical line to break.</param>
    /// <param name="options">The text options used for layout.</param>
    public TextLineBreakEnumerator(in LogicalTextLine logicalLine, TextOptions options)
    {
        this.logicalLine = logicalLine;
        this.options = options;
        this.breakAll = options.WordBreaking == WordBreaking.BreakAll;
        this.keepAll = options.WordBreaking == WordBreaking.KeepAll;
        this.breakWord = options.WordBreaking == WordBreaking.BreakWord;
        this.normalizeDecomposedAdvances = options.LayoutMode.IsVertical();
        this.maxLines = options.MaxLines;
        this.ellipsisMarkerCodePoint = TextLayout.GetEllipsisMarkerCodePoint(options);
        this.lineBreaks = logicalLine.LineBreaks;

        // The breaker mutates the remaining line as it advances, so each cursor owns
        // a clone of the immutable prepared line held by TextBlock.
        this.textLine = new(logicalLine.TextLine);
    }

    /// <summary>
    /// Gets the current finalized visual line.
    /// </summary>
    public TextLine Current => this.current!;

    /// <summary>
    /// Advances to the next visual line using the supplied wrapping length.
    /// </summary>
    /// <param name="wrappingLength">The wrapping length in pixels.</param>
    /// <returns><see langword="true"/> when a line was produced.</returns>
    public bool MoveNext(float wrappingLength)
    {
        if (this.textLine.Count == 0)
        {
            return false;
        }

        bool shouldWrap = wrappingLength > 0;

        // Wrapping length is always provided in pixels. Convert to inches for comparison.
        float scaledWrappingLength = shouldWrap ? wrappingLength / this.options.Dpi : float.MaxValue;

        while (this.textLine.Count > 0)
        {
            LineBreak? bestBreak = null;
            foreach (LineBreak lineBreak in this.lineBreaks)
            {
                // Skip breaks that are already behind the processed portion.
                if (lineBreak.PositionWrap <= this.processed)
                {
                    continue;
                }

                // Measure the text up to the adjusted break point.
                int measureIndex = lineBreak.PositionMeasure - this.processed;
                float advance = this.textLine.MeasureAt(measureIndex);
                if (lineBreak.IsHyphenationBreak)
                {
                    advance += this.textLine.GetHyphenationMarkerAdvance(
                        measureIndex - 1,
                        this.logicalLine.HyphenationMarkers);
                }

                if (advance >= scaledWrappingLength)
                {
                    bestBreak ??= lineBreak;
                    break;
                }

                // If it's a mandatory break, stop immediately.
                if (lineBreak.Required)
                {
                    bestBreak = lineBreak;
                    break;
                }

                // Update the best break.
                bestBreak = lineBreak;
            }

            if (bestBreak != null)
            {
                if (this.BreakAt(bestBreak.Value, scaledWrappingLength))
                {
                    return true;
                }

                continue;
            }

            return this.BreakLastLine(scaledWrappingLength);
        }

        return false;
    }

    /// <summary>
    /// Breaks the current remaining line at the supplied break opportunity.
    /// </summary>
    /// <param name="breakAt">The selected line break opportunity.</param>
    /// <param name="scaledWrappingLength">The wrapping length in inches.</param>
    /// <returns><see langword="true"/> when a visual line was produced.</returns>
    private bool BreakAt(LineBreak breakAt, float scaledWrappingLength)
    {
        if (this.breakAll)
        {
            return this.BreakAtAnyGlyph(breakAt, scaledWrappingLength);
        }

        int hyphenationMarkerIndex = breakAt.PositionMeasure - this.processed - 1;

        // Split the current line at the adjusted break index.
        if (this.textLine.TrySplitAt(breakAt, this.keepAll, out TextLine? remaining))
        {
            if (breakAt.IsHyphenationBreak)
            {
                this.textLine.ApplyHyphenationMarker(
                    hyphenationMarkerIndex,
                    this.logicalLine.HyphenationMarkers);
            }

            if (breakAt.Required
                && this.options.TextInteractionMode == TextInteractionMode.Editor
                && remaining.Count > 0
                && remaining[0].IsNewLine
                && this.textLine.TrySplitTerminalHardBreak(out TextLine? blankLine))
            {
                // Consecutive hard breaks need an editable blank line for the break that
                // ended this segment, plus the next break still waiting in the remainder.
                remaining.InsertAt(0, blankLine);
            }

            // If 'keepAll' is true then the break could be later than expected.
            this.processed = this.keepAll
                ? this.processed + Math.Max(this.textLine.Count, breakAt.PositionWrap - this.processed)
                : breakAt.PositionWrap;

            if (this.breakWord)
            {
                // A break was found, but we need to check if the line is too long
                // and break if required.
                if (this.textLine.ScaledLineAdvance > scaledWrappingLength &&
                    this.textLine.TrySplitAt(scaledWrappingLength, out TextLine? overflow))
                {
                    // Reinsert the overflow at the beginning of the remaining line.
                    this.processed -= overflow.Count;
                    remaining.InsertAt(0, overflow);
                }
            }

            bool stopLayout = this.SetCurrent(
                this.textLine,
                breakAt.Required,
                remaining.Count > 0,
                scaledWrappingLength);

            this.textLine = stopLayout ? new TextLine() : remaining;
            return true;
        }

        this.processed += this.textLine.Count;
        return false;
    }

    /// <summary>
    /// Breaks the current remaining line using CSS <see cref="WordBreaking.BreakAll"/> behavior.
    /// </summary>
    /// <param name="breakAt">The selected line break opportunity.</param>
    /// <param name="scaledWrappingLength">The wrapping length in inches.</param>
    /// <returns><see langword="true"/> when a visual line was produced.</returns>
    private bool BreakAtAnyGlyph(LineBreak breakAt, float scaledWrappingLength)
    {
        TextLine? remaining;
        if (breakAt.Required)
        {
            if (this.textLine.TrySplitAt(breakAt, this.keepAll, out remaining))
            {
                this.processed = breakAt.PositionWrap;

                bool stopLayout = this.SetCurrent(
                    this.textLine,
                    true,
                    remaining.Count > 0,
                    scaledWrappingLength);

                this.textLine = stopLayout ? new TextLine() : remaining;
                return true;
            }
        }
        else if (this.textLine.TrySplitAt(scaledWrappingLength, out remaining))
        {
            this.processed += this.textLine.Count;

            bool stopLayout = this.SetCurrent(
                this.textLine,
                false,
                remaining.Count > 0,
                scaledWrappingLength);

            this.textLine = stopLayout ? new TextLine() : remaining;
            return true;
        }
        else
        {
            this.processed += this.textLine.Count;
        }

        return false;
    }

    /// <summary>
    /// Breaks and finalizes the last remaining line.
    /// </summary>
    /// <param name="scaledWrappingLength">The wrapping length in inches.</param>
    /// <returns><see langword="true"/> when a visual line was produced.</returns>
    private bool BreakLastLine(float scaledWrappingLength)
    {
        if (this.breakWord || this.breakAll)
        {
            while (this.textLine.ScaledLineAdvance > scaledWrappingLength)
            {
                if (!this.textLine.TrySplitAt(scaledWrappingLength, out TextLine? overflow))
                {
                    break;
                }

                bool stopLayout = this.SetCurrent(
                    this.textLine,
                    false,
                    overflow.Count > 0,
                    scaledWrappingLength);

                // Width-based overflow splits do not come from a stored LineBreak, so the
                // cursor advances by the consumed entries before the next MoveNext scan.
                this.processed += this.textLine.Count;
                this.textLine = stopLayout ? new TextLine() : overflow;
                return true;
            }
        }

        if (this.options.TextInteractionMode == TextInteractionMode.Editor
            && this.textLine.TrySplitTerminalHardBreak(out TextLine? hardBreakLine))
        {
            // A terminal Enter has no following glyph for the normal required-break split.
            // Editor interaction still needs the next blank line as a caret target.
            this.SetCurrent(
                this.textLine,
                true,
                true,
                scaledWrappingLength);

            this.textLine = hardBreakLine;
            return true;
        }

        this.SetCurrent(
            this.textLine,
            true,
            false,
            scaledWrappingLength);

        this.textLine = new TextLine();
        return true;
    }

    /// <summary>
    /// Finalizes the current line and stores it as the enumerator result.
    /// </summary>
    /// <param name="line">The line to finalize.</param>
    /// <param name="skipJustification">Whether the line should skip justification.</param>
    /// <param name="hasOverflow">Whether source text remains after this line.</param>
    /// <param name="scaledWrappingLength">The wrapping length in inches.</param>
    /// <returns><see langword="true"/> when no further lines should be produced.</returns>
    private bool SetCurrent(
        TextLine line,
        bool skipJustification,
        bool hasOverflow,
        float scaledWrappingLength)
    {
        bool isLimitedFinalLine = this.maxLines > -1 && this.lineCount + 1 >= this.maxLines;
        if (isLimitedFinalLine && hasOverflow)
        {
            // A max-lines ellipsis is a final-line transformation: wrapping has already
            // chosen the visible line, so the marker replaces the tail of that line and
            // the line must behave like a paragraph-final line for justification.
            if (this.ellipsisMarkerCodePoint.HasValue)
            {
                line.ApplyEllipsisMarker(this.ellipsisMarkerCodePoint.Value, scaledWrappingLength, this.options);
            }

            skipJustification = true;
        }

        bool preserveTrailingBreakingWhitespace = this.options.TextInteractionMode == TextInteractionMode.Editor;

        // Paragraph layout trims trailing breaking whitespace. Editor interaction keeps
        // ordinary trailing whitespace addressable so typed spaces can advance the caret.
        this.current = line.Finalize(
            skipJustification,
            this.normalizeDecomposedAdvances,
            preserveTrailingBreakingWhitespace);

        if (!this.current.SkipJustification)
        {
            this.current.Justify(this.options);
        }

        this.lineCount++;
        return isLimitedFinalLine;
    }
}
